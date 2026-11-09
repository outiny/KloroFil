using KloroFil.Web.Data;
using KloroFil.Web.Models;
using KloroFil.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KloroFil.Web.Controllers;

/// <summary>Flux RSS / Atom interrogés par la veille réglementaire automatisée.</summary>
[Authorize(Roles = RolesKloroFil.Gestion)]
public class SourcesController(KloroFilDbContext db) : Controller
{
    public async Task<IActionResult> Index() =>
        View(await db.SourcesVeille.AsNoTracking().OrderBy(s => s.Nom).ToListAsync());

    public async Task<IActionResult> Edition(int? id)
    {
        var s = id is null ? new SourceVeille() : await db.SourcesVeille.FindAsync(id);
        return s is null ? NotFound() : View(s);
    }

    [HttpPost]
    public async Task<IActionResult> Edition(int? id, SourceVeille s)
    {
        if (!Uri.TryCreate(s.Url, UriKind.Absolute, out var uri) || uri.Scheme is not ("http" or "https"))
            ModelState.AddModelError(nameof(s.Url), "Adresse http(s) attendue.");
        if (await db.SourcesVeille.AnyAsync(x => x.Url == s.Url && x.Id != (id ?? 0)))
            ModelState.AddModelError(nameof(s.Url), "Ce flux est déjà déclaré.");
        if (!ModelState.IsValid) return View(s);

        if (id is null) db.SourcesVeille.Add(s);
        else
        {
            var existant = await db.SourcesVeille.FindAsync(id);
            if (existant is null) return NotFound();
            s.Id = existant.Id;
            s.DerniereLecture = existant.DerniereLecture;
            s.DernierResultat = existant.DernierResultat;
            db.Entry(existant).CurrentValues.SetValues(s);
        }
        await db.SaveChangesAsync();
        TempData["Succes"] = $"Source « {s.Nom} » enregistrée.";
        return RedirectToAction(nameof(Index));
    }

    /// <summary>Lit immédiatement une source (pratique pour tester l'adresse et les mots-clés).</summary>
    [HttpPost]
    public async Task<IActionResult> Lire(int id, [FromServices] VeilleService veille, CancellationToken ct)
    {
        var r = await veille.LancerAsync(id, ct);
        TempData[r.Erreurs.Count == 0 ? "Succes" : "Erreur"] = r.Erreurs.Count == 0
            ? $"{r.TextesAjoutes} nouveau(x) texte(s) importé(s)."
            : string.Join(" | ", r.Erreurs);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        var s = await db.SourcesVeille.FindAsync(id);
        if (s is null) return NotFound();
        db.SourcesVeille.Remove(s);
        await db.SaveChangesAsync();
        TempData["Succes"] = $"Source « {s.Nom} » supprimée (les textes déjà importés sont conservés).";
        return RedirectToAction(nameof(Index));
    }
}
