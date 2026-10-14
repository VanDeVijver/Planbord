using System.Globalization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Planbord.Core;

namespace Planbord.Web.Pages.Overzichten;

public record Balk(Project Project, double Links, double Breedte, string Kleur);

/// Projects per project leader on one 12-month timeline, so the planner sees who is free when.
public class JaarkalenderModel(PlanbordDb db) : PageModel
{
    /// Window shift in whole years; 0 = 3 months before today to 9 months after.
    [Microsoft.AspNetCore.Mvc.BindProperty(SupportsGet = true)] public int Jaar { get; set; }

    public DateOnly Van { get; private set; }
    public DateOnly Tot { get; private set; } // exclusive
    public double? Vandaag { get; private set; }
    public List<(DateOnly Begin, double Links)> Maanden { get; private set; } = [];
    public List<(Medewerker Leider, List<Balk> Balken)> Rijen { get; private set; } = [];

    public static string Procent(double fractie) => (fractie * 100).ToString("0.###", CultureInfo.InvariantCulture) + "%";

    public async Task OnGetAsync()
    {
        var vandaag = Huidig.Vandaag;
        Van = vandaag.AddMonths(-3).AddYears(Jaar);
        Tot = Van.AddMonths(12);
        if (vandaag >= Van && vandaag < Tot) Vandaag = Tijdlijn.Positie(Van, Tot, vandaag, vandaag)!.Value.Links;

        for (var m = new DateOnly(Van.Year, Van.Month, 1).AddMonths(1); m < Tot; m = m.AddMonths(1))
            Maanden.Add((m, Tijdlijn.Positie(Van, Tot, m, m)!.Value.Links));

        var leiders = await db.Medewerkers.Where(m => m.Actief && m.IsProjectleider).OrderBy(m => m.Naam).ToListAsync();
        var ids = leiders.Select(l => l.Id).ToList();
        // Archived projects are shown too when they fall inside the window.
        var projecten = await db.Projecten
            .Where(p => ids.Contains(p.ProjectleiderId) && (p.Startdatum == null || p.Startdatum < Tot) && (p.Einddatum == null || p.Einddatum >= Van))
            .OrderBy(p => p.Startdatum).ToListAsync();

        Rijen = leiders.Select(l => (l, projecten.Where(p => p.ProjectleiderId == l.Id)
                .Select(p => (p, pos: Tijdlijn.Positie(Van, Tot, p.Startdatum, p.Einddatum)))
                .Where(x => x.pos is not null)
                // Same per-project colour as on the planning board.
                .Select(x => new Balk(x.p, x.pos!.Value.Links, x.pos.Value.Breedte, Planning.IndexModel.Kleur(x.p.Id)))
                .ToList()))
            .ToList();
    }
}
