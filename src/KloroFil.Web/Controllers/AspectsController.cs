using KloroFil.Web.Data;
using KloroFil.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace KloroFil.Web.Controllers;

/// <summary>Analyse environnementale : aspects, impacts et cotation (gravité × fréquence × maîtrise).</summary>
public class AspectsController(KloroFilDbContext db) : Controller
{
    public async Task<IActionResult> Index(bool significatifs = false)
    {
        var aspects = await db.Aspects.AsNoTracking().Include(a => a.Site).ToListAsync();
        ViewBag.Significatifs = significatifs;
        return View(aspects.Where(a => !significatifs || a.Significatif)
            .OrderByDescending(a => a.Significatif).ThenByDescending(a => a.Criticite).ThenBy(a => a.Activite).ToList());
    }

    [Authorize(Roles = RolesKloroFil.Gestion)]
    public async Task<IActionResult> Edition(int? id)
    {
        var a = id is null ? new AspectEnvironnemental() : await db.Aspects.FindAsync(id);
        if (a is null) return NotFound();
        await ListesAsync(a.SiteId);
        return View(a);
    }

    [HttpPost, Authorize(Roles = RolesKloroFil.Gestion)]
    public async Task<IActionResult> Edition(int? id, AspectEnvironnemental a)
    {
        if (!ModelState.IsValid)
        {
            await ListesAsync(a.SiteId);
            return View(a);
        }
        if (id is null) db.Aspects.Add(a);
        else
        {
            var existant = await db.Aspects.FindAsync(id);
            if (existant is null) return NotFound();
            a.Id = existant.Id;
            db.Entry(existant).CurrentValues.SetValues(a);
        }
        await db.SaveChangesAsync();
        TempData["Succes"] = a.Significatif
            ? $"Aspect enregistré : criticité {a.Criticite}, SIGNIFICATIF."
            : $"Aspect enregistré : criticité {a.Criticite}.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, Authorize(Roles = RolesKloroFil.Gestion)]
    public async Task<IActionResult> Delete(int id)
    {
        var a = await db.Aspects.FindAsync(id);
        if (a is null) return NotFound();
        await db.DetacherActionsAsync(aspectId: id);
        db.Aspects.Remove(a);
        await db.SaveChangesAsync();
        TempData["Succes"] = "Aspect supprimé.";
        return RedirectToAction(nameof(Index));
    }

    private async Task ListesAsync(int? siteId) =>
        ViewBag.Sites = new SelectList(await db.Sites.OrderBy(s => s.Nom).ToListAsync(), nameof(Site.Id), nameof(Site.Nom), siteId);
}
