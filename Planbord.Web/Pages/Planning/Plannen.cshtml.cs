using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Planbord.Core;

namespace Planbord.Web.Pages.Planning;

public class PlannenModel(PlanbordDb db, PlanbordService svc) : PageModel
{
    [BindProperty] public Invoer Form { get; set; } = new();
    public List<Medewerker> Medewerkers { get; private set; } = [];
    public List<Project> Projecten { get; private set; } = [];
    public List<string> Overgeslagen { get; private set; } = [];

    public class Invoer
    {
        [Required(ErrorMessage = "Kies een werf."), Display(Name = "Werf")] public int? ProjectId { get; set; }
        [MinLength(1, ErrorMessage = "Kies minstens één medewerker.")] public List<int> MedewerkerIds { get; set; } = [];
        [Required(ErrorMessage = "Kies een begindatum."), Display(Name = "Van")] public DateOnly? Van { get; set; }
        [Required(ErrorMessage = "Kies een einddatum."), Display(Name = "Tot en met")] public DateOnly? Tot { get; set; }
        [MinLength(1, ErrorMessage = "Kies minstens één weekdag.")] public List<DayOfWeek> Weekdagen { get; set; } =
            [DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday];
    }

    async Task Laad()
    {
        Medewerkers = await db.Medewerkers.Where(m => m.Actief).OrderBy(m => m.Naam).ToListAsync();
        Projecten = await db.Projecten.Where(p => !p.Gearchiveerd).OrderBy(p => p.Naam).ToListAsync();
    }

    public async Task OnGetAsync()
    {
        Form.Van = Form.Tot = Huidig.Vandaag;
        await Laad();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (Form.Van > Form.Tot) ModelState.AddModelError("Form.Tot", "De einddatum ligt voor de begindatum.");
        if (Form.Tot?.DayNumber - Form.Van?.DayNumber > 92) ModelState.AddModelError("Form.Tot", "Plan maximaal 3 maanden in één keer.");
        if (!ModelState.IsValid) { await Laad(); return Page(); }

        var datums = Enumerable.Range(0, Form.Tot!.Value.DayNumber - Form.Van!.Value.DayNumber + 1)
            .Select(i => Form.Van.Value.AddDays(i)).Where(d => Form.Weekdagen.Contains(d.DayOfWeek));
        var (aangemaakt, overgeslagen) = await svc.Plannen(Form.MedewerkerIds, Form.ProjectId!.Value, datums);
        TempData["Melding"] = $"{aangemaakt} toewijzing(en) aangemaakt.";
        Overgeslagen = overgeslagen;
        await Laad();
        return Page();
    }
}
