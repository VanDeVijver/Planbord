using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Planbord.Web.Pages;

public class IndexModel : PageModel
{
    public IActionResult OnGet() => RedirectToPage(User.IsPlanner() ? "/Planning/Index" : "/MijnDag");
}
