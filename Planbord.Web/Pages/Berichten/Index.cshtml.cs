using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Planbord.Core;

namespace Planbord.Web.Pages.Berichten;

public class IndexModel(PlanbordDb db, PlanbordService svc, Signalen signalen) : PageModel
{
    [BindProperty(SupportsGet = true)] public DateOnly? Datum { get; set; }
    public DateOnly Dag => Datum ?? Huidig.Vandaag.AddDays(1);
    public List<(Medewerker Medewerker, string Samenvatting, Bericht? Bericht)> Rijen { get; private set; } = [];

    public async Task OnGetAsync()
    {
        var berichten = await db.Berichten.Where(b => b.Datum == Dag).ToDictionaryAsync(b => b.MedewerkerId);
        foreach (var m in await db.Medewerkers.Where(m => m.Actief).OrderBy(m => m.Naam).ToListAsync())
            Rijen.Add((m, BerichtTekst.Samenvatting(Dag, await svc.GeplandeProjecten(m.Id, Dag)), berichten.GetValueOrDefault(m.Id)));
    }

    /// Only the planner's extra text is stored; editing it marks the message unseen again.
    public async Task<IActionResult> OnPostAsync(int medewerkerId, string? extraTekst)
    {
        extraTekst = string.IsNullOrWhiteSpace(extraTekst) ? null : extraTekst.Trim();
        if (extraTekst?.Length > 2000) { TempData["Fout"] = "Het bericht is te lang (max. 2000 tekens)."; return RedirectToPage(new { datum = Dag.ToString("yyyy-MM-dd") }); }
        var b = await db.Berichten.FirstOrDefaultAsync(b => b.MedewerkerId == medewerkerId && b.Datum == Dag);
        if (b is null) db.Berichten.Add(b = new Bericht { MedewerkerId = medewerkerId, Datum = Dag });
        if (b.ExtraTekst != extraTekst) b.GezienOp = null;
        b.ExtraTekst = extraTekst;
        await db.SaveChangesAsync();
        signalen.Stuur("bericht", medewerkerId, Dag);
        TempData["Melding"] = "Bericht klaargezet.";
        return RedirectToPage(new { datum = Dag.ToString("yyyy-MM-dd") });
    }
}
