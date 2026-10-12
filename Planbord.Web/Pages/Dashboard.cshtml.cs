using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Planbord.Core;

namespace Planbord.Web.Pages;

public record Categorie(int Nr, string Label, string Kleur);
public record Kaart(Project Project, int Categorie, ProjectStatus Status);
public record Schijf(string Label, int Minuten, string Kleur);

/// Per-employee dashboard: assigned projects by length category and clocked hours.
public class DashboardModel(PlanbordDb db, PlanbordService svc) : PageModel
{
    /// Forecast horizon: 12 months shown; computed further so long projects can be fit-checked.
    public const int ToonMaanden = 12, RekenMaanden = 36;

    public static readonly Categorie[] Categorieen =
    [
        new(1, "≤ 1 maand", "#CDEBDD"),
        new(2, "≤ 3 maanden", "#CFE4F7"),
        new(3, "≤ 6 maanden", "#DDD6F3"),
        new(4, "≤ 12 maanden", "#FBE0CC"),
        new(5, "1 jaar+", "#F7D3DA"),
    ];

    public static readonly (string Waarde, string Label)[] Periodes =
        [("jaar", "Dit jaar"), ("maand", "Deze maand"), ("kwartaal", "Dit kwartaal"), ("vorigjaar", "Vorig jaar"), ("alles", "Alles")];

    public static readonly (string Waarde, string Label)[] Weergaven =
        [("project", "Per project"), ("categorie", "Per categorie"), ("totaal", "Totaal")];

    /// Only honoured for planners; employees always see their own dashboard.
    [BindProperty(SupportsGet = true)] public int? MedewerkerId { get; set; }
    [BindProperty(SupportsGet = true)] public string Periode { get; set; } = "jaar";
    [BindProperty(SupportsGet = true)] public string Weergave { get; set; } = "project";

    public Medewerker? Medewerker { get; private set; }
    public List<Medewerker> AlleMedewerkers { get; private set; } = [];
    public List<Kaart> Kaarten { get; private set; } = [];
    public List<Schijf> Schijven { get; private set; } = [];
    public int TotaalMinuten { get; private set; }
    public string PeriodeLabel { get; private set; } = "";
    public DateOnly Vandaag { get; } = Huidig.Vandaag;

    public List<Maand> Maanden { get; private set; } = [];
    public List<Project> Verlopen { get; private set; } = [];
    public int[] Aandelen { get; private set; } = [];
    /// Per category: index of the first month a typical project of that size fits, or null.
    public int?[] BeschikbaarVanaf { get; private set; } = [];

    public async Task OnGetAsync()
    {
        var id = await db.MedewerkerId(User);
        if (User.IsPlanner())
        {
            AlleMedewerkers = await db.Medewerkers.Where(m => m.Actief).OrderBy(m => m.Naam).ToListAsync();
            id = MedewerkerId ?? id ?? AlleMedewerkers.FirstOrDefault()?.Id;
        }
        Medewerker = id is null ? null : await db.Medewerkers.FindAsync(id);
        if (Medewerker is null) return;

        // Cards: projects the employee is planned on that are active or finished in the last 3 months.
        var projectIds = await db.Planningen.Where(p => p.MedewerkerId == id).Select(p => p.ProjectId).Distinct().ToListAsync();
        Kaarten = (await db.Projecten.Where(p => projectIds.Contains(p.Id)).ToListAsync())
            .Select(p => (p, s: ProjectRegels.Status(p.Startdatum, p.Einddatum, Vandaag)))
            .Where(x => x.s is not null)
            .Select(x => new Kaart(x.p, ProjectRegels.Categorie(x.p.Startdatum, x.p.Einddatum), x.s!.Value))
            .OrderBy(k => k.Project.Startdatum).ThenBy(k => k.Project.Naam).ToList();

        await LaadUren(Medewerker.Id);

        var prognose = (await svc.Prognoses(Vandaag, RekenMaanden, Medewerker.Id))[Medewerker.Id];
        Maanden = prognose.Maanden.Take(ToonMaanden).ToList();
        Verlopen = prognose.Verlopen;
        Aandelen = await svc.Aandelen();
        BeschikbaarVanaf = Enumerable.Range(0, 5)
            .Select(c => Prognose.BeschikbaarVanaf(prognose.Maanden, Aandelen[c], Prognose.TypischeDuur[c]) is { } i && i < ToonMaanden ? i : (int?)null)
            .ToArray();
    }

