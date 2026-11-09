using System.Globalization;
using System.Net;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;

namespace KloroFil.Web.Services;

public record ElementFlux(string Titre, string? Lien, string? Resume, DateTime? Date)
{
    /// <summary>Référence officielle (NOR pour Légifrance).</summary>
    public string? Reference { get; init; }
}

/// <summary>Résultat d'une lecture : les éléments et l'adresse du flux réellement lu (peut différer si elle a été détectée dans une page web).</summary>
public record LectureFlux(List<ElementFlux> Elements, string UrlFlux);

/// <summary>Erreur de lecture avec un message compréhensible par l'utilisateur.</summary>
public class LectureFluxException(string message, Exception? cause = null) : Exception(message, cause);

/// <summary>
/// Lit un flux RSS 2.0, RSS 1.0 (RDF) ou Atom sans dépendance externe.
/// Si l'adresse est une page web, cherche le flux qu'elle annonce (balise &lt;link rel="alternate"&gt; ou lien « RSS / flux »).
/// </summary>
public partial class LecteurFlux(IHttpClientFactory clients)
{
    public const string NomClient = "veille";
    private const int TailleMaxResume = 4000;
    private const int TailleMaxReponse = 20_000_000;
    private const int CandidatsMax = 5;

    public async Task<LectureFlux> LireAsync(string url, CancellationToken ct)
    {
        var (contenu, typeContenu) = await TelechargerAsync(url, ct);
        if (EstFlux(contenu)) return new LectureFlux(Analyser(Charger(contenu)), url);

        if (!EstHtml(contenu, typeContenu))
            throw new LectureFluxException("Le contenu reçu n'est ni un flux RSS / Atom ni une page web.");

        // Page web : on essaie les flux qu'elle annonce.
        var candidats = ChercherFlux(contenu, new Uri(url));
        foreach (var candidat in candidats.Take(CandidatsMax))
        {
            try
            {
                var (c, _) = await TelechargerAsync(candidat, ct);
                if (EstFlux(c)) return new LectureFlux(Analyser(Charger(c)), candidat);
            }
            catch (LectureFluxException) { /* candidat suivant */ }
        }

        throw new LectureFluxException(candidats.Count == 0
            ? "Cette adresse est une page web, pas un flux RSS, et elle n'annonce aucun flux. Cherchez sur le site un lien « RSS » ou « Flux » et collez son adresse."
            : "Cette adresse est une page web, pas un flux RSS ; les liens « RSS » trouvés dans la page ne sont pas des flux valides.");
    }

    private async Task<(string Contenu, string? Type)> TelechargerAsync(string url, CancellationToken ct)
    {
        HttpResponseMessage reponse;
        try
        {
            reponse = await clients.CreateClient(NomClient).GetAsync(url, HttpCompletionOption.ResponseHeadersRead, ct);
        }
        catch (TaskCanceledException ex) when (!ct.IsCancellationRequested)
        {
            throw new LectureFluxException("Le site n'a pas répondu à temps.", ex);
        }
        catch (HttpRequestException ex)
        {
            throw new LectureFluxException("Site injoignable : " + ex.Message, ex);
        }

        using (reponse)
        {
            switch (reponse.StatusCode)
            {
                case HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden:
                    throw new LectureFluxException(
                        $"Le site refuse les lectures automatiques (erreur {(int)reponse.StatusCode}). Il ne propose pas de flux public à cette adresse : " +
                        "utilisez un autre flux (ex. INERIS, DREAL, Google Actualités) ou l'API officielle du site.");
                case HttpStatusCode.NotFound or HttpStatusCode.Gone:
                    throw new LectureFluxException($"Adresse introuvable (erreur {(int)reponse.StatusCode}).");
            }
            if (!reponse.IsSuccessStatusCode)
                throw new LectureFluxException($"Le site a répondu par une erreur {(int)reponse.StatusCode}.");
            if (reponse.Content.Headers.ContentLength > TailleMaxReponse)
                throw new LectureFluxException("Le contenu est trop volumineux pour un flux.");

            var contenu = await reponse.Content.ReadAsStringAsync(ct);
            if (contenu.Length > TailleMaxReponse)
                throw new LectureFluxException("Le contenu est trop volumineux pour un flux.");
            return (contenu, reponse.Content.Headers.ContentType?.MediaType);
        }
    }

