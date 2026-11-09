using KloroFil.Web.Data;
using KloroFil.Web.Models;
using KloroFil.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KloroFil.Web.Controllers;

/// <summary>Veille réglementaire : registre des textes, analyse d'applicabilité et exigences à évaluer.</summary>
public class TextesController(KloroFilDbContext db) : Controller
{
    public async Task<IActionResult> Index(StatutVeille? statut, Domaine? domaine, string? q)
    {
        var requete = db.Textes.AsNoTracking().Include(t => t.Exigences).AsQueryable();
        if (statut is not null) requete = requete.Where(t => t.Statut == statut);
        if (domaine is not null) requete = requete.Where(t => t.Domaine == domaine);
        if (!string.IsNullOrWhiteSpace(q))
            requete = requete.Where(t => t.Titre.Contains(q) || (t.Reference != null && t.Reference.Contains(q)) || (t.RubriquesConcernees != null && t.RubriquesConcernees.Contains(q)));

        ViewBag.Statut = statut;
        ViewBag.Domaine = domaine;
        ViewBag.Q = q;
        ViewBag.Compteurs = await db.Textes.GroupBy(t => t.Statut).Select(g => new { g.Key, N = g.Count() })
            .ToDictionaryAsync(x => x.Key, x => x.N);
        return View(await requete.OrderBy(t => t.Statut != StatutVeille.AAnalyser)
            .ThenByDescending(t => t.DatePublication ?? t.DateDetection).Take(500).ToListAsync());
    }

    public async Task<IActionResult> Details(int id)
    {
        var t = await db.Textes.AsNoTracking().Include(x => x.SourceVeille)
            .Include(x => x.Exigences.OrderBy(e => e.Article)).FirstOrDefaultAsync(x => x.Id == id);
        if (t is null) return NotFound();
        ViewBag.Actions = await db.Actions.AsNoTracking()
            .Where(a => a.TexteReglementaireId == id || a.Exigence!.TexteReglementaireId == id)
            .OrderBy(a => a.Echeance).ToListAsync();
        return View(t);
    }

    [Authorize(Roles = RolesKloroFil.Gestion)]
    public async Task<IActionResult> Edition(int? id)
    {
        if (id is null) return View(new TexteReglementaire { Statut = StatutVeille.AAnalyser });
        var t = await db.Textes.FindAsync(id);
        return t is null ? NotFound() : View(t);
    }

    [HttpPost, Authorize(Roles = RolesKloroFil.Gestion)]
    public async Task<IActionResult> Edition(int? id, TexteReglementaire t)
    {
        if (!ModelState.IsValid) return View(t);

        if (id is null)
        {
            t.DateDetection = DateTime.Now;
            MarquerAnalyse(t, null);
            db.Textes.Add(t);
        }
        else
        {
            var existant = await db.Textes.FindAsync(id);
            if (existant is null) return NotFound();
            var statutAvant = existant.Statut;
            // Champs gérés par l'application, absents du formulaire.
            t.Id = existant.Id;
            t.DateDetection = existant.DateDetection;
            t.SourceVeilleId = existant.SourceVeilleId;
            t.AnalyseLe = existant.AnalyseLe;
            t.AnalysePar = existant.AnalysePar;
            db.Entry(existant).CurrentValues.SetValues(t);
            MarquerAnalyse(existant, statutAvant);
        }
        await db.SaveChangesAsync();
        TempData["Succes"] = "Texte enregistré.";
        return RedirectToAction(nameof(Details), new { id = t.Id });
    }

    /// <summary>Qualification rapide depuis la liste ou la fiche (applicable / pour information / non applicable).</summary>
    [HttpPost, Authorize(Roles = RolesKloroFil.Gestion)]
    public async Task<IActionResult> Qualifier(int id, StatutVeille statut, string? retour)
    {
        var t = await db.Textes.FindAsync(id);
        if (t is null) return NotFound();
        var avant = t.Statut;
        t.Statut = statut;
        MarquerAnalyse(t, avant);
        await db.SaveChangesAsync();
        TempData["Succes"] = $"Texte classé « {statut.Libelle()} ».";
        return Url.IsLocalUrl(retour) ? LocalRedirect(retour) : RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost, Authorize(Roles = RolesKloroFil.Gestion)]
    public async Task<IActionResult> Delete(int id)
    {
        var t = await db.Textes.FindAsync(id);
        if (t is null) return NotFound();
        await db.DetacherActionsAsync(texteId: id);
        db.Textes.Remove(t);
        await db.SaveChangesAsync();
        TempData["Succes"] = "Texte supprimé.";
        return RedirectToAction(nameof(Index));
    }

