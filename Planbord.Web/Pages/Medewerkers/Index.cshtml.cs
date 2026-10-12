using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Planbord.Core;

namespace Planbord.Web.Pages.Medewerkers;

public class IndexModel(PlanbordDb db) : PageModel
{
    [BindProperty] public Invoer Form { get; set; } = new();
    public List<Medewerker> Medewerkers { get; private set; } = [];
    public Dictionary<int, string> Accounts { get; private set; } = [];

    public class Invoer
    {
        public int? Id { get; set; }
        [Required(ErrorMessage = "Geef een naam op."), MaxLength(100), Display(Name = "Naam")] public string Naam { get; set; } = "";
        [MaxLength(30), Phone(ErrorMessage = "Ongeldig telefoonnummer."), Display(Name = "Telefoon")] public string? Telefoon { get; set; }
    }

    public async Task OnGetAsync()
    {
        Medewerkers = await db.Medewerkers.OrderByDescending(m => m.Actief).ThenBy(m => m.Naam).ToListAsync();
        Accounts = await db.Users.Where(u => u.MedewerkerId != null).ToDictionaryAsync(u => u.MedewerkerId!.Value, u => u.Email!);
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid) { await OnGetAsync(); return Page(); }
        var m = Form.Id is { } id ? await db.Medewerkers.FindAsync(id) : null;
        if (m is null) db.Medewerkers.Add(m = new Medewerker());
        m.Naam = Form.Naam.Trim();
        m.Telefoon = Form.Telefoon?.Trim();
        await db.SaveChangesAsync();
        TempData["Melding"] = $"{m.Naam} opgeslagen.";
        return RedirectToPage();
    }

    /// Employees are never deleted so historical records stay meaningful.
    public async Task<IActionResult> OnPostActiefAsync(int id)
    {
        if (await db.Medewerkers.FindAsync(id) is { } m) { m.Actief = !m.Actief; await db.SaveChangesAsync(); }
        return RedirectToPage();
    }
}