    private static XDocument Charger(string contenu)
    {
        // Pas de DTD ni de ressources externes ; taille bornée.
        var reglages = new XmlReaderSettings
        {
            DtdProcessing = DtdProcessing.Ignore,
            XmlResolver = null,
            MaxCharactersInDocument = TailleMaxReponse
        };
        try
        {
            using var lecteur = XmlReader.Create(new StringReader(contenu), reglages);
            return XDocument.Load(lecteur);
        }
        catch (XmlException ex)
        {
            throw new LectureFluxException("Le flux est mal formé : " + ex.Message, ex);
        }
    }

    /// <summary>Repère un flux d'après sa balise racine (rss, feed, rdf:RDF).</summary>
    private static bool EstFlux(string contenu)
    {
        var debut = DebutXml().Replace(contenu.TrimStart('﻿', ' ', '\r', '\n', '\t'), "");
        return RacineFlux().IsMatch(debut.Length > 2000 ? debut[..2000] : debut);
    }

    private static bool EstHtml(string contenu, string? type) =>
        type is "text/html" or "application/xhtml+xml" || PageHtml().IsMatch(contenu.Length > 2000 ? contenu[..2000] : contenu);

    /// <summary>Adresses de flux candidates dans une page : d'abord les balises &lt;link&gt; d'autodécouverte, puis les liens « RSS / Atom / flux ».</summary>
    public static List<string> ChercherFlux(string html, Uri basePage)
    {
        var resultat = new List<string>();
        void Ajouter(string? href)
        {
            if (string.IsNullOrWhiteSpace(href)) return;
            if (Uri.TryCreate(basePage, WebUtility.HtmlDecode(href.Trim()), out var u) && u.Scheme is "http" or "https"
                && !resultat.Contains(u.AbsoluteUri))
                resultat.Add(u.AbsoluteUri);
        }

        foreach (Match balise in BaliseLink().Matches(html))
        {
            var attributs = balise.Value;
            if (TypeFlux().IsMatch(attributs)) Ajouter(Attribut(attributs, "href"));
        }
        foreach (Match lien in BaliseA().Matches(html))
        {
            var href = lien.Groups["href"].Value;
            var texte = lien.Groups["texte"].Value;
            if (MotFlux().IsMatch(href) || MotFlux().IsMatch(Balises().Replace(texte, " "))) Ajouter(href);
        }
        return resultat;
    }

    private static string? Attribut(string balise, string nom)
    {
        var m = Regex.Match(balise, nom + @"\s*=\s*(?:""(?<v>[^""]*)""|'(?<v>[^']*)'|(?<v>[^\s>]+))", RegexOptions.IgnoreCase);
        return m.Success ? m.Groups["v"].Value : null;
    }

    [GeneratedRegex(@"^(<\?xml[^>]*\?>|<!--.*?-->|<!DOCTYPE[^>]*>|\s)+", RegexOptions.Singleline | RegexOptions.IgnoreCase)] private static partial Regex DebutXml();
    [GeneratedRegex(@"^<(?:[\w-]+:)?(rss|feed|RDF)\b", RegexOptions.IgnoreCase)] private static partial Regex RacineFlux();
    [GeneratedRegex(@"<!DOCTYPE\s+html|<html\b", RegexOptions.IgnoreCase)] private static partial Regex PageHtml();
    [GeneratedRegex(@"<link\b[^>]*>", RegexOptions.IgnoreCase)] private static partial Regex BaliseLink();
    [GeneratedRegex(@"type\s*=\s*[""']?application/(rss|atom|rdf)\+xml", RegexOptions.IgnoreCase)] private static partial Regex TypeFlux();
    [GeneratedRegex(@"<a\b[^>]*?href\s*=\s*[""'](?<href>[^""']+)[""'][^>]*>(?<texte>.*?)</a>", RegexOptions.IgnoreCase | RegexOptions.Singleline, 2000)] private static partial Regex BaliseA();
    [GeneratedRegex(@"\b(rss|atom|feed|flux[\s-]?rss|flux)\b", RegexOptions.IgnoreCase)] private static partial Regex MotFlux();

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
