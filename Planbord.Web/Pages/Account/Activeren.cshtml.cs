using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Planbord.Core;

namespace Planbord.Web.Pages.Account;

/// Invited users set their own password through the link the owner shares.
public class ActiverenModel(UserManager<AppUser> users, SignInManager<AppUser> signIn) : PageModel
{
    [BindProperty(SupportsGet = true)] public string Gebruiker { get; set; } = "";
    [BindProperty(SupportsGet = true)] public string Token { get; set; } = "";
    [BindProperty] public Invoer Form { get; set; } = new();

    public class Invoer
    {
        [Required(ErrorMessage = "Kies een wachtwoord."), MinLength(8, ErrorMessage = "Minstens 8 tekens."), DataType(DataType.Password), Display(Name = "Nieuw wachtwoord")]
        public string Wachtwoord { get; set; } = "";
        [Compare(nameof(Wachtwoord), ErrorMessage = "De wachtwoorden zijn niet gelijk."), DataType(DataType.Password), Display(Name = "Herhaal wachtwoord")]
        public string Herhaal { get; set; } = "";
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid) return Page();
        var user = await users.FindByIdAsync(Gebruiker);
        var res = user is null ? IdentityResult.Failed() : await users.ResetPasswordAsync(user, Token, Form.Wachtwoord);
        if (!res.Succeeded)
        {
            foreach (var e in res.Errors.DefaultIfEmpty(new IdentityError { Description = "Deze uitnodiging is ongeldig of verlopen." }))
                ModelState.AddModelError("", e.Description);
            return Page();
        }
        user!.EmailConfirmed = true;
        await users.UpdateAsync(user);
        await signIn.SignInAsync(user, false);
        return RedirectToPage("/Index");
    }
}
