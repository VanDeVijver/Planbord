using System.Collections.Concurrent;
using System.Security.Claims;
using System.Threading.Channels;
using Microsoft.EntityFrameworkCore;
using Planbord.Core;

namespace Planbord.Web;

/// In-app realtime signals over Server-Sent Events. A signal only tells clients to refresh;
/// the database stays authoritative, so it carries no content.
// ponytail: in-memory, single instance only; move to Postgres LISTEN/NOTIFY when scaling out
public class Signalen
{
    readonly ConcurrentDictionary<Guid, Channel<string>> abonnees = new();

    /// soort: "rapport" or "bericht".
    public void Stuur(string soort, int medewerkerId, DateOnly datum)
    {
        var data = $"{soort}:{medewerkerId}:{datum:yyyy-MM-dd}";
        foreach (var c in abonnees.Values) c.Writer.TryWrite(data);
    }

    public async Task Stream(HttpContext ctx)
    {
        ctx.Response.Headers.ContentType = "text/event-stream";
        ctx.Response.Headers.CacheControl = "no-cache";
        var id = Guid.NewGuid();
        var kanaal = Channel.CreateBounded<string>(new BoundedChannelOptions(50) { FullMode = BoundedChannelFullMode.DropOldest });
        abonnees[id] = kanaal;
        try
        {
            await ctx.Response.WriteAsync(": verbonden\n\n");
            await ctx.Response.Body.FlushAsync();
            await foreach (var s in kanaal.Reader.ReadAllAsync(ctx.RequestAborted))
            {
                await ctx.Response.WriteAsync($"data: {s}\n\n");
                await ctx.Response.Body.FlushAsync();
            }
        }
        catch (OperationCanceledException) { }
        finally { abonnees.TryRemove(id, out _); }
    }
}

public static class Huidig
{
    public static bool IsPlanner(this ClaimsPrincipal u) => u.IsInRole(Rollen.Eigenaar) || u.IsInRole(Rollen.Planner);

    /// The employee record linked to the signed-in account — derived server-side, never from the client.
    public static Task<int?> MedewerkerId(this PlanbordDb db, ClaimsPrincipal u)
    {
        var id = u.FindFirstValue(ClaimTypes.NameIdentifier);
        return db.Users.Where(x => x.Id == id).Select(x => x.MedewerkerId).FirstOrDefaultAsync();
    }

    public static DateOnly Vandaag => DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeBySystemTimeZoneId(DateTime.UtcNow, "Europe/Brussels"));
}
