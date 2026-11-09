using KloroFil.Web.Data;
using KloroFil.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace KloroFil.Web.Controllers;

/// <summary>Prescriptions des arrêtés ICPE : contrôles périodiques, mesures, déclarations (GEREP…), échéancier.</summary>
public class PrescriptionsController(KloroFilDbContext db) : Controller
{
    public async Task<IActionResult> Index(int? siteId, string? filtre, string? q)
    {
        var aujourdhui = DateTime.Today;
        var requete = db.Prescriptions.AsNoTracking().Include(p => p.Site).AsQueryable();
        if (siteId is not null) requete = requete.Where(p => p.SiteId == siteId);
        if (!string.IsNullOrWhiteSpace(q)) requete = requete.Where(p => p.Intitule.Contains(q) || p.Source.Contains(q) || (p.Article != null && p.Article.Contains(q)));
        requete = filtre switch
        {
            "retard" => requete.Where(p => p.ProchaineEcheance < aujourdhui),
            "avenir" => requete.Where(p => p.ProchaineEcheance >= aujourdhui && p.ProchaineEcheance <= aujourdhui.AddDays(60)),
            "nc" => requete.Where(p => p.Conformite == Conformite.NonConforme || p.Conformite == Conformite.Partielle),
            _ => requete
        };

        ViewBag.Sites = new SelectList(await db.Sites.OrderBy(s => s.Nom).ToListAsync(), nameof(Site.Id), nameof(Site.Nom), siteId);
        ViewBag.Filtre = filtre;
        ViewBag.Q = q;
        return View(await requete.OrderBy(p => p.ProchaineEcheance == null).ThenBy(p => p.ProchaineEcheance).ThenBy(p => p.Intitule).ToListAsync());
    }

    // Consultation ouverte à tous (formulaire en lecture seule) ; l'enregistrement est restreint.
    public async Task<IActionResult> Edition(int? id, int? siteId)
    {
        var p = id is null ? new Prescription { SiteId = siteId ?? 0 } : await db.Prescriptions.FindAsync(id);
        if (p is null) return NotFound();
        if (!await db.Sites.AnyAsync())
        {
            TempData["Erreur"] = "Créez d'abord un site.";
            return RedirectToAction("Edition", "Sites");
        }
        await ListesAsync(p.SiteId);
        return View(p);
    }

    [HttpPost, Authorize(Roles = RolesKloroFil.Gestion)]
    public async Task<IActionResult> Edition(int? id, Prescription p)
    {
        if (!await db.Sites.AnyAsync(s => s.Id == p.SiteId)) ModelState.AddModelError(nameof(p.SiteId), "Choisissez un site.");
        if (!ModelState.IsValid)
        {
            await ListesAsync(p.SiteId);
            return View(p);
        }

        // Échéance calculée si elle n'est pas saisie.
        p.ProchaineEcheance ??= Affichage.Suivante(p.DerniereRealisation, p.Periodicite);

        if (id is null) db.Prescriptions.Add(p);
        else
        {
            var existant = await db.Prescriptions.FindAsync(id);
            if (existant is null) return NotFound();
            p.Id = existant.Id;
            db.Entry(existant).CurrentValues.SetValues(p);
        }
        await db.SaveChangesAsync();
        TempData["Succes"] = $"Prescription « {p.Intitule} » enregistrée.";
        return RedirectToAction(nameof(Index), new { siteId = p.SiteId });
    }

    /// <summary>Enregistre la réalisation du jour et reporte l'échéance selon la périodicité.</summary>
    [HttpPost, Authorize(Roles = RolesKloroFil.Saisie)]
    public async Task<IActionResult> Realiser(int id, string? retour)
    {
        var p = await db.Prescriptions.FindAsync(id);
        if (p is null) return NotFound();
        p.DerniereRealisation = DateTime.Today;
        p.ProchaineEcheance = Affichage.Suivante(p.DerniereRealisation, p.Periodicite);
        if (p.Conformite == Conformite.AEvaluer) p.Conformite = Conformite.Conforme;
        await db.SaveChangesAsync();
        TempData["Succes"] = p.ProchaineEcheance is null
            ? $"« {p.Intitule} » réalisée."
            : $"« {p.Intitule} » réalisée ; prochaine échéance le {p.ProchaineEcheance.Date()}.";
        return Url.IsLocalUrl(retour) ? LocalRedirect(retour) : RedirectToAction(nameof(Index));
    }

    [HttpPost, Authorize(Roles = RolesKloroFil.Gestion)]
    public async Task<IActionResult> Delete(int id)
    {
        var p = await db.Prescriptions.FindAsync(id);
        if (p is null) return NotFound();
        await db.DetacherActionsAsync(prescriptionId: id);
        db.Prescriptions.Remove(p);
        await db.SaveChangesAsync();
        TempData["Succes"] = $"Prescription « {p.Intitule} » supprimée.";
        return RedirectToAction(nameof(Index), new { siteId = p.SiteId });
    }

    private async Task ListesAsync(int siteId) =>
        ViewBag.Sites = new SelectList(await db.Sites.OrderBy(s => s.Nom).ToListAsync(), nameof(Site.Id), nameof(Site.Nom), siteId);
}
