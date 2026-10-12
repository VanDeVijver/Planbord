using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Planbord.Core;

namespace Planbord.Web.Pages;

/// Employee station: schedule, planner message, site tasks and the daily report.
public class MijnDagModel(PlanbordDb db, PlanbordService svc, Signalen signalen) : PageModel
{
    [BindProperty(SupportsGet = true)] public DateOnly? Datum { get; set; }
    /// Only honoured for planners; employees always get their own linked record.
    [BindProperty(SupportsGet = true)] public int? MedewerkerId { get; set; }
    [BindProperty] public Invoer Form { get; set; } = new();

    public class Invoer
    {
        public TimeOnly? Start { get; set; }
        public TimeOnly? Einde { get; set; }
        public int PauzeMinuten { get; set; } = 30;
        public List<RegelForm> Regels { get; set; } = [];
    }

    public class RegelForm
    {
        public int ProjectId { get; set; }
        public RegelStatus? Status { get; set; }
        public string? Omschrijving { get; set; }
        public string? Duur { get; set; }
    }

    public DateOnly Dag => Datum ?? Huidig.Vandaag;
    public Medewerker? Medewerker { get; private set; }
    public bool EigenStation { get; private set; }
    public List<Project> Projecten { get; private set; } = [];
    public List<Core.Planning> Week { get; private set; } = [];
    public string? ExtraTekst { get; private set; }
    public Dagrapport? Rapport { get; private set; }
    public List<Medewerker> AlleMedewerkers { get; private set; } = [];
    public List<string> Fouten { get; private set; } = [];
    public bool Bewerkbaar => Rapport is null or { Status: RapportStatus.Concept };

    async Task<bool> BepaalMedewerker()
    {
        var eigen = await db.MedewerkerId(User);
        var id = User.IsPlanner() && MedewerkerId is not null ? MedewerkerId : eigen;
        EigenStation = id is not null && id == eigen;
        if (User.IsPlanner()) AlleMedewerkers = await db.Medewerkers.Where(m => m.Actief).OrderBy(m => m.Naam).ToListAsync();
        Medewerker = id is null ? null : await db.Medewerkers.FindAsync(id);
        return Medewerker is not null;
    }

    async Task Laad(bool vulFormulier)
    {
        var mid = Medewerker!.Id;
        Projecten = await svc.GeplandeProjecten(mid, Dag);
        Rapport = await svc.Rapport(mid, Dag);
        var maandag = PlanningRegels.Maandag(Dag);
        Week = await db.Planningen.Include(p => p.Project)
            .Where(p => p.MedewerkerId == mid && p.Datum >= maandag && p.Datum < maandag.AddDays(7))
            .OrderBy(p => p.Datum).ThenBy(p => p.ToegewezenOp).ToListAsync();
        var bericht = await db.Berichten.FirstOrDefaultAsync(b => b.MedewerkerId == mid && b.Datum == Dag);
        ExtraTekst = bericht?.ExtraTekst;
        if (EigenStation && bericht is { GezienOp: null })
        {
            bericht.GezienOp = DateTime.UtcNow;
            await db.SaveChangesAsync();
            signalen.Stuur("bericht", mid, Dag);
        }
        if (!vulFormulier) return;
        Form = new Invoer
        {
            Start = Rapport?.Start, Einde = Rapport?.Einde, PauzeMinuten = Rapport?.PauzeMinuten ?? 30,
            Regels = Projecten.Select(p => Rapport?.Regels.FirstOrDefault(r => r.ProjectId == p.Id) is { } r
                ? new RegelForm { ProjectId = p.Id, Status = r.Status, Omschrijving = r.Omschrijving, Duur = r.Minuten > 0 ? Duur.Formatteer(r.Minuten) : null }
                : new RegelForm { ProjectId = p.Id }).ToList(),
        };
    }

    public async Task<IActionResult> OnGetAsync()
    {
        if (await BepaalMedewerker()) await Laad(vulFormulier: true);
        return Page();
    }

    public Task<IActionResult> OnPostBewaarAsync() => Bewaar(indienen: false);
    public Task<IActionResult> OnPostIndienenAsync() => Bewaar(indienen: true);

    async Task<IActionResult> Bewaar(bool indienen)
    {
        if (!await BepaalMedewerker()) return Forbid();
        var invoer = new RapportInvoer { Start = Form.Start, Einde = Form.Einde, PauzeMinuten = Form.PauzeMinuten };
        foreach (var r in Form.Regels)
        {
            var minuten = Duur.Parse(r.Duur);
            if (!string.IsNullOrWhiteSpace(r.Duur) && minuten is null) Fouten.Add($"Ongeldige duur '{r.Duur}'. Gebruik u:mm, bv. 3:30.");
            invoer.Regels.Add(new RegelInvoer { ProjectId = r.ProjectId, Status = r.Status, Omschrijving = r.Omschrijving, Minuten = minuten });
        }
        if (Fouten.Count == 0) Fouten = await svc.BewaarRapport(Medewerker!.Id, Dag, invoer, indienen);
        if (Fouten.Count > 0)
        {
            await Laad(vulFormulier: false);
            return Page();
        }
        signalen.Stuur("rapport", Medewerker!.Id, Dag);
        TempData["Melding"] = indienen ? "Dagrapport ingediend." : "Concept bewaard.";
        return RedirectToPage(new { datum = Dag.ToString("yyyy-MM-dd"), medewerkerId = EigenStation ? null : (int?)Medewerker.Id });
    }

    /// Employees may only tick tasks of sites on their own schedule for this day.
    public async Task<IActionResult> OnPostTaakAsync(int taakId)
    {
        if (!await BepaalMedewerker()) return Forbid();
        var taak = await db.Taken.FindAsync(taakId);
        if (taak is null) return NotFound();
        var gepland = await db.Planningen.AnyAsync(p => p.MedewerkerId == Medewerker!.Id && p.Datum == Dag && p.ProjectId == taak.ProjectId);
        if (!gepland && !User.IsPlanner()) return Forbid();
        taak.Klaar = !taak.Klaar;
        taak.KlaarOp = taak.Klaar ? DateTime.UtcNow : null;
        taak.KlaarDoor = taak.Klaar ? Medewerker!.Naam : null;
        await db.SaveChangesAsync();
        return RedirectToPage(new { datum = Dag.ToString("yyyy-MM-dd"), medewerkerId = EigenStation ? null : (int?)Medewerker!.Id });
    }
}
