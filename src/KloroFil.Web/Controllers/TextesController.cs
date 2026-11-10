using KloroFil.Web.Data;
using KloroFil.Web.Models;
using KloroFil.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KloroFil.Web.Controllers;

/// <summary>Veille réglementaire : registre des textes, analyse d'applicabilité et exigences à évaluer.</summary>
public partial class TextesController(KloroFilDbContext db) : Controller
{
    public async Task<IActionResult> Index(StatutVeille? statut, Domaine? domaine, string? q)
    {
        var requete = db.Textes.AsNoTracking().Include(t => t.Exigences).AsQueryable();
        if (statut is not null) requete = requete.Where(t => t.Statut == statut);
        if (domaine is not null) requete = requete.Where(t => t.Domaine == domaine);
        if (!string.IsNullOrWhiteSpace(q))
            requete = requete.Where(t => t.Titre.Contains(q) || (t.Reference != null && t.Reference.Contains(q))
                || (t.RubriquesConcernees != null && t.RubriquesConcernees.Contains(q)) || (t.Resume != null && t.Resume.Contains(q)));

        ViewBag.Statut = statut;
        ViewBag.Domaine = domaine;
        ViewBag.Q = q;
        ViewBag.Compteurs = await db.Textes.GroupBy(t => t.Statut).Select(g => new { g.Key, N = g.Count() })
            .ToDictionaryAsync(x => x.Key, x => x.N);
        var textes = await requete.OrderBy(t => t.Statut != StatutVeille.AAnalyser)
            .ThenByDescending(t => t.DatePublication ?? t.DateDetection).Take(500).ToListAsync();

        // Avancement des évaluations : par texte affiché, et pour l'ensemble des textes applicables.
        var ids = textes.Select(t => t.Id).ToList();
        ViewBag.Avancements = await Avancement.ParTexteAsync(db.Exigences.Where(e => ids.Contains(e.TexteReglementaireId)));
        ViewBag.AvancementApplicables = Avancement.Cumuler((await Avancement.ParTexteAsync(
            db.Exigences.Where(e => e.Texte!.Statut == StatutVeille.Applicable))).Values);
        return View(textes);
    }