    /// <summary>Lance immédiatement la lecture de toutes les sources actives.</summary>
    [HttpPost, Authorize(Roles = RolesKloroFil.Gestion)]
    public async Task<IActionResult> LancerVeille([FromServices] VeilleService veille, CancellationToken ct)
    {
        var r = await veille.LancerAsync(ct: ct);
        if (r.SourcesLues == 0) TempData["Erreur"] = "Aucune source de veille active : ajoutez-en dans « Sources ».";
        else TempData[r.Erreurs.Count == 0 ? "Succes" : "Erreur"] =
            $"{r.SourcesLues} source(s) lue(s), {r.TextesAjoutes} nouveau(x) texte(s)." +
            (r.Erreurs.Count == 0 ? "" : " Erreurs : " + string.Join(" | ", r.Erreurs));
        return RedirectToAction(nameof(Index), new { statut = StatutVeille.AAnalyser });
    }

    // ---- Exigences ---------------------------------------------------------------------------

    [Authorize(Roles = RolesKloroFil.Saisie)]
    public async Task<IActionResult> Exigence(int? id, int texteId)
    {
        var e = id is null ? new Exigence { TexteReglementaireId = texteId } : await db.Exigences.FindAsync(id);
        if (e is null) return NotFound();
        e.Texte = await db.Textes.FindAsync(e.TexteReglementaireId);
        if (e.Texte is null) return NotFound();
        if (id is null && !User.PeutGerer()) return Forbid();
        return View(e);
    }

    [HttpPost, Authorize(Roles = RolesKloroFil.Saisie)]
    public async Task<IActionResult> Exigence(int? id, Exigence e)
    {
        if (!await db.Textes.AnyAsync(t => t.Id == e.TexteReglementaireId)) return NotFound();
        if (id is null && !User.PeutGerer()) return Forbid();
        if (!ModelState.IsValid)
        {
            e.Texte = await db.Textes.FindAsync(e.TexteReglementaireId);
            return View(e);
        }

        if (e.Conformite != Conformite.AEvaluer)
        {
            e.DateEvaluation ??= DateTime.Today;
            e.EvaluePar = string.IsNullOrWhiteSpace(e.EvaluePar) ? User.Identity?.Name : e.EvaluePar;
            e.ProchaineEvaluation ??= e.DateEvaluation.Value.AddYears(1);
        }

        if (id is null) db.Exigences.Add(e);
        else
        {
            var existant = await db.Exigences.FindAsync(id);
            if (existant is null) return NotFound();
            e.Id = existant.Id;
            db.Entry(existant).CurrentValues.SetValues(e);
        }
        await db.SaveChangesAsync();

        TempData["Succes"] = "Exigence enregistrée.";
        if (e.Conformite is Conformite.NonConforme or Conformite.Partielle &&
            !await db.Actions.AnyAsync(a => a.ExigenceId == e.Id && (a.Statut == StatutAction.AFaire || a.Statut == StatutAction.EnCours)))
            TempData["Info"] = "Exigence non conforme sans action ouverte : pensez à créer une action de mise en conformité.";
        return RedirectToAction(nameof(Details), new { id = e.TexteReglementaireId });
    }

    [HttpPost, Authorize(Roles = RolesKloroFil.Gestion)]
    public async Task<IActionResult> SupprimerExigence(int id)
    {
        var e = await db.Exigences.FindAsync(id);
        if (e is null) return NotFound();
        await db.Actions.Where(a => a.ExigenceId == id).ExecuteUpdateAsync(s => s.SetProperty(a => a.ExigenceId, (int?)null));
        db.Exigences.Remove(e);
        await db.SaveChangesAsync();
        TempData["Succes"] = "Exigence supprimée.";
        return RedirectToAction(nameof(Details), new { id = e.TexteReglementaireId });
    }

    /// <summary>Registre de conformité : toutes les exigences des textes applicables.</summary>
    public async Task<IActionResult> Registre(Conformite? conformite)
    {
        var requete = db.Exigences.AsNoTracking().Include(e => e.Texte).Where(e => e.Texte!.Statut == StatutVeille.Applicable);
        if (conformite is not null) requete = requete.Where(e => e.Conformite == conformite);
        ViewBag.Conformite = conformite;
        return View(await requete.OrderBy(e => e.Conformite == Conformite.Conforme)
            .ThenBy(e => e.ProchaineEvaluation).ThenBy(e => e.Texte!.Titre).ToListAsync());
    }

    private void MarquerAnalyse(TexteReglementaire t, StatutVeille? avant)
    {
        if (t.Statut != StatutVeille.AAnalyser && t.Statut != avant)
        {
            t.AnalyseLe = DateTime.Now;
            t.AnalysePar = User.Identity?.Name;
        }
    }
}
