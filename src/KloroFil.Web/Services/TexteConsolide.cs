using System.Net;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace KloroFil.Web.Services;

/// <summary>Un point d'un texte consolidé : un article, ou un paragraphe numéroté d'une annexe (« Annexe I – 5.1 »).</summary>
public record PointTexte(string Numero, string? Titre, string Contenu)
{
    /// <summary>Texte de l'exigence : intitulé du point puis son contenu.</summary>
    public string Description => string.IsNullOrWhiteSpace(Titre) ? Contenu : Titre + "\n" + Contenu;
}

/// <summary>
/// Découpe la réponse de /consult/lawDecree (version en vigueur d'un texte) en points à évaluer :
/// chaque article en vigueur, et pour les annexes chaque paragraphe numéroté (1.1, 1.1.1…), les intitulés sans contenu
/// propre servant de contexte au point suivant. Les articles abrogés et l'article d'exécution sont ignorés.
/// </summary>
public static partial class TexteConsolide
{
    public static List<PointTexte> Decouper(JsonNode? texte)
    {
        var points = new List<PointTexte>();
        if (texte is null) return points;
        Parcourir(texte, points);
        return points;
    }

    private static void Parcourir(JsonNode noeud, List<PointTexte> points)
    {
        // Articles et sections d'un même niveau, dans l'ordre du texte (intOrdre).
        var elements = new List<(long Ordre, JsonObject Objet, bool EstSection)>();
        if (noeud["articles"] is JsonArray articles)
            elements.AddRange(articles.OfType<JsonObject>().Select(a => (Ordre(a), a, false)));
        if (noeud["sections"] is JsonArray sections)
            elements.AddRange(sections.OfType<JsonObject>().Select(s => (Ordre(s), s, true)));

        foreach (var (_, objet, estSection) in elements.OrderBy(e => e.Ordre))
        {
            if (!EnVigueur(objet)) continue;
            if (estSection) Parcourir(objet, points);
            else AjouterArticle(objet, points);
        }
    }

    private static void AjouterArticle(JsonObject article, List<PointTexte> points)
    {
        var num = Texte(article["num"])?.Trim() ?? "";
        var paragraphes = Paragraphes(Texte(article["content"]));
        if (paragraphes.Count == 0) return;
        var contenu = string.Join("\n", paragraphes);
        if (ArticleExecution().IsMatch(contenu)) return;

        var estAnnexe = num.StartsWith("Annexe", StringComparison.OrdinalIgnoreCase);
        var libelle = string.IsNullOrEmpty(num) ? "Article" : estAnnexe ? num : "Art. " + num;
        if (!estAnnexe || !paragraphes.Any(EstIntitule))
        {
            points.Add(new PointTexte(libelle, null, contenu));
            return;
        }

        // Annexe : un point par paragraphe numéroté ; les intitulés successifs sans contenu (« 1.1. Conformité… »)
        // forment le contexte du point suivant (« 1.1.1. Conformité à la déclaration »).
        string? numero = null, titre = null;
        var contexte = new List<string>();
        var corps = new List<string>();
        void Fermer()
        {
            if (numero is null)
            {
                if (corps.Count > 0) points.Add(new PointTexte(libelle, null, string.Join("\n", corps)));
            }
            else if (corps.Count > 0)
            {
                var intitule = string.Join(" › ", contexte.Append(titre!));
                points.Add(new PointTexte($"{libelle} – {numero}", intitule, string.Join("\n", corps)));
            }
            else if (!titre!.StartsWith('(')) contexte.Add($"{numero}. {titre}");   // « 4.3. (1) » : point du modèle non utilisé
            corps.Clear();
        }

        foreach (var p in paragraphes)
        {
            var m = Intitule().Match(p);
            if (EstIntitule(p))
            {
                Fermer();
                numero = m.Groups["num"].Value.TrimEnd('.');
                titre = m.Groups["titre"].Value.Trim();
                // Un intitulé de niveau supérieur remplace le contexte des niveaux inférieurs.
                contexte.RemoveAll(c => Niveau(c) >= Niveau(numero));
            }
            else if (!NoteDeBasDePage().IsMatch(p)) corps.Add(p);
        }
        Fermer();
    }

    private static int Niveau(string numero) => numero.Split(' ')[0].TrimEnd('.').Count(c => c == '.') + 1;

    /// <summary>« 5.1. Prélèvements » : numéro hiérarchique suivi d'un intitulé court sans ponctuation finale.</summary>
    private static bool EstIntitule(string p) =>
        p.Length <= 160 && Intitule().IsMatch(p) && !p.TrimEnd().EndsWith('.') && !p.TrimEnd().EndsWith(':') && !p.TrimEnd().EndsWith(';');

    private static List<string> Paragraphes(string? html)
    {
        if (string.IsNullOrWhiteSpace(html)) return [];
        var texte = Retour().Replace(html, "\n");
        texte = FinParagraphe().Replace(texte, "\n\n");
        texte = WebUtility.HtmlDecode(Balises().Replace(texte, ""));
        return texte.Split("\n\n", StringSplitOptions.RemoveEmptyEntries)
            .Select(p => Lignes().Replace(Espaces().Replace(p, " "), "\n").Trim())
            .Where(p => p.Length > 0)
            .ToList();
    }

    private static bool EnVigueur(JsonObject o) => Texte(o["etat"]) is null or "" or "VIGUEUR" or "VIGUEUR_DIFF";
    private static long Ordre(JsonObject o) => o["intOrdre"] is JsonValue v && v.TryGetValue<long>(out var n) ? n : long.MaxValue;
    private static string? Texte(JsonNode? n) => n is JsonValue v && v.TryGetValue<string>(out var s) ? s : null;

    /// <summary>« 5.1. Titre », « 5.1 Titre » ou « 5. Titre » ; le titre commence par une majuscule ou est un renvoi « (1) ».
    /// Exclut les cellules de tableau (« 4 dB (A) »).</summary>
    [GeneratedRegex(@"^(?<num>\d+(?:\.\d+)+\.?|\d+\.)\s+(?<titre>(?:\p{Lu}|\(\d+\)).*)$")] private static partial Regex Intitule();

    /// <summary>Note de bas de page du modèle d'arrêté : « (1) Un modèle a été constitué… ».</summary>
    [GeneratedRegex(@"^\(\d+\)\s")] private static partial Regex NoteDeBasDePage();
    [GeneratedRegex(@"chargée?s? de l'exécution du présent (arrêté|décret)", RegexOptions.IgnoreCase)] private static partial Regex ArticleExecution();
    [GeneratedRegex(@"<br\s*/?>", RegexOptions.IgnoreCase)] private static partial Regex Retour();
    [GeneratedRegex(@"</p>|</div>|</li>|</h\d>|</tr>", RegexOptions.IgnoreCase)] private static partial Regex FinParagraphe();
    [GeneratedRegex("<[^>]+>")] private static partial Regex Balises();
    [GeneratedRegex(@"[ \t ]+")] private static partial Regex Espaces();
    [GeneratedRegex(@"\s*\n\s*")] private static partial Regex Lignes();
}