    public async Task<IActionResult> Details(int id)
    {
        var t = await db.Textes.AsNoTracking().Include(x => x.SourceVeille)
            .Include(x => x.Exigences.OrderBy(e => e.Id)).ThenInclude(e => e.Evaluations).FirstOrDefaultAsync(x => x.Id == id);
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

    /// <summary>Importe un texte du Journal officiel par son NOR, quelle que soit sa date (textes de référence anciens).</summary>
    [HttpPost, Authorize(Roles = RolesKloroFil.Gestion)]
    public async Task<IActionResult> ImporterNor(string? nor, [FromServices] LegifranceClient legifrance, CancellationToken ct)
    {
        nor = nor?.Trim().ToUpperInvariant();
        if (string.IsNullOrEmpty(nor) || nor.Length > 20 || !nor.All(char.IsLetterOrDigit))
        {
            TempData["Erreur"] = "Indiquez un NOR valide (ex. ATEP9760302A).";
            return RedirectToAction(nameof(Index));
        }

        var existant = await db.Textes.FirstOrDefaultAsync(t => t.Reference == nor, ct);
        if (existant is not null)
        {
            TempData["Info"] = $"Le texte {nor} est déjà dans la veille.";
            return RedirectToAction(nameof(Details), new { id = existant.Id });
        }

        ElementFlux? e;
        try
        {
            e = await legifrance.RechercherParNorAsync(nor, ct);
        }
        catch (LectureFluxException ex)
        {
            TempData["Erreur"] = ex.Message;
            return RedirectToAction(nameof(Index), new { q = nor });
        }
        if (e is null)
        {
            TempData["Erreur"] = $"Aucun texte du Journal officiel ne porte le NOR {nor}.";
            return RedirectToAction(nameof(Index), new { q = nor });
        }

        existant = await db.Textes.FirstOrDefaultAsync(t => t.Url == e.Lien, ct);
        if (existant is not null)
        {
            existant.Reference ??= e.Reference ?? nor;
            await db.SaveChangesAsync(ct);
            TempData["Info"] = $"Le texte {nor} est déjà dans la veille.";
            return RedirectToAction(nameof(Details), new { id = existant.Id });
        }

        var texte = new TexteReglementaire
        {
            Titre = e.Titre.Length > 600 ? e.Titre[..600] : e.Titre,
            Reference = e.Reference ?? nor,
            Url = e.Lien,
            Resume = e.Resume,
            DatePublication = e.Date,
            Type = VeilleService.DevinerType(e.Titre),
            Domaine = Domaine.Icpe,
            Statut = StatutVeille.AAnalyser,
            DateDetection = DateTime.Now
        };
        db.Textes.Add(texte);
        await db.SaveChangesAsync(ct);
        TempData["Succes"] = $"Texte {texte.Reference} importé depuis Légifrance.";
        return RedirectToAction(nameof(Details), new { id = texte.Id });
    }

    /// <summary>
    /// Importe la version en vigueur du texte (articles et points numérotés des annexes) comme exigences à évaluer.
    /// Réimporter met à jour les points dont la rédaction a changé et les repasse « En cours d'analyse » ; rien n'est supprimé.
    /// </summary>
    [HttpPost, Authorize(Roles = RolesKloroFil.Gestion)]
    public async Task<IActionResult> ImporterArticles(int id, [FromServices] LegifranceClient legifrance, CancellationToken ct)
    {
        var texte = await db.Textes.Include(t => t.Exigences).ThenInclude(x => x.Evaluations).FirstOrDefaultAsync(t => t.Id == id, ct);
        if (texte is null) return NotFound();
        if (LegifranceClient.IdentifiantTexte(texte.Url) is not { } idTexte)
        {
            TempData["Erreur"] = "Ce texte n'a pas de lien Légifrance (JORFTEXT… ou LEGITEXT…) : ses articles ne peuvent pas être importés.";
            return RedirectToAction(nameof(Details), new { id });
        }

        List<PointTexte> points;
        try
        {
            points = await legifrance.LireTexteEnVigueurAsync(idTexte, ct);
        }
        catch (LectureFluxException ex)
        {
            TempData["Erreur"] = ex.Message;
            return RedirectToAction(nameof(Details), new { id });
        }
        if (points.Count == 0)
        {
            TempData["Erreur"] = "Légifrance ne fournit aucun article en vigueur pour ce texte.";
            return RedirectToAction(nameof(Details), new { id });
        }

        int ajoutes = 0, modifies = 0;
        var existantes = texte.Exigences.Where(e => e.Article != null)
            .GroupBy(e => e.Article!, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);
        foreach (var p in points)
        {
            var article = p.Numero.Length > 50 ? p.Numero[..50] : p.Numero;
            if (existantes.TryGetValue(article, out var e))
            {
                if (Normaliser(e.Description) == Normaliser(p.Description)) continue;
                e.Description = p.Description;
                // Rédaction modifiée : chaque évaluation déjà faite repasse « En cours d'analyse », avec une trace de l'ancienne.
                foreach (var ev in e.Evaluations.Where(v => v.Conformite is not (Conformite.EnCoursAnalyse or Conformite.NonConcerne)))
                {
                    ev.Preuves = $"[{DateTime.Today.Date()}] Rédaction modifiée sur Légifrance : repassée en cours d'analyse. Évaluation précédente : {ev.Conformite.Libelle()}."
                        + (string.IsNullOrWhiteSpace(ev.Preuves) ? "" : "\n" + ev.Preuves);
                    ev.Conformite = Conformite.EnCoursAnalyse;
                }
                e.Synthetiser();
                modifies++;
            }
            else
            {
                db.Exigences.Add(new Exigence { TexteReglementaireId = id, Article = article, Description = p.Description });
                ajoutes++;
            }
        }
        await db.SaveChangesAsync(ct);

        TempData["Succes"] = ajoutes + modifies == 0
            ? $"Les {points.Count} points du texte en vigueur sont déjà à jour."
            : $"{points.Count} point(s) lu(s) sur Légifrance : {ajoutes} exigence(s) ajoutée(s), {modifies} mise(s) à jour (repassées « En cours d'analyse »).";

        // Certaines annexes ne sont pas reproduites au Journal officiel (publiées au Bulletin officiel du ministère).
        var nonReproduites = points.Where(p => AnnexeNonReproduite().IsMatch(p.Contenu)).Select(p => p.Numero).ToList();
        if (nonReproduites.Count > 0)
            TempData["Info"] = $"{string.Join(", ", nonReproduites)} : contenu non reproduit sur Légifrance (publié au Bulletin officiel). "
                + "Consultez la version consolidée sur AIDA (aida.ineris.fr) et saisissez ses prescriptions avec « + Ajouter une exigence ».";
        return RedirectToAction(nameof(Details), new { id });

        static string Normaliser(string s) => string.Join(' ', s.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
    }

    // ---- Exigences ---------------------------------------------------------------------------

    /// <summary>Évaluation d'une exigence, phase « Législation » (texte applicable) ou « Récépissé » (dossier déposé).</summary>
    [Authorize(Roles = RolesKloroFil.Saisie)]
    public async Task<IActionResult> Exigence(int? id, int texteId, PhaseEvaluation phase = PhaseEvaluation.Legislation)
    {
        if (id is null && !User.PeutGerer()) return Forbid();
        var e = id is null
            ? new Exigence { TexteReglementaireId = texteId }
            : await db.Exigences.Include(x => x.Evaluations).FirstOrDefaultAsync(x => x.Id == id);
        if (e is null) return NotFound();
        var texte = await db.Textes.FindAsync(e.TexteReglementaireId);
        if (texte is null) return NotFound();

        var evaluation = e.Evaluation(phase) ?? new EvaluationExigence { Phase = phase };
        // Première évaluation « Récépissé » : on repart des critères et moyens de surveillance définis en « Législation ».
        if (evaluation.Id == 0 && phase == PhaseEvaluation.Recepisse && e.Evaluation(PhaseEvaluation.Legislation) is { } leg)
        {
            evaluation.CriteresEvaluation = leg.CriteresEvaluation;
            evaluation.MoyensSurveillance = leg.MoyensSurveillance;
        }
        return View(new ExigenceVm { Exigence = e, Evaluation = evaluation, Phase = phase, TitreTexte = texte.Titre, Evaluations = e.Evaluations });
    }

    [HttpPost, Authorize(Roles = RolesKloroFil.Saisie)]
    public async Task<IActionResult> Exigence(int? id, ExigenceVm vm)
    {
        var texte = await db.Textes.FindAsync(vm.Exigence.TexteReglementaireId);
        if (texte is null) return NotFound();
        if (id is null && !User.PeutGerer()) return Forbid();

        var exigence = id is null
            ? new Exigence { TexteReglementaireId = texte.Id }
            : await db.Exigences.Include(x => x.Evaluations).FirstOrDefaultAsync(x => x.Id == id && x.TexteReglementaireId == texte.Id);
        if (exigence is null) return NotFound();

        // Seuls les gestionnaires modifient le libellé de l'exigence ; un contributeur n'évalue que.
        if (!User.PeutGerer())
        {
            vm.Exigence.Article = exigence.Article;
            vm.Exigence.Description = exigence.Description;
            ModelState.Remove("Exigence.Description");
        }
        if (!ModelState.IsValid)
        {
            vm.TitreTexte = texte.Titre;
            vm.Evaluations = exigence.Evaluations;
            return View(vm);
        }

        exigence.Article = vm.Exigence.Article;
        exigence.Description = vm.Exigence.Description;
        if (id is null) db.Exigences.Add(exigence);

        var evaluation = exigence.Evaluation(vm.Phase);
        if (evaluation is null)
        {
            evaluation = new EvaluationExigence { Phase = vm.Phase };
            exigence.Evaluations.Add(evaluation);
        }
        var saisie = vm.Evaluation;
        evaluation.Conformite = saisie.Conformite;
        evaluation.DateEvaluation = saisie.DateEvaluation;
        evaluation.EvaluePar = saisie.EvaluePar;
        evaluation.CriteresEvaluation = saisie.CriteresEvaluation;
        evaluation.MoyensSurveillance = saisie.MoyensSurveillance;
        evaluation.ActionsCorrectives = saisie.ActionsCorrectives;
        evaluation.Preuves = saisie.Preuves;
        if (evaluation.Conformite != Conformite.EnCoursAnalyse)
        {
            evaluation.DateEvaluation ??= DateTime.Today;
            if (string.IsNullOrWhiteSpace(evaluation.EvaluePar)) evaluation.EvaluePar = User.Identity?.Name;
        }
        exigence.Synthetiser();
        await db.SaveChangesAsync();

        TempData["Succes"] = $"Évaluation « {vm.Phase.Libelle()} » enregistrée.";
        if (evaluation.Conformite is Conformite.NonConforme or Conformite.NcEnCours &&
            !await db.Actions.AnyAsync(a => a.ExigenceId == exigence.Id && (a.Statut == StatutAction.AFaire || a.Statut == StatutAction.EnCours)))
            TempData["Info"] = "Exigence non conforme sans action ouverte dans le plan d'actions : pensez à en créer une (bouton « + Action »).";
        return RedirectToAction(nameof(Details), new { id = texte.Id });
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
        var requete = db.Exigences.AsNoTracking().Include(e => e.Texte).Include(e => e.Evaluations).Where(e => e.Texte!.Statut == StatutVeille.Applicable);
        if (conformite is not null) requete = requete.Where(e => e.Conformite == conformite);
        ViewBag.Conformite = conformite;
        return View(await requete.OrderBy(e => e.Conformite == Conformite.Conforme)
            .ThenBy(e => e.Texte!.Titre).ThenBy(e => e.Id).ToListAsync());
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

public partial class TextesController
{
    [System.Text.RegularExpressions.GeneratedRegex(@"publiée? au Bulletin officiel", System.Text.RegularExpressions.RegexOptions.IgnoreCase)]
    private static partial System.Text.RegularExpressions.Regex AnnexeNonReproduite();
}
