using Microsoft.EntityFrameworkCore;

namespace KloroFil.Web.Models;

/// <summary>
/// Avancement des évaluations d'un ensemble d'exigences (un texte, ou tous les textes applicables).
/// Une phase est « évaluée » dès que son statut n'est plus « En cours d'analyse » (« Non concerné » compte comme évalué).
/// La répartition porte sur la conformité de synthèse de chaque exigence.
/// </summary>
public class Avancement
{
    public int Total { get; set; }
    public int Legislation { get; set; }
    public int Recepisse { get; set; }
    public Dictionary<Conformite, int> Repartition { get; set; } = [];

    public int Nombre(Conformite c) => Repartition.GetValueOrDefault(c);
    public static int Pourcent(int n, int total) => total == 0 ? 0 : (int)Math.Round(100.0 * n / total);
    public int PourcentLegislation => Pourcent(Legislation, Total);
    public int PourcentRecepisse => Pourcent(Recepisse, Total);

    /// <summary>Exigences dont la conformité est connue, hors « Non concerné » (base du taux de conformité).</summary>
    public int Evaluees => Total - Nombre(Conformite.EnCoursAnalyse) - Nombre(Conformite.NonConcerne);
    public int Conformes => Nombre(Conformite.Conforme) + Nombre(Conformite.ASurveiller);
    public int NonConformes => Nombre(Conformite.NonConforme) + Nombre(Conformite.NcEnCours);
    public int TauxConformite => Pourcent(Conformes, Evaluees);

    /// <summary>Calcul en mémoire (exigences chargées avec leurs évaluations).</summary>
    public static Avancement Calculer(IEnumerable<Exigence> exigences)
    {
        var liste = exigences.ToList();
        return new Avancement
        {
            Total = liste.Count,
            Legislation = liste.Count(e => e.Evaluations.Any(v => v.Phase == PhaseEvaluation.Legislation && v.Conformite != Conformite.EnCoursAnalyse)),
            Recepisse = liste.Count(e => e.Evaluations.Any(v => v.Phase == PhaseEvaluation.Recepisse && v.Conformite != Conformite.EnCoursAnalyse)),
            Repartition = liste.GroupBy(e => e.Conformite).ToDictionary(g => g.Key, g => g.Count())
        };
    }

    /// <summary>Calcul en base, regroupé par texte.</summary>
    public static async Task<Dictionary<int, Avancement>> ParTexteAsync(IQueryable<Exigence> exigences, CancellationToken ct = default)
    {
        var lignes = await exigences
            .GroupBy(e => new { e.TexteReglementaireId, e.Conformite })
            .Select(g => new
            {
                g.Key.TexteReglementaireId,
                g.Key.Conformite,
                Nombre = g.Count(),
                Legislation = g.Count(e => e.Evaluations.Any(v => v.Phase == PhaseEvaluation.Legislation && v.Conformite != Conformite.EnCoursAnalyse)),
                Recepisse = g.Count(e => e.Evaluations.Any(v => v.Phase == PhaseEvaluation.Recepisse && v.Conformite != Conformite.EnCoursAnalyse))
            })
            .ToListAsync(ct);

        return lignes.GroupBy(l => l.TexteReglementaireId).ToDictionary(g => g.Key, g => new Avancement
        {
            Total = g.Sum(l => l.Nombre),
            Legislation = g.Sum(l => l.Legislation),
            Recepisse = g.Sum(l => l.Recepisse),
            Repartition = g.ToDictionary(l => l.Conformite, l => l.Nombre)
        });
    }

    /// <summary>Somme de plusieurs avancements (ex. tous les textes applicables).</summary>
    public static Avancement Cumuler(IEnumerable<Avancement> avancements)
    {
        var a = new Avancement();
        foreach (var x in avancements)
        {
            a.Total += x.Total;
            a.Legislation += x.Legislation;
            a.Recepisse += x.Recepisse;
            foreach (var (c, n) in x.Repartition) a.Repartition[c] = a.Repartition.GetValueOrDefault(c) + n;
        }
        return a;
    }
}