    /// Hours from submitted or closed reports, worked lines only (same rule as the hours report).
    /// Includes projects no longer shown as cards.
    async Task LaadUren(int medewerkerId)
    {
        var (van, tot) = PeriodeGrenzen();
        PeriodeLabel = Periodes.FirstOrDefault(p => p.Waarde == Periode).Label ?? "Dit jaar";
        var q = db.DagrapportRegels.Where(r => r.Status == RegelStatus.GEWERKT
            && r.Dagrapport.Status != RapportStatus.Concept && r.Dagrapport.MedewerkerId == medewerkerId);
        if (van is { } v) q = q.Where(r => r.Dagrapport.Datum >= v && r.Dagrapport.Datum <= tot);
        var perProject = await q.GroupBy(r => r.ProjectId).Select(g => new { ProjectId = g.Key, Minuten = g.Sum(r => r.Minuten) }).ToListAsync();
        var ids = perProject.Select(x => x.ProjectId).ToList();
        var projecten = await db.Projecten.Where(p => ids.Contains(p.Id)).ToDictionaryAsync(p => p.Id);
        TotaalMinuten = perProject.Sum(x => x.Minuten);

        var rijen = perProject
            .Select(x => (Project: projecten[x.ProjectId], x.Minuten, Cat: ProjectRegels.Categorie(projecten[x.ProjectId].Startdatum, projecten[x.ProjectId].Einddatum)))
            .ToList();
        Schijven = Weergave == "categorie"
            ? rijen.GroupBy(r => r.Cat).OrderBy(g => g.Key)
                .Select(g => new Schijf(Categorieen[g.Key - 1].Label, g.Sum(r => r.Minuten), Categorieen[g.Key - 1].Kleur)).ToList()
            // Per project: category colour, a slightly darker shade per extra project in the same category.
            : rijen.GroupBy(r => r.Cat).OrderBy(g => g.Key)
                .SelectMany(g => g.OrderByDescending(r => r.Minuten)
                    .Select((r, i) => new Schijf(r.Project.Naam, r.Minuten, Donkerder(Categorieen[g.Key - 1].Kleur, i))))
                .ToList();
    }

    (DateOnly? Van, DateOnly Tot) PeriodeGrenzen()
    {
        var j = Vandaag.Year;
        var kwartaalStart = new DateOnly(j, (Vandaag.Month - 1) / 3 * 3 + 1, 1);
        return Periode switch
        {
            "maand" => (new DateOnly(j, Vandaag.Month, 1), new DateOnly(j, Vandaag.Month, 1).AddMonths(1).AddDays(-1)),
            "kwartaal" => (kwartaalStart, kwartaalStart.AddMonths(3).AddDays(-1)),
            "vorigjaar" => (new DateOnly(j - 1, 1, 1), new DateOnly(j - 1, 12, 31)),
            "alles" => (null, DateOnly.MaxValue),
            _ => (new DateOnly(j, 1, 1), new DateOnly(j, 12, 31)),
        };
    }

    /// Same pastel, 6% darker per step (max 4), so slices of one category stay distinguishable.
    static string Donkerder(string hex, int stap)
    {
        var f = 1 - 0.06 * Math.Min(stap, 4);
        var kanalen = Enumerable.Range(0, 3).Select(i => (int)(Convert.ToInt32(hex.Substring(1 + i * 2, 2), 16) * f));
        return "#" + string.Concat(kanalen.Select(c => c.ToString("X2")));
    }

    /// SVG pie slice from fraction a to b of a circle (centre 50,50, radius 48).
    public static string Pad(double a, double b)
    {
        static (double X, double Y) Punt(double f) => (50 + 48 * Math.Sin(2 * Math.PI * f), 50 - 48 * Math.Cos(2 * Math.PI * f));
        var (x0, y0) = Punt(a);
        var (x1, y1) = Punt(b);
        return string.Create(CultureInfo.InvariantCulture, $"M50,50 L{x0:0.##},{y0:0.##} A48,48 0 {(b - a > .5 ? 1 : 0)} 1 {x1:0.##},{y1:0.##} Z");
    }
}
