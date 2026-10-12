using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Planbord.Core;

namespace Planbord.Web.Pages.Account;

public class LoginModel(SignInManager<AppUser> signIn) : PageModel
{
    [BindProperty] public Invoer Form { get; set; } = new();
    [BindProperty(SupportsGet = true)] public string? ReturnUrl { get; set; }

    public class Invoer
    {
        [Required(ErrorMessage = "Vul je e-mailadres in."), EmailAddress(ErrorMessage = "Ongeldig e-mailadres."), Display(Name = "E-mailadres")]
        public string Email { get; set; } = "";
        [Required(ErrorMessage = "Vul je wachtwoord in."), DataType(DataType.Password), Display(Name = "Wachtwoord")]
        public string Wachtwoord { get; set; } = "";
        [Display(Name = "Aangemeld blijven")] public bool Onthouden { get; set; }
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid) return Page();
        var res = await signIn.PasswordSignInAsync(Form.Email, Form.Wachtwoord, Form.Onthouden, lockoutOnFailure: true);
        if (res.Succeeded) return LocalRedirect(Url.IsLocalUrl(ReturnUrl) ? ReturnUrl! : "/");
        ModelState.AddModelError("", res.IsLockedOut ? "Te veel pogingen. Probeer het over enkele minuten opnieuw." : "E-mailadres of wachtwoord is onjuist.");
        return Page();
    }
}
