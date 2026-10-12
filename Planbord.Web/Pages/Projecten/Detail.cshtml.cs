using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Planbord.Core;

namespace Planbord.Web.Pages.Projecten;

public class DetailModel(PlanbordDb db) : PageModel
{
    [BindProperty(SupportsGet = true)] public int Id { get; set; }
    [BindProperty] public ProjectInvoer Form { get; set; } = new();
    [BindProperty, MaxLength(300)] public string? NieuweTaak { get; set; }
    public Project Project { get; private set; } = null!;

    async Task<bool> Laad()
    {
        var p = await db.Projecten.Include(p => p.Taken.OrderBy(t => t.Klaar).ThenBy(t => t.Id)).FirstOrDefaultAsync(p => p.Id == Id);
        if (p is null) return false;
        Project = p;
        return true;
    }

    public async Task<IActionResult> OnGetAsync()
    {
        if (!await Laad()) return NotFound();
        Form = ProjectInvoer.Van(Project);
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!await Laad()) return NotFound();
        if (!ModelState.IsValid) return Page();
        Form.ZetOp(Project);
        await db.SaveChangesAsync();
        TempData["Melding"] = "Opgeslagen.";
        return RedirectToPage(new { Id });
    }

    public async Task<IActionResult> OnPostTaakAsync()
    {
        if (!string.IsNullOrWhiteSpace(NieuweTaak) && NieuweTaak.Length <= 300)
        {
            db.Taken.Add(new ProjectTaak { ProjectId = Id, Omschrijving = NieuweTaak.Trim() });
            await db.SaveChangesAsync();
        }
        return RedirectToPage(new { Id });
    }

    public async Task<IActionResult> OnPostWisselAsync(int taakId)
    {
        var t = await db.Taken.FirstOrDefaultAsync(t => t.Id == taakId && t.ProjectId == Id);
        if (t is not null)
        {
            t.Klaar = !t.Klaar;
            t.KlaarOp = t.Klaar ? DateTime.UtcNow : null;
            t.KlaarDoor = t.Klaar ? User.Identity!.Name : null;
            await db.SaveChangesAsync();
        }
        return RedirectToPage(new { Id });
    }

    public async Task<IActionResult> OnPostVerwijderTaakAsync(int taakId)
    {
        await db.Taken.Where(t => t.Id == taakId && t.ProjectId == Id).ExecuteDeleteAsync();
        return RedirectToPage(new { Id });
    }
}
