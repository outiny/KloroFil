using KloroFil.Web.Data;
using KloroFil.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KloroFil.Web.Controllers;

/// <summary>Objectifs environnementaux et indicateurs de performance.</summary>
public class ObjectifsController(KloroFilDbContext db) : Controller
{
    public async Task<IActionResult> Index() =>
        View(await db.Objectifs.AsNoTracking().OrderBy(o => o.Statut).ThenBy(o => o.Echeance).ToListAsync());

    [Authorize(Roles = RolesKloroFil.Gestion)]
    public async Task<IActionResult> Edition(int? id)
    {
        var o = id is null ? new ObjectifEnvironnemental { Echeance = new DateTime(DateTime.Today.Year, 12, 31) } : await db.Objectifs.FindAsync(id);
        return o is null ? NotFound() : View(o);
    }

    [HttpPost, Authorize(Roles = RolesKloroFil.Gestion)]
    public async Task<IActionResult> Edition(int? id, ObjectifEnvironnemental o)
    {
        if (!ModelState.IsValid) return View(o);
        if (id is null) db.Objectifs.Add(o);
        else
        {
            var existant = await db.Objectifs.FindAsync(id);
            if (existant is null) return NotFound();
            o.Id = existant.Id;
            db.Entry(existant).CurrentValues.SetValues(o);
        }
        await db.SaveChangesAsync();
        TempData["Succes"] = "Objectif enregistré.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, Authorize(Roles = RolesKloroFil.Gestion)]
    public async Task<IActionResult> Delete(int id)
    {
        var o = await db.Objectifs.FindAsync(id);
        if (o is null) return NotFound();
        db.Objectifs.Remove(o);
        await db.SaveChangesAsync();
        TempData["Succes"] = "Objectif supprimé.";
        return RedirectToAction(nameof(Index));
    }
}
