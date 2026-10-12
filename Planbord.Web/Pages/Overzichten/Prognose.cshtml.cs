using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Planbord.Core;

namespace Planbord.Web.Pages.Overzichten;

public record Kandidaat(Medewerker Medewerker, int? Vanaf, double VrijeUren, Maand DezeMaand);

/// Who has room for a new project of a given length, ranked by earliest start, then most free hours.
// ponytail: availability only; filter on expertise first once employees and projects have expertise fields
public class PrognoseModel(PlanbordDb db, PlanbordService svc) : PageModel
{
    const int RekenMaanden = 36;

    [BindProperty(SupportsGet = true)] public int Duur { get; set; } = 3;
    [BindProperty] public List<int> Aandelen { get; set; } = [];

    public int Categorie { get; private set; }
    public int Procent { get; private set; }
    public List<Kandidaat> Kandidaten { get; private set; } = [];
    public List<(Medewerker Medewerker, Project Project)> Verlopen { get; private set; } = [];
    public List<DateOnly> Maandbegin { get; private set; } = [];

    public async Task OnGetAsync()
    {
        Duur = Math.Clamp(Duur, 1, 24);
        var vandaag = Huidig.Vandaag;
        Aandelen = [.. await svc.Aandelen()];
        Categorie = ProjectRegels.Categorie(vandaag, vandaag.AddMonths(Duur));
        Procent = Aandelen[Categorie - 1];

        var prognoses = await svc.Prognoses(vandaag, RekenMaanden);
        var mensen = await db.Medewerkers.Where(m => prognoses.Keys.Contains(m.Id)).ToDictionaryAsync(m => m.Id);
        Maandbegin = prognoses.Values.FirstOrDefault()?.Maanden.Select(m => m.Begin).ToList() ?? [];
        Kandidaten = prognoses
            .Select(p =>
            {
                var vanaf = Prognose.BeschikbaarVanaf(p.Value.Maanden, Procent, Duur);
                return new Kandidaat(mensen[p.Key], vanaf, Prognose.VrijeUren(p.Value.Maanden, vanaf ?? 0, Duur), p.Value.Maanden[0]);
            })
            .OrderBy(k => k.Vanaf ?? int.MaxValue).ThenByDescending(k => k.VrijeUren).ThenBy(k => k.Medewerker.Naam)
            .ToList();
        Verlopen = prognoses.SelectMany(p => p.Value.Verlopen.Select(v => (mensen[p.Key], v)))
            .OrderBy(x => x.v.Naam).ThenBy(x => x.Item1.Naam).ToList();
    }

    public async Task<IActionResult> OnPostAandelenAsync()
    {
        if (Aandelen.Count != 5 || Aandelen.Any(a => a is < 0 or > 100))
        {
            TempData["Fout"] = "Geef voor elke categorie een percentage tussen 0 en 100.";
            return RedirectToPage(new { Duur });
        }
        var rijen = await db.CategorieAandelen.OrderBy(c => c.Categorie).ToListAsync();
        for (var i = 0; i < rijen.Count; i++) rijen[i].Procent = Aandelen[i];
        await db.SaveChangesAsync();
        TempData["Melding"] = "Aandelen per categorie opgeslagen.";
        return RedirectToPage(new { Duur });
    }
}
