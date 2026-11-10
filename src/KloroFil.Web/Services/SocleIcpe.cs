using System.Text.RegularExpressions;
using KloroFil.Web.Models;

namespace KloroFil.Web.Services;

/// <summary>
/// Texte commun à toutes les ICPE d'un régime donné, dont le titre ne cite pas de rubrique.
/// <paramref name="Termes"/> : expressions exigées dans le titre (Légifrance, recherche exacte) ; la recherche ramène aussi
/// les arrêtés modificatifs, qui reprennent le titre du texte modifié.
/// </summary>
public record EntreeSocle(string Libelle, string[] Termes, RegimeIcpe RegimeMinimum, bool SevesoSeulement = false, int? DepuisAnnees = null);

public static partial class SocleIcpe
{
    /// <summary>
    /// Socle réglementaire commun, vérifié sur l'API Légifrance (novembre 2026). Régime minimum : le texte est recherché si l'un
    /// des sites atteint ce régime (Déclaration = tous les sites classés).
    /// </summary>
    public static readonly EntreeSocle[] Textes =
    [
        new("Bruit des installations classées (AM du 23 janvier 1997)", ["23 janvier 1997", "bruits"], RegimeIcpe.Declaration),
        new("Registres déchets, terres excavées et sédiments (AM du 31 mai 2021)", ["registres déchets"], RegimeIcpe.Declaration),
        new("Sols pollués et cessation d'activité", ["cessation d'activité", "installations classées"], RegimeIcpe.Declaration),
        new("Nomenclature des installations classées (5 dernières années)", ["nomenclature des installations classées"], RegimeIcpe.Declaration, DepuisAnnees: 5),
        new("Contrôle périodique des installations soumises à déclaration avec contrôle", ["contrôle périodique", "installations classées"], RegimeIcpe.DeclarationControle),
        new("Déclaration annuelle des émissions – GEREP (AM du 31 janvier 2008)", ["31 janvier 2008", "déclaration annuelle des émissions"], RegimeIcpe.Enregistrement),
        new("Garanties financières (AM du 31 mai 2012)", ["31 mai 2012", "garanties financières"], RegimeIcpe.Enregistrement),
        new("Prélèvements d'eau et émissions des installations soumises à autorisation (AM du 2 février 1998)", ["2 février 1998", "prélèvements"], RegimeIcpe.Autorisation),
        new("Prévention des risques accidentels (AM du 4 octobre 2010)", ["4 octobre 2010", "risques accidentels"], RegimeIcpe.Autorisation),
        new("Prévention des accidents majeurs – Seveso (AM du 26 mai 2014)", ["26 mai 2014", "accidents majeurs"], RegimeIcpe.Autorisation, SevesoSeulement: true)
    ];

    /// <summary>Textes du socle à rechercher pour le régime le plus élevé du parc (au moins la déclaration).</summary>
    public static IEnumerable<EntreeSocle> Applicables(RegimeIcpe regimeMax, bool seveso) =>
        Textes.Where(t => regimeMax >= t.RegimeMinimum && (!t.SevesoSeulement || seveso));

    /// <summary>
    /// Termes de recherche des arrêtés modifiant un arrêté donné : « modifiant », « arrêté du JJ mois AAAA » et le début de son objet
    /// (les modificatifs reprennent le titre du texte modifié ; l'objet écarte les autres arrêtés de même date). Null si le titre
    /// n'est pas celui d'un arrêté de base (ex. un arrêté lui-même modificatif).
    /// </summary>
    public static string[]? TermesModifications(string titre)
    {
        var m = TitreArrete().Match(Espaces().Replace(titre.Replace('’', '\''), " ").Trim());
        if (!m.Success) return null;
        var objet = m.Groups["objet"].Value.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (objet.Length < 3 || objet[0].StartsWith("modifiant", StringComparison.OrdinalIgnoreCase)
            || objet[0].Equals("portant", StringComparison.OrdinalIgnoreCase) && objet[1].StartsWith("modification", StringComparison.OrdinalIgnoreCase))
            return null;
        var debutObjet = string.Join(' ', objet.Take(5)).TrimEnd(',', ';', '.', ':');
        return ["modifiant", "arrêté du " + m.Groups["date"].Value, debutObjet];
    }

    [GeneratedRegex(@"^Arrêté (?:ministériel )?du (?<date>(?:1er|\d{1,2}) \p{L}+ \d{4}) (?<objet>.+)$", RegexOptions.IgnoreCase)]
    private static partial Regex TitreArrete();

    [GeneratedRegex(@"\s+")] private static partial Regex Espaces();
}
