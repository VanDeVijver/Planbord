using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Planbord.Core;

namespace Planbord.Web.Pages.Account;

public class LogoutModel(SignInManager<AppUser> signIn) : PageModel
{
    public IActionResult OnGet() => RedirectToPage("/Index");

    public async Task<IActionResult> OnPostAsync()
    {
        await signIn.SignOutAsync();
        return RedirectToPage("/Account/Login");
    }
}
