using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Planbord.Core;

namespace Planbord.Web.Pages.Planning;

/// One cell of the board: an employee's day, or a site's day.
public class CelModel(PlanbordDb db, PlanbordService svc) : PageModel
{
    [BindProperty(SupportsGet = true)] public int? MedewerkerId { get; set; }
    [BindProperty(SupportsGet = true)] public int? ProjectId { get; set; }
    [BindProperty(SupportsGet = true)] public DateOnly Datum { get; set; }

    public string Titel { get; private set; } = "";
    public List<Core.Planning> Rijen { get; private set; } = [];
    public List<Medewerker> Medewerkers { get; private set; } = [];
    public List<Project> Projecten { get; private set; } = [];

    public async Task<IActionResult> OnGetAsync()
    {
        if (MedewerkerId is null == ProjectId is null) return NotFound();
        Medewerkers = await db.Medewerkers.Where(m => m.Actief).OrderBy(m => m.Naam).ToListAsync();
        Projecten = await db.Projecten.Where(p => !p.Gearchiveerd).OrderBy(p => p.Naam).ToListAsync();
        var q = db.Planningen.Include(p => p.Project).Include(p => p.Medewerker).Where(p => p.Datum == Datum);
        q = MedewerkerId is { } m ? q.Where(p => p.MedewerkerId == m) : q.Where(p => p.ProjectId == ProjectId);
        Rijen = await q.OrderBy(p => p.ToegewezenOp).ToListAsync();
        Titel = MedewerkerId is { } mid
            ? (await db.Medewerkers.FindAsync(mid))?.Naam ?? ""
            : (await db.Projecten.FindAsync(ProjectId))?.Naam ?? "";
        return Page();
    }

    IActionResult Terug(string? fout)
    {
        if (fout is not null) TempData["Fout"] = fout;
        return RedirectToPage(new { MedewerkerId, ProjectId, datum = Datum.ToString("yyyy-MM-dd") });
    }

    /// kies = the project (employee cell) or the employee (site cell).
    public async Task<IActionResult> OnPostToevoegenAsync(int kies) =>
        Terug(await svc.Toewijzen(MedewerkerId ?? kies, ProjectId ?? kies, Datum));

    public async Task<IActionResult> OnPostVerplaatsAsync(int id, int naarMedewerker, int naarProject, DateOnly naarDatum) =>
        Terug(await svc.Verplaats(id, naarMedewerker, naarProject, naarDatum));

    public async Task<IActionResult> OnPostVerwijderAsync(int id) { await svc.Verwijder(id); return Terug(null); }

    public async Task<IActionResult> OnPostGoedkeurenAsync(int id) { await svc.Goedkeuren(id); return Terug(null); }
}
