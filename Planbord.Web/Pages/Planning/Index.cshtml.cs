using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Planbord.Core;

namespace Planbord.Web.Pages.Planning;

public class IndexModel(PlanbordDb db, PlanbordService svc) : PageModel
{
    [BindProperty(SupportsGet = true)] public DateOnly? Week { get; set; }
    [BindProperty(SupportsGet = true)] public string Weergave { get; set; } = "medewerker";
    /// Day index 0–6: single-day board for small screens.
    [BindProperty(SupportsGet = true)] public int? Dag { get; set; }

    public DateOnly Maandag { get; private set; }
    public List<DateOnly> Dagen { get; private set; } = [];
    public List<Medewerker> Medewerkers { get; private set; } = [];
    public List<Project> Projecten { get; private set; } = [];
    public List<Core.Planning> Rijen { get; private set; } = [];
    public List<Afwezigheid> Afwezig { get; private set; } = [];
    public bool PerProject => Weergave == "project";

    public bool IsAfwezig(int medewerkerId, DateOnly d) => Afwezig.Any(a => a.MedewerkerId == medewerkerId && a.Van <= d && a.Tot >= d);

    async Task Laad()
    {
        Maandag = PlanningRegels.Maandag(Week ?? Huidig.Vandaag);
        var alle = Enumerable.Range(0, 7).Select(i => Maandag.AddDays(i)).ToList();
        Dagen = Dag is >= 0 and < 7 ? [alle[Dag.Value]] : alle;
        var zondag = Maandag.AddDays(6);
        Rijen = await db.Planningen.Include(p => p.Project).Include(p => p.Medewerker)
            .Where(p => p.Datum >= Maandag && p.Datum <= zondag).OrderBy(p => p.ToegewezenOp).ToListAsync();
        var gepland = Rijen.Select(r => r.ProjectId).ToHashSet();
        Projecten = await db.Projecten.Where(p => !p.Gearchiveerd || gepland.Contains(p.Id)).OrderBy(p => p.Naam).ToListAsync();
        Medewerkers = await db.Medewerkers.Where(m => m.Actief).OrderBy(m => m.Naam).ToListAsync();
        Afwezig = await db.Afwezigheden.Where(a => a.Van <= zondag && a.Tot >= Maandag).ToListAsync();
    }

    public async Task OnGetAsync() => await Laad();

    public async Task<IActionResult> OnGetCsvAsync()
    {
        Dag = null;
        await Laad();
        var sb = new StringBuilder().AppendLine("Medewerker;" + string.Join(';', Dagen.Select(d => d.ToString("ddd dd/MM"))));
        foreach (var m in Medewerkers)
            sb.AppendLine(Csv.Rij([m.Naam, .. Dagen.Select(d => IsAfwezig(m.Id, d) ? "AFWEZIG"
                : string.Join(" + ", Rijen.Where(r => r.MedewerkerId == m.Id && r.Datum == d).Select(r => r.Project.Naam)))]));
        return Csv.Bestand(sb, $"planning-{Maandag:yyyy-MM-dd}.csv");
    }

    public async Task<IActionResult> OnPostVerplaatsAsync(int id, int medewerkerId, int projectId, DateOnly datum)
    {
        if (await svc.Verplaats(id, medewerkerId, projectId, datum) is { } fout) TempData["Fout"] = fout;
        return new OkResult();
    }
}
