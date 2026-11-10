using System.Diagnostics;
using KloroFil.Web.Data;
using KloroFil.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KloroFil.Web.Controllers;

public class TableauDeBordVm
{
    public int Sites { get; set; }
    public int Classements { get; set; }
    public int TextesAAnalyser { get; set; }
    public int TextesApplicables { get; set; }
    /// <summary>Avancement des évaluations sur l'ensemble des textes applicables.</summary>
    public Avancement Evaluations { get; set; } = new();
    /// <summary>Textes applicables ayant des exigences, du moins avancé au plus avancé.</summary>
    public List<(TexteReglementaire Texte, Avancement Avancement)> AvancementParTexte { get; set; } = [];
    public int ExigencesEvaluees => Evaluations.Evaluees;
    public int ExigencesConformes => Evaluations.Conformes;
    public int ExigencesNonConformes => Evaluations.NonConformes;
    public int PrescriptionsEnRetard { get; set; }
    public int PrescriptionsAVenir { get; set; }
    public int ActionsOuvertes { get; set; }
    public int ActionsEnRetard { get; set; }
    public int AspectsSignificatifs { get; set; }
    public int ObjectifsEnCours { get; set; }
    public double TauxConformite => ExigencesEvaluees == 0 ? 0 : 100.0 * ExigencesConformes / ExigencesEvaluees;

    public List<TexteReglementaire> DerniersTextes { get; set; } = [];
    public List<Prescription> Echeances { get; set; } = [];
    public List<ActionQse> ActionsUrgentes { get; set; } = [];
    public List<ObjectifEnvironnemental> Objectifs { get; set; } = [];
}

public class HomeController(KloroFilDbContext db) : Controller
{
    public async Task<IActionResult> Index()
    {
        var aujourdhui = DateTime.Today;
        var horizon = aujourdhui.AddDays(60);
        var ouvertes = new[] { StatutAction.AFaire, StatutAction.EnCours };

        var vm = new TableauDeBordVm
        {
            Sites = await db.Sites.CountAsync(),
            Classements = await db.Classements.CountAsync(),
            TextesAAnalyser = await db.Textes.CountAsync(t => t.Statut == StatutVeille.AAnalyser),
            TextesApplicables = await db.Textes.CountAsync(t => t.Statut == StatutVeille.Applicable),
            PrescriptionsEnRetard = await db.Prescriptions.CountAsync(p => p.ProchaineEcheance < aujourdhui),
            PrescriptionsAVenir = await db.Prescriptions.CountAsync(p => p.ProchaineEcheance >= aujourdhui && p.ProchaineEcheance <= horizon),
            ActionsOuvertes = await db.Actions.CountAsync(a => ouvertes.Contains(a.Statut)),
            ActionsEnRetard = await db.Actions.CountAsync(a => ouvertes.Contains(a.Statut) && a.Echeance < aujourdhui),
            ObjectifsEnCours = await db.Objectifs.CountAsync(o => o.Statut == StatutObjectif.EnCours),
            DerniersTextes = await db.Textes.AsNoTracking().OrderByDescending(t => t.DateDetection).Take(6).ToListAsync(),
            Echeances = await db.Prescriptions.AsNoTracking().Include(p => p.Site)
                .Where(p => p.ProchaineEcheance != null && p.ProchaineEcheance <= horizon)
                .OrderBy(p => p.ProchaineEcheance).Take(8).ToListAsync(),
            ActionsUrgentes = await db.Actions.AsNoTracking()
                .Where(a => ouvertes.Contains(a.Statut))
                .OrderBy(a => a.Echeance == null).ThenBy(a => a.Echeance).ThenByDescending(a => a.Priorite)
                .Take(8).ToListAsync(),
            Objectifs = await db.Objectifs.AsNoTracking().Where(o => o.Statut == StatutObjectif.EnCours)
                .OrderBy(o => o.Echeance).Take(6).ToListAsync()
        };

        // Avancement des évaluations des textes applicables. Taux de conformité : exigences évaluées hors « Non concerné » ;
        // « À surveiller » compte comme conforme, « NC en cours » comme non conforme.
        var parTexte = await Avancement.ParTexteAsync(db.Exigences.Where(e => e.Texte!.Statut == StatutVeille.Applicable));
        vm.Evaluations = Avancement.Cumuler(parTexte.Values);
        var textes = await db.Textes.AsNoTracking().Where(t => parTexte.Keys.Contains(t.Id)).ToListAsync();
        vm.AvancementParTexte = textes.Select(t => (t, parTexte[t.Id]))
            .OrderBy(x => x.Item2.PourcentLegislation).ThenBy(x => x.Item2.PourcentRecepisse).ThenBy(x => x.t.Titre)
            .ToList();

        // La criticité est calculée en mémoire (propriété non stockée).
        vm.AspectsSignificatifs = (await db.Aspects.AsNoTracking().ToListAsync()).Count(a => a.Significatif);
        return View(vm);
    }

    [AllowAnonymous]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error() => View(model: Activity.Current?.Id ?? HttpContext.TraceIdentifier);
}
