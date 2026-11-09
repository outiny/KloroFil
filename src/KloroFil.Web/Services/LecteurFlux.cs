using System.Globalization;
using System.Net;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;

namespace KloroFil.Web.Services;

public record ElementFlux(string Titre, string? Lien, string? Resume, DateTime? Date);

/// <summary>Lit un flux RSS 2.0, RSS 1.0 (RDF) ou Atom sans dépendance externe.</summary>
public partial class LecteurFlux(IHttpClientFactory clients)
{
    public const string NomClient = "veille";
    private const int TailleMaxResume = 4000;

    public async Task<List<ElementFlux>> LireAsync(string url, CancellationToken ct)
    {
        using var reponse = await clients.CreateClient(NomClient).GetAsync(url, HttpCompletionOption.ResponseHeadersRead, ct);
        reponse.EnsureSuccessStatusCode();
        await using var flux = await reponse.Content.ReadAsStreamAsync(ct);

        // Pas de DTD ni de ressources externes ; taille bornée.
        var reglages = new XmlReaderSettings
        {
            DtdProcessing = DtdProcessing.Ignore,
            XmlResolver = null,
            MaxCharactersInDocument = 20_000_000,
            Async = true
        };
        using var lecteur = XmlReader.Create(flux, reglages);
        var doc = await XDocument.LoadAsync(lecteur, LoadOptions.None, ct);
        return Analyser(doc);
    }

    public static List<ElementFlux> Analyser(XDocument doc)
    {
        var racine = doc.Root ?? throw new InvalidDataException("Flux vide.");
        var elements = racine.Name.LocalName == "feed"
            ? racine.Elements().Where(e => e.Name.LocalName == "entry").Select(Atom)
            : racine.Descendants().Where(e => e.Name.LocalName == "item").Select(Rss);
        return elements.Where(e => !string.IsNullOrWhiteSpace(e.Titre)).Take(300).ToList();
    }

    private static ElementFlux Rss(XElement item) => new(
        Nettoyer(Enfant(item, "title")) ?? "",
        Enfant(item, "link")?.Trim() ?? Enfant(item, "guid")?.Trim(),
        Nettoyer(Enfant(item, "description") ?? Enfant(item, "encoded")),
        LireDate(Enfant(item, "pubDate") ?? Enfant(item, "date")));

    private static ElementFlux Atom(XElement entree)
    {
        var liens = entree.Elements().Where(e => e.Name.LocalName == "link").ToList();
        var lien = liens.FirstOrDefault(l => (string?)l.Attribute("rel") is null or "alternate") ?? liens.FirstOrDefault();
        return new(
            Nettoyer(Enfant(entree, "title")) ?? "",
            (string?)lien?.Attribute("href"),
            Nettoyer(Enfant(entree, "summary") ?? Enfant(entree, "content")),
            LireDate(Enfant(entree, "published") ?? Enfant(entree, "updated")));
    }

    private static string? Enfant(XElement parent, string nom) =>
        parent.Elements().FirstOrDefault(e => e.Name.LocalName == nom)?.Value;

    /// <summary>Retire les balises HTML et les espaces superflus.</summary>
    private static string? Nettoyer(string? texte)
    {
        if (string.IsNullOrWhiteSpace(texte)) return null;
        var brut = WebUtility.HtmlDecode(Balises().Replace(texte, " "));
        brut = Espaces().Replace(brut, " ").Trim();
        return brut.Length > TailleMaxResume ? brut[..TailleMaxResume] + "…" : brut;
    }

    private static DateTime? LireDate(string? texte)
    {
        if (string.IsNullOrWhiteSpace(texte)) return null;
        texte = texte.Trim();
        if (DateTimeOffset.TryParse(texte, CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces, out var d)) return d.LocalDateTime;
        // RFC 822 avec fuseau nommé (« GMT », « CET »…)
        var sansFuseau = Fuseau().Replace(texte, "");
        return DateTime.TryParse(sansFuseau, CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces, out var d2) ? d2 : null;
    }

    [GeneratedRegex("<[^>]+>")] private static partial Regex Balises();
    [GeneratedRegex(@"\s+")] private static partial Regex Espaces();
    [GeneratedRegex(@"\s+[A-Z]{2,4}$")] private static partial Regex Fuseau();
}
