using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Planbord.Core;

namespace Planbord.Web.Pages.Projecten;

/// Shared by the create form (Index) and the edit form (Detail).
public class ProjectInvoer : IValidatableObject
{
    [Required(ErrorMessage = "Geef een naam op."), MaxLength(150), Display(Name = "Naam")] public string Naam { get; set; } = "";
    [MaxLength(250), Display(Name = "Adres")] public string? Adres { get; set; }
    [Display(Name = "Startdatum")] public DateOnly? Startdatum { get; set; }
    [Display(Name = "Einddatum")] public DateOnly? Einddatum { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext _)
    {
        if (Einddatum < Startdatum) yield return new("De einddatum ligt voor de startdatum.", [nameof(Einddatum)]);
    }

    public static ProjectInvoer Van(Project p) => new() { Naam = p.Naam, Adres = p.Adres, Startdatum = p.Startdatum, Einddatum = p.Einddatum };

    public void ZetOp(Project p)
    {
        p.Naam = Naam.Trim();
        p.Adres = Adres?.Trim();
        p.Startdatum = Startdatum;
        p.Einddatum = Einddatum;
    }
}

public class IndexModel(PlanbordDb db) : PageModel
{
    [BindProperty(SupportsGet = true)] public bool ToonArchief { get; set; }
    [BindProperty] public ProjectInvoer Form { get; set; } = new();
    public List<(Project Project, int OpenTaken)> Projecten { get; private set; } = [];

    public async Task OnGetAsync() =>
        Projecten = (await db.Projecten.Where(p => ToonArchief || !p.Gearchiveerd).OrderBy(p => p.Gearchiveerd).ThenBy(p => p.Naam)
            .Select(p => new { p, Open = p.Taken.Count(t => !t.Klaar) }).ToListAsync())
            .Select(x => (x.p, x.Open)).ToList();

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid) { await OnGetAsync(); return Page(); }
        var nieuw = new Project();
        Form.ZetOp(nieuw);
        db.Projecten.Add(nieuw);
        await db.SaveChangesAsync();
        TempData["Melding"] = $"Project '{Form.Naam}' aangemaakt.";
        return RedirectToPage();
    }

    /// Projects with history are archived; only unused ones are really deleted.
    public async Task<IActionResult> OnPostVerwijderAsync(int id)
    {
        var p = await db.Projecten.FindAsync(id);
        if (p is null) return NotFound();
        var heeftHistoriek = await db.Planningen.AnyAsync(x => x.ProjectId == id) || await db.DagrapportRegels.AnyAsync(x => x.ProjectId == id);
        if (heeftHistoriek) { p.Gearchiveerd = true; TempData["Melding"] = $"'{p.Naam}' heeft historiek en is gearchiveerd."; }
        else { db.Projecten.Remove(p); TempData["Melding"] = $"'{p.Naam}' is verwijderd."; }
        await db.SaveChangesAsync();
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostHerstelAsync(int id)
    {
        if (await db.Projecten.FindAsync(id) is { } p) { p.Gearchiveerd = false; await db.SaveChangesAsync(); }
        return RedirectToPage(new { toonArchief = true });
    }
}
