using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Planbord.Core;

namespace Planbord.Web.Pages.Gebruikers;

public class IndexModel(PlanbordDb db, UserManager<AppUser> users) : PageModel
{
    [BindProperty] public Invoer Form { get; set; } = new();
    public List<(AppUser User, string Rol)> Lijst { get; private set; } = [];
    public List<Medewerker> Medewerkers { get; private set; } = [];

    public class Invoer
    {
        [Required(ErrorMessage = "Vul een e-mailadres in."), EmailAddress(ErrorMessage = "Ongeldig e-mailadres."), Display(Name = "E-mailadres")]
        public string Email { get; set; } = "";
        [Required, Display(Name = "Rol")] public string Rol { get; set; } = Rollen.Medewerker;
        [Display(Name = "Gekoppelde medewerker")] public int? MedewerkerId { get; set; }
    }

    public async Task OnGetAsync()
    {
        Medewerkers = await db.Medewerkers.Where(m => m.Actief).OrderBy(m => m.Naam).ToListAsync();
        var rollen = await (from ur in db.UserRoles join r in db.Roles on ur.RoleId equals r.Id select new { ur.UserId, r.Name }).ToListAsync();
        Lijst = (await db.Users.Include(u => u.Medewerker).OrderBy(u => u.Email).ToListAsync())
            .Select(u => (u, rollen.FirstOrDefault(r => r.UserId == u.Id)?.Name ?? "—")).ToList();
    }

    async Task<string> Uitnodigingslink(AppUser u)
    {
        var token = await users.GeneratePasswordResetTokenAsync(u);
        return Url.Page("/Account/Activeren", null, new { gebruiker = u.Id, token }, Request.Scheme)!;
    }

    string? Controleer(int? medewerkerId, string rol, string? userId)
    {
        if (!Rollen.Alle.Contains(rol)) return "Onbekende rol.";
        if (rol == Rollen.Medewerker && medewerkerId is null) return "Koppel een medewerkersaccount aan een medewerker.";
        if (medewerkerId is not null && db.Users.Any(u => u.MedewerkerId == medewerkerId && u.Id != userId)) return "Deze medewerker is al aan een ander account gekoppeld.";
        return null;
    }

    public async Task<IActionResult> OnPostUitnodigenAsync()
    {
        if (Controleer(Form.MedewerkerId, Form.Rol, null) is { } f) ModelState.AddModelError("", f);
        if (await users.FindByEmailAsync(Form.Email) is not null) ModelState.AddModelError("Form.Email", "Er bestaat al een account met dit e-mailadres.");
        if (!ModelState.IsValid) { await OnGetAsync(); return Page(); }

        var u = new AppUser { UserName = Form.Email, Email = Form.Email, MedewerkerId = Form.MedewerkerId };
        var res = await users.CreateAsync(u); // no password: the invitee sets it via the link
        if (!res.Succeeded) { TempData["Fout"] = string.Join(" ", res.Errors.Select(e => e.Description)); return RedirectToPage(); }
        await users.AddToRoleAsync(u, Form.Rol);
        TempData["Melding"] = $"Uitnodiging voor {u.Email} aangemaakt. Bezorg deze link: {await Uitnodigingslink(u)}";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostWijzigAsync(string id, string rol, int? medewerkerId)
    {
        var u = await users.FindByIdAsync(id);
        if (u is null) return NotFound();
        if (u.Id == users.GetUserId(User) && rol != Rollen.Eigenaar) { TempData["Fout"] = "Je kan je eigen eigenaarsrol niet afnemen."; return RedirectToPage(); }
        if (Controleer(medewerkerId, rol, u.Id) is { } f) { TempData["Fout"] = f; return RedirectToPage(); }
        u.MedewerkerId = medewerkerId;
        await users.UpdateAsync(u);
        await users.RemoveFromRolesAsync(u, await users.GetRolesAsync(u));
        await users.AddToRoleAsync(u, rol);
        await users.UpdateSecurityStampAsync(u); // forces the new role on next request
        TempData["Melding"] = $"{u.Email} bijgewerkt.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostLinkAsync(string id)
    {
        var u = await users.FindByIdAsync(id);
        if (u is null) return NotFound();
        TempData["Melding"] = $"Nieuwe activatielink voor {u.Email}: {await Uitnodigingslink(u)}";
        return RedirectToPage();
    }
}
