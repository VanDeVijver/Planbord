using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Planbord.Core;

namespace Planbord.Web.Pages.Overzichten;

public record UrenRegel(DateOnly Datum, string Medewerker, string Project, int Minuten, string? Omschrijving);
public record Bezetting(string Project, int Mandagen, int Medewerkers);

public class IndexModel(PlanbordDb db, PlanbordService svc) : PageModel
{
    [BindProperty(SupportsGet = true)] public DateOnly? Van { get; set; }
    [BindProperty(SupportsGet = true)] public DateOnly? Tot { get; set; }
    [BindProperty(SupportsGet = true)] public int? ProjectId { get; set; }
    [BindProperty(SupportsGet = true)] public int? MedewerkerId { get; set; }
    [BindProperty(SupportsGet = true)] public string Groep { get; set; } = "dag";

    public DateOnly Begin => Van ?? new DateOnly(Huidig.Vandaag.Year, Huidig.Vandaag.Month, 1);
    public DateOnly Einde => Tot ?? Begin.AddMonths(1).AddDays(-1);
    public DateOnly VolgendeMaandag => PlanningRegels.Maandag(Huidig.Vandaag).AddDays(7);

    public List<Project> Projecten { get; private set; } = [];
    public List<Medewerker> Medewerkers { get; private set; } = [];
    public List<Bezetting> Bezettingen { get; private set; } = [];
    public List<UrenRegel> Uren { get; private set; } = [];
    public List<(Medewerker Medewerker, List<DateOnly> LegeDagen)> Beschikbaar { get; private set; } = [];
    public List<Afwezigheid> AfwezigVolgendeWeek { get; private set; } = [];
    public List<Core.Planning> Conflicten { get; private set; } = [];

    public IEnumerable<IGrouping<string, UrenRegel>> Gegroepeerd => Groep switch
    {
        "project" => Uren.GroupBy(u => u.Project).OrderBy(g => g.Key),
        "medewerker" => Uren.GroupBy(u => u.Medewerker).OrderBy(g => g.Key),
        _ => Uren.GroupBy(u => u.Datum.ToString("ddd dd/MM/yyyy")),
    };

    /// Only submitted or closed reports and only worked lines count as hours.
    async Task LaadUren()
    {
        var q = db.DagrapportRegels.Where(r => r.Status == RegelStatus.GEWERKT
            && r.Dagrapport.Status != RapportStatus.Concept
            && r.Dagrapport.Datum >= Begin && r.Dagrapport.Datum <= Einde);
        if (ProjectId is { } p) q = q.Where(r => r.ProjectId == p);
        if (MedewerkerId is { } m) q = q.Where(r => r.Dagrapport.MedewerkerId == m);
        Uren = await q.OrderBy(r => r.Dagrapport.Datum).ThenBy(r => r.Dagrapport.Medewerker.Naam)
            .Select(r => new UrenRegel(r.Dagrapport.Datum, r.Dagrapport.Medewerker.Naam, r.Project.Naam, r.Minuten, r.Omschrijving))
            .ToListAsync();
    }

    public async Task OnGetAsync()
    {
        Projecten = await db.Projecten.OrderBy(p => p.Naam).ToListAsync();
        Medewerkers = await db.Medewerkers.OrderBy(m => m.Naam).ToListAsync();
        await LaadUren();

        Bezettingen = (await db.Planningen.Where(p => p.Datum >= Begin && p.Datum <= Einde)
                .Select(p => new { p.Project.Naam, p.MedewerkerId, p.Datum }).Distinct().ToListAsync())
            .GroupBy(x => x.Naam)
            .Select(g => new Bezetting(g.Key, g.Count(), g.Select(x => x.MedewerkerId).Distinct().Count()))
            .OrderByDescending(b => b.Mandagen).ToList();

        var werkdagen = Enumerable.Range(0, 5).Select(i => VolgendeMaandag.AddDays(i)).ToList();
        var vrijdag = werkdagen[^1];
        var gepland = await db.Planningen.Where(p => p.Datum >= VolgendeMaandag && p.Datum <= vrijdag)
            .Select(p => new { p.MedewerkerId, p.Datum }).ToListAsync();
        AfwezigVolgendeWeek = await db.Afwezigheden.Include(a => a.Medewerker)
            .Where(a => a.Van <= vrijdag && a.Tot >= VolgendeMaandag).ToListAsync();
        Beschikbaar = Medewerkers.Where(m => m.Actief)
            .Select(m => (m, werkdagen.Where(d => !gepland.Any(g => g.MedewerkerId == m.Id && g.Datum == d)
                && !AfwezigVolgendeWeek.Any(a => a.MedewerkerId == m.Id && a.Van <= d && a.Tot >= d)).ToList()))
            .Where(x => x.Item2.Count > 0).ToList();
        Conflicten = await db.Planningen.Include(p => p.Medewerker).Include(p => p.Project)
            .Where(p => p.IsInterventie && !p.Goedgekeurd && p.Datum >= Huidig.Vandaag)
            .OrderBy(p => p.Datum).ToListAsync();
    }

    public async Task<IActionResult> OnGetCsvAsync()
    {
        await LaadUren();
        var sb = new StringBuilder().AppendLine("Datum;Medewerker;Project;Uren;Omschrijving");
        foreach (var u in Uren)
            sb.AppendLine(Csv.Rij([u.Datum.ToString("yyyy-MM-dd"), u.Medewerker, u.Project, (u.Minuten / 60m).ToString("0.00"), u.Omschrijving]));
        return Csv.Bestand(sb, $"uren-{Begin:yyyy-MM-dd}-{Einde:yyyy-MM-dd}.csv");
    }

    public async Task<IActionResult> OnPostGoedkeurenAsync(int id)
    {
        await svc.Goedkeuren(id);
        return RedirectToPage();
    }
}
