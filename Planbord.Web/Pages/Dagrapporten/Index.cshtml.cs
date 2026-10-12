using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Planbord.Core;

namespace Planbord.Web.Pages.Dagrapporten;

public record RapportRij(Medewerker Medewerker, List<string> Werven, Dagrapport? Rapport, Dictionary<int, string> ProjectNamen);

public class IndexModel(PlanbordDb db, PlanbordService svc, Signalen signalen) : PageModel
{
    [BindProperty(SupportsGet = true)] public DateOnly? Datum { get; set; }
    public DateOnly Dag => Datum ?? Huidig.Vandaag;

    public void OnGet() { }

    public async Task<IActionResult> OnGetTabelAsync()
    {
        var planning = await db.Planningen.Include(p => p.Project).Where(p => p.Datum == Dag).OrderBy(p => p.ToegewezenOp).ToListAsync();
        var rapporten = await db.Dagrapporten.Include(r => r.Regels).Where(r => r.Datum == Dag).ToDictionaryAsync(r => r.MedewerkerId);
        var ids = planning.Select(p => p.MedewerkerId).Concat(rapporten.Keys).ToHashSet();
        var namen = await db.Projecten.ToDictionaryAsync(p => p.Id, p => p.Naam);
        var rijen = (await db.Medewerkers.Where(m => ids.Contains(m.Id)).OrderBy(m => m.Naam).ToListAsync())
            .Select(m => new RapportRij(m,
                planning.Where(p => p.MedewerkerId == m.Id).Select(p => p.Project.Naam).Distinct().ToList(),
                rapporten.GetValueOrDefault(m.Id), namen))
            .ToList();
        return Partial("_Tabel", rijen);
    }

    async Task<IActionResult> Actie(int id, Func<int, Task<string?>> actie)
    {
        if (await actie(id) is { } fout) TempData["Fout"] = fout;
        else if (await db.Dagrapporten.FindAsync(id) is { } r) signalen.Stuur("rapport", r.MedewerkerId, r.Datum);
        return RedirectToPage(new { datum = Dag.ToString("yyyy-MM-dd") });
    }

    public Task<IActionResult> OnPostSluitAsync(int id) => Actie(id, svc.SluitRapport);
    public Task<IActionResult> OnPostVrijgevenAsync(int id) => Actie(id, svc.GeefVrij);
}
