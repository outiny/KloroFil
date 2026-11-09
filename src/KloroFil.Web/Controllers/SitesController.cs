using KloroFil.Web.Data;
using KloroFil.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KloroFil.Web.Controllers;

/// <summary>Sites, situation administrative ICPE et tableau des rubriques de classement.</summary>
public class SitesController(KloroFilDbContext db) : Controller
{
    public async Task<IActionResult> Index() =>
        View(await db.Sites.AsNoTracking().Include(s => s.Classements).Include(s => s.Prescriptions).OrderBy(s => s.Nom).ToListAsync());

    public async Task<IActionResult> Details(int id)
    {
        var site = await db.Sites.AsNoTracking()
            .Include(s => s.Classements.OrderBy(c => c.Rubrique))
            .Include(s => s.Prescriptions.OrderBy(p => p.ProchaineEcheance))
            .FirstOrDefaultAsync(s => s.Id == id);
        return site is null ? NotFound() : View(site);
    }

    [Authorize(Roles = RolesKloroFil.Gestion)]
    public async Task<IActionResult> Edition(int? id)
    {
        if (id is null) return View(new Site());
        var site = await db.Sites.FindAsync(id);
        return site is null ? NotFound() : View(site);
    }

    [HttpPost, Authorize(Roles = RolesKloroFil.Gestion)]
    public async Task<IActionResult> Edition(int? id, Site site)
    {
        if (await db.Sites.AnyAsync(s => s.Nom == site.Nom && s.Id != (id ?? 0)))
            ModelState.AddModelError(nameof(site.Nom), "Un site porte déjà ce nom.");
        if (!ModelState.IsValid) return View(site);

        if (id is null) db.Sites.Add(site);
        else
        {
            var existant = await db.Sites.FindAsync(id);
            if (existant is null) return NotFound();
            site.Id = existant.Id;
            db.Entry(existant).CurrentValues.SetValues(site);
        }
        await db.SaveChangesAsync();
        TempData["Succes"] = $"Site « {site.Nom} » enregistré.";
        return RedirectToAction(nameof(Details), new { id = site.Id });
    }

    [HttpPost, Authorize(Roles = RolesKloroFil.Gestion)]
    public async Task<IActionResult> Delete(int id)
    {
        var site = await db.Sites.FindAsync(id);
        if (site is null) return NotFound();
        await db.DetacherActionsAsync(prescriptionsDuSite: id);
        db.Sites.Remove(site);
        await db.SaveChangesAsync();
        TempData["Succes"] = $"Site « {site.Nom} » supprimé.";
        return RedirectToAction(nameof(Index));
    }

    // ---- Rubriques de classement -------------------------------------------------------------

    [Authorize(Roles = RolesKloroFil.Gestion)]
    public async Task<IActionResult> Classement(int? id, int siteId)
    {
        var c = id is null ? new Classement { SiteId = siteId } : await db.Classements.FindAsync(id);
        if (c is null) return NotFound();
        c.Site = await db.Sites.FindAsync(c.SiteId);
        return c.Site is null ? NotFound() : View(c);
    }

    [HttpPost, Authorize(Roles = RolesKloroFil.Gestion)]
    public async Task<IActionResult> Classement(int? id, Classement c)
    {
        if (!await db.Sites.AnyAsync(s => s.Id == c.SiteId)) return NotFound();
        if (!ModelState.IsValid)
        {
            c.Site = await db.Sites.FindAsync(c.SiteId);
            return View(c);
        }

        c.Rubrique = c.Rubrique.Trim();
        if (id is null) db.Classements.Add(c);
        else
        {
            var existant = await db.Classements.FindAsync(id);
            if (existant is null) return NotFound();
            c.Id = existant.Id;
            db.Entry(existant).CurrentValues.SetValues(c);
        }
        await db.SaveChangesAsync();
        TempData["Succes"] = $"Rubrique {c.Rubrique} enregistrée.";
        return RedirectToAction(nameof(Details), new { id = c.SiteId });
    }

    [HttpPost, Authorize(Roles = RolesKloroFil.Gestion)]
    public async Task<IActionResult> SupprimerClassement(int id)
    {
        var c = await db.Classements.FindAsync(id);
        if (c is null) return NotFound();
        db.Classements.Remove(c);
        await db.SaveChangesAsync();
        TempData["Succes"] = $"Rubrique {c.Rubrique} supprimée.";
        return RedirectToAction(nameof(Details), new { id = c.SiteId });
    }
}
