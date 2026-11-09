using KloroFil.Web.Data;
using KloroFil.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace KloroFil.Web.Controllers;

/// <summary>Plan d'actions environnemental : actions correctives, préventives, de mise en conformité.</summary>
public class ActionsController(KloroFilDbContext db) : Controller
{
    public async Task<IActionResult> Index(string? filtre, OrigineAction? origine, string? q)
    {
        filtre ??= "ouvertes";
        var aujourdhui = DateTime.Today;
        var requete = db.Actions.AsNoTracking().AsQueryable();
        requete = filtre switch
        {
            "ouvertes" => requete.Where(a => a.Statut == StatutAction.AFaire || a.Statut == StatutAction.EnCours),
            "retard" => requete.Where(a => (a.Statut == StatutAction.AFaire || a.Statut == StatutAction.EnCours) && a.Echeance < aujourdhui),
            "verifier" => requete.Where(a => a.Statut == StatutAction.Realisee),
            "closes" => requete.Where(a => a.Statut == StatutAction.Verifiee || a.Statut == StatutAction.Abandonnee),
            _ => requete
        };
        if (origine is not null) requete = requete.Where(a => a.Origine == origine);
        if (!string.IsNullOrWhiteSpace(q)) requete = requete.Where(a => a.Titre.Contains(q) || (a.Responsable != null && a.Responsable.Contains(q)));

        ViewBag.Filtre = filtre;
        ViewBag.Origine = origine;
        ViewBag.Q = q;
        return View(await requete.OrderBy(a => a.Statut).ThenBy(a => a.Echeance == null).ThenBy(a => a.Echeance)
            .ThenByDescending(a => a.Priorite).ToListAsync());
    }

    /// <summary>Création (éventuellement pré-remplie depuis un texte, une exigence, une prescription, un audit ou un aspect) ou modification.</summary>
    // Consultation ouverte à tous (formulaire en lecture seule) ; l'enregistrement est restreint.
    public async Task<IActionResult> Edition(int? id, int? texteId, int? exigenceId, int? prescriptionId, int? auditId, int? aspectId)
    {
        ActionQse? a;
        if (id is not null) a = await db.Actions.FindAsync(id);
        else
        {
            a = new ActionQse
            {
                TexteReglementaireId = texteId, ExigenceId = exigenceId, PrescriptionId = prescriptionId,
                AuditId = auditId, AspectEnvironnementalId = aspectId,
                Echeance = DateTime.Today.AddMonths(3)
            };
            if (exigenceId is not null)
            {
                var e = await db.Exigences.Include(x => x.Texte).FirstOrDefaultAsync(x => x.Id == exigenceId);
                a.Origine = OrigineAction.Conformite;
                a.Type = TypeAction.MiseEnConformite;
                a.TexteReglementaireId = e?.TexteReglementaireId;
                a.Titre = e is null ? "" : $"Mise en conformité : {Tronquer(e.Description, 250)}";
            }
            else if (prescriptionId is not null)
            {
                var p = await db.Prescriptions.FindAsync(prescriptionId);
                a.Origine = OrigineAction.Prescription;
                a.Type = TypeAction.MiseEnConformite;
                a.Titre = p is null ? "" : Tronquer(p.Intitule, 300);
                a.Echeance = p?.ProchaineEcheance ?? a.Echeance;
            }
            else if (texteId is not null) a.Origine = OrigineAction.Veille;
            else if (auditId is not null) { a.Origine = OrigineAction.Audit; a.Type = TypeAction.Corrective; }
            else if (aspectId is not null) { a.Origine = OrigineAction.Aspect; a.Type = TypeAction.Preventive; }
        }
        if (a is null) return NotFound();
        await ListesAsync(a);
        return View(a);
    }

    [HttpPost, Authorize(Roles = RolesKloroFil.Saisie)]
    public async Task<IActionResult> Edition(int? id, ActionQse a)
    {
        if (a.Statut is StatutAction.Realisee or StatutAction.Verifiee) a.DateRealisation ??= DateTime.Today;
        if (a.Statut == StatutAction.Verifiee && string.IsNullOrWhiteSpace(a.Efficacite))
            ModelState.AddModelError(nameof(a.Efficacite), "Décrivez comment l'efficacité a été vérifiée.");
        if (!ModelState.IsValid)
        {
            await ListesAsync(a);
            return View(a);
        }

        if (id is null)
        {
            a.DateCreation = DateTime.Today;
            db.Actions.Add(a);
        }
        else
        {
            var existant = await db.Actions.FindAsync(id);
            if (existant is null) return NotFound();
            a.Id = existant.Id;
            a.DateCreation = existant.DateCreation;
            db.Entry(existant).CurrentValues.SetValues(a);
        }
        await db.SaveChangesAsync();
        TempData["Succes"] = $"Action {a.Numero} enregistrée.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, Authorize(Roles = RolesKloroFil.Gestion)]
    public async Task<IActionResult> Delete(int id)
    {
        var a = await db.Actions.FindAsync(id);
        if (a is null) return NotFound();
        db.Actions.Remove(a);
        await db.SaveChangesAsync();
        TempData["Succes"] = $"Action {a.Numero} supprimée.";
        return RedirectToAction(nameof(Index));
    }

    private async Task ListesAsync(ActionQse a)
    {
        ViewBag.Textes = new SelectList(await db.Textes.Where(t => t.Statut == StatutVeille.Applicable || t.Id == a.TexteReglementaireId)
            .OrderBy(t => t.Titre).Select(t => new { t.Id, Libelle = t.Titre.Length > 120 ? t.Titre.Substring(0, 120) + "…" : t.Titre }).ToListAsync(),
            "Id", "Libelle", a.TexteReglementaireId);
        ViewBag.Exigences = new SelectList(await db.Exigences
            .Where(e => e.Conformite == Conformite.NonConforme || e.Conformite == Conformite.Partielle || e.Id == a.ExigenceId)
            .OrderBy(e => e.Article).Select(e => new { e.Id, Libelle = (e.Article ?? "") + " " + (e.Description.Length > 110 ? e.Description.Substring(0, 110) + "…" : e.Description) }).ToListAsync(),
            "Id", "Libelle", a.ExigenceId);
        ViewBag.Prescriptions = new SelectList(await db.Prescriptions.Include(p => p.Site).OrderBy(p => p.Intitule)
            .Select(p => new { p.Id, Libelle = p.Site!.Nom + " – " + p.Intitule }).ToListAsync(), "Id", "Libelle", a.PrescriptionId);
        ViewBag.Audits = new SelectList((await db.Audits.OrderByDescending(x => x.Date).ToListAsync())
            .Select(x => new { x.Id, Libelle = $"{x.Date.Date()} – {x.Type.Libelle()}" }), "Id", "Libelle", a.AuditId);
        ViewBag.Aspects = new SelectList(await db.Aspects.OrderBy(x => x.Activite)
            .Select(x => new { x.Id, Libelle = x.Activite + " – " + x.Aspect }).ToListAsync(), "Id", "Libelle", a.AspectEnvironnementalId);
    }

    private static string Tronquer(string s, int max) => s.Length <= max ? s : s[..(max - 1)] + "…";
}
