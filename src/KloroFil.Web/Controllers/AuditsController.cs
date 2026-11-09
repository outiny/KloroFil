using KloroFil.Web.Data;
using KloroFil.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace KloroFil.Web.Controllers;

/// <summary>Audits ISO 14001, inspections des installations classées et revues de direction.</summary>
public class AuditsController(KloroFilDbContext db) : Controller
{
    public async Task<IActionResult> Index() =>
        View(await db.Audits.AsNoTracking().Include(a => a.Site).OrderByDescending(a => a.Date).ToListAsync());

    public async Task<IActionResult> Details(int id)
    {
        var a = await db.Audits.AsNoTracking().Include(x => x.Site).FirstOrDefaultAsync(x => x.Id == id);
        if (a is null) return NotFound();
        ViewBag.Actions = await db.Actions.AsNoTracking().Where(x => x.AuditId == id).OrderBy(x => x.Echeance).ToListAsync();
        return View(a);
    }

    [Authorize(Roles = RolesKloroFil.Gestion)]
    public async Task<IActionResult> Edition(int? id)
    {
        var a = id is null ? new Audit() : await db.Audits.FindAsync(id);
        if (a is null) return NotFound();
        await ListesAsync(a.SiteId);
        return View(a);
    }

    [HttpPost, Authorize(Roles = RolesKloroFil.Gestion)]
    public async Task<IActionResult> Edition(int? id, Audit a)
    {
        if (!ModelState.IsValid)
        {
            await ListesAsync(a.SiteId);
            return View(a);
        }
        if (id is null) db.Audits.Add(a);
        else
        {
            var existant = await db.Audits.FindAsync(id);
            if (existant is null) return NotFound();
            a.Id = existant.Id;
            db.Entry(existant).CurrentValues.SetValues(a);
        }
        await db.SaveChangesAsync();
        TempData["Succes"] = "Audit enregistré.";
        return RedirectToAction(nameof(Details), new { id = a.Id });
    }

    [HttpPost, Authorize(Roles = RolesKloroFil.Gestion)]
    public async Task<IActionResult> Delete(int id)
    {
        var a = await db.Audits.FindAsync(id);
        if (a is null) return NotFound();
        await db.DetacherActionsAsync(auditId: id);
        db.Audits.Remove(a);
        await db.SaveChangesAsync();
        TempData["Succes"] = "Audit supprimé.";
        return RedirectToAction(nameof(Index));
    }

    private async Task ListesAsync(int? siteId) =>
        ViewBag.Sites = new SelectList(await db.Sites.OrderBy(s => s.Nom).ToListAsync(), nameof(Site.Id), nameof(Site.Nom), siteId);
}
