using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Planbord.Core;

namespace Planbord.Web.Pages.Medewerkers;

public class AfwezighedenModel(PlanbordDb db, PlanbordService svc) : PageModel
{
    [BindProperty] public Invoer Form { get; set; } = new();
    public List<Afwezigheid> Lijst { get; private set; } = [];
    public List<Medewerker> Medewerkers { get; private set; } = [];

    public class Invoer
    {
        [Required(ErrorMessage = "Kies een medewerker."), Display(Name = "Medewerker")] public int? MedewerkerId { get; set; }
        [Required(ErrorMessage = "Kies een begindatum."), Display(Name = "Van")] public DateOnly? Van { get; set; }
        [Required(ErrorMessage = "Kies een einddatum."), Display(Name = "Tot en met")] public DateOnly? Tot { get; set; }
        [Required(ErrorMessage = "Geef een reden op."), MaxLength(100), Display(Name = "Reden")] public string Reden { get; set; } = "";
    }

    async Task Laad()
    {
        var vanaf = Huidig.Vandaag.AddDays(-30);
        Lijst = await db.Afwezigheden.Include(a => a.Medewerker).Where(a => a.Tot >= vanaf).OrderBy(a => a.Van).ToListAsync();
        Medewerkers = await db.Medewerkers.Where(m => m.Actief).OrderBy(m => m.Naam).ToListAsync();
    }

    public async Task OnGetAsync()
    {
        Form.Van = Form.Tot = Huidig.Vandaag;
        await Laad();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (Form.Van > Form.Tot) ModelState.AddModelError("Form.Tot", "De einddatum ligt voor de begindatum.");
        if (!ModelState.IsValid) { await Laad(); return Page(); }
        var gewist = await svc.RegistreerAfwezigheid(Form.MedewerkerId!.Value, Form.Van!.Value, Form.Tot!.Value, Form.Reden.Trim());
        TempData["Melding"] = gewist.Count == 0
            ? "Afwezigheid geregistreerd."
            : $"Afwezigheid geregistreerd. {gewist.Count} planning(en) gewist: {string.Join(", ", gewist)}.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostVerwijderAsync(int id)
    {
        await db.Afwezigheden.Where(a => a.Id == id).ExecuteDeleteAsync();
        return RedirectToPage();
    }
}
