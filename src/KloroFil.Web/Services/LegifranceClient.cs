using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace KloroFil.Web.Services;

/// <summary>
/// Recherche des textes publiés au Journal officiel via l'API Légifrance de la DILA (portail PISTE).
/// Configuration (User Secrets), section « Piste » :
///   ClientId, ClientSecret (obligatoires) ; Sandbox (true = bac à sable) ;
///   TokenUrl / TokenUrlSandbox et ApiBaseUrl / ApiBaseUrlSandbox (facultatifs, valeurs PISTE par défaut).
/// </summary>
public partial class LegifranceClient(IHttpClientFactory clients, IConfiguration config, ILogger<LegifranceClient> journal)
{
    public const string NomClient = "piste";
    private const int TailleMaxResume = 4000;
    private const string CheminApi = "/dila/legifrance/lf-engine-app";

    private static readonly SemaphoreSlim Verrou = new(1, 1);
    private static string? _jeton;
    private static string? _jetonPour;   // URL du serveur qui a délivré le jeton (production ou bac à sable)
    private static DateTime _expiration;

    private bool BacASable => config.GetValue<bool?>("Piste:Sandbox") ?? config.GetValue("Piste:BacASable", false);

    private string UrlJeton => Valeur(BacASable ? "Piste:TokenUrlSandbox" : "Piste:TokenUrl")
        ?? (BacASable ? "https://sandbox-oauth.piste.gouv.fr/api/oauth/token" : "https://oauth.piste.gouv.fr/api/oauth/token");

    /// <summary>Adresse de base de l'API ; le chemin Légifrance est ajouté s'il n'est pas déjà dans la configuration.</summary>
    private string UrlApi
    {
        get
        {
            var baseUrl = (Valeur(BacASable ? "Piste:ApiBaseUrlSandbox" : "Piste:ApiBaseUrl")
                ?? (BacASable ? "https://sandbox-api.piste.gouv.fr" : "https://api.piste.gouv.fr")).TrimEnd('/');
            if (baseUrl.EndsWith("/search", StringComparison.OrdinalIgnoreCase)) baseUrl = baseUrl[..^"/search".Length];
            return baseUrl.Contains("lf-engine-app", StringComparison.OrdinalIgnoreCase) ? baseUrl : baseUrl + CheminApi;
        }
    }

    private string? Valeur(string cle) => string.IsNullOrWhiteSpace(config[cle]) ? null : config[cle]!.Trim();

    public bool EstConfigure => !string.IsNullOrWhiteSpace(config["Piste:ClientId"]) && !string.IsNullOrWhiteSpace(config["Piste:ClientSecret"]);

    /// <summary>Une adresse legifrance.gouv.fr est lue par l'API plutôt que par le site, qui refuse les robots.</summary>
    public static bool EstLegifrance(string url) =>
        Uri.TryCreate(url, UriKind.Absolute, out var u) &&
        (u.Host.Equals("legifrance.gouv.fr", StringComparison.OrdinalIgnoreCase) || u.Host.EndsWith(".legifrance.gouv.fr", StringComparison.OrdinalIgnoreCase));

    /// <summary>Textes du JORF publiés depuis <paramref name="depuis"/> contenant l'un des mots-clés (une recherche par mot-clé).</summary>
    public async Task<List<ElementFlux>> RechercherJorfAsync(IReadOnlyList<string> motsCles, DateTime depuis, CancellationToken ct)
    {
        if (!EstConfigure)
            throw new LectureFluxException(
                "Légifrance est lu par l'API PISTE : renseignez Piste:ClientId et Piste:ClientSecret dans les secrets de l'application (voir le README).");

        var termes = motsCles.Count > 0 ? motsCles : ["installations classées"];
        var resultats = new Dictionary<string, ElementFlux>(StringComparer.OrdinalIgnoreCase);
        foreach (var terme in termes)
            foreach (var e in await RechercherAsync(terme, depuis, ct))
                resultats.TryAdd(e.Lien!, e);

        return resultats.Values.OrderByDescending(e => e.Date).ToList();
    }

    /// <summary>Recherche d'un texte du JORF par son NOR, quelle que soit sa date (pour les textes de référence anciens).</summary>
    public async Task<ElementFlux?> RechercherParNorAsync(string nor, CancellationToken ct)
    {
        if (!EstConfigure)
            throw new LectureFluxException("L'import depuis Légifrance utilise l'API PISTE : renseignez Piste:ClientId et Piste:ClientSecret (voir le README).");
        nor = nor.Trim().ToUpperInvariant();
        var (elements, _) = await RechercherPageAsync("NOR", nor, null, 1, 10, ct);
        return elements.FirstOrDefault(e => string.Equals(e.Reference, nor, StringComparison.OrdinalIgnoreCase)) ?? elements.FirstOrDefault();
    }

    /// <summary>Toutes les pages de résultats d'un mot-clé, dans la limite de Piste:MaxParMotCle (200 par défaut).</summary>
    private async Task<List<ElementFlux>> RechercherAsync(string terme, DateTime depuis, CancellationToken ct)
    {
        const int taillePage = 50;   // maximum accepté par l'API
        var maximum = Math.Max(taillePage, config.GetValue("Piste:MaxParMotCle", 200));
        var tous = new List<ElementFlux>();
        int? total = null;
        for (var page = 1; tous.Count < maximum; page++)
        {
            var (elements, nb) = await RechercherPageAsync("ALL", terme, depuis, page, taillePage, ct);
            total ??= nb;
            tous.AddRange(elements);
            if (elements.Count < taillePage || (total is { } t && tous.Count >= t)) break;
        }

        journal.LogInformation("Légifrance : « {Terme} » depuis le {Depuis} → {Lus} texte(s) lu(s) sur {Total} trouvé(s)",
            terme, depuis.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture), tous.Count, total?.ToString() ?? "?");
        return tous;
    }

    private async Task<(List<ElementFlux> Elements, int? Total)> RechercherPageAsync(string typeChamp, string terme, DateTime? depuis, int page, int taillePage, CancellationToken ct)
    {
        // Filtre de date seulement pour la veille ; une recherche par NOR porte sur tout le JORF.
        var filtres = new JsonArray();
        if (depuis is { } d)
            filtres.Add(new JsonObject
            {
                ["facette"] = "DATE_PUBLICATION",
                ["dates"] = new JsonObject
                {
                    ["start"] = d.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                    ["end"] = DateTime.Today.AddDays(1).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
                }
            });

        var corps = new JsonObject
        {
            ["fond"] = "JORF",
            ["recherche"] = new JsonObject
            {
                ["champs"] = new JsonArray(new JsonObject
                {
                    ["typeChamp"] = typeChamp,
                    ["operateur"] = "ET",
                    ["criteres"] = new JsonArray(new JsonObject
                    {
                        ["valeur"] = terme,
                        ["typeRecherche"] = "EXACTE",
                        ["operateur"] = "ET"
                    })
                }),
                ["filtres"] = filtres,
                ["pageNumber"] = page,
                ["pageSize"] = taillePage,
                ["operateur"] = "ET",
                ["sort"] = "DATE_PUBLI_DESC",
                ["typePagination"] = "DEFAUT"
            }
        };

        var reponse = await EnvoyerAsync(corps, ct);
        if (reponse.StatusCode == HttpStatusCode.Unauthorized)
        {
            // Jeton expiré côté serveur : on en redemande un, une fois.
            _jeton = null;
            reponse.Dispose();
            reponse = await EnvoyerAsync(corps, ct);
        }

        using (reponse)
        {
            if (reponse.StatusCode == HttpStatusCode.Forbidden)
                throw new LectureFluxException("PISTE refuse l'accès (403) : vérifiez que votre application PISTE est abonnée à l'API « Légifrance » et que ses conditions d'utilisation sont acceptées.");
            if (!reponse.IsSuccessStatusCode)
            {
                var detail = await reponse.Content.ReadAsStringAsync(ct);
                journal.LogWarning("Recherche Légifrance en erreur {Code} : {Detail}", (int)reponse.StatusCode, detail);
                throw new LectureFluxException($"L'API Légifrance a répondu par une erreur {(int)reponse.StatusCode}.");
            }

            var json = JsonNode.Parse(await reponse.Content.ReadAsStringAsync(ct));
            int? total = int.TryParse(json?["totalResultNumber"]?.ToString(), out var n) ? n : null;
            return (Analyser(json), total);
        }
    }

    private async Task<HttpResponseMessage> EnvoyerAsync(JsonObject corps, CancellationToken ct)
    {
        var requete = new HttpRequestMessage(HttpMethod.Post, UrlApi + "/search")
        {
            Content = new StringContent(corps.ToJsonString(), System.Text.Encoding.UTF8, "application/json")
        };
        requete.Headers.Authorization = new AuthenticationHeaderValue("Bearer", await JetonAsync(ct));
        requete.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        try
        {
            return await clients.CreateClient(NomClient).SendAsync(requete, ct);
        }
        catch (TaskCanceledException ex) when (!ct.IsCancellationRequested)
        {
            throw new LectureFluxException("L'API Légifrance n'a pas répondu à temps.", ex);
        }
        catch (HttpRequestException ex)
        {
            throw new LectureFluxException("API Légifrance injoignable : " + ex.Message, ex);
        }
    }

    /// <summary>Jeton OAuth 2.0 (client credentials), conservé jusqu'à une minute avant son expiration.</summary>
    private async Task<string> JetonAsync(CancellationToken ct)
    {
        await Verrou.WaitAsync(ct);
        try
        {
            if (_jeton is not null && _jetonPour == UrlJeton && DateTime.UtcNow < _expiration) return _jeton;

            using var reponse = await clients.CreateClient(NomClient).PostAsync(UrlJeton, new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "client_credentials",
                ["client_id"] = config["Piste:ClientId"]!,
                ["client_secret"] = config["Piste:ClientSecret"]!,
                ["scope"] = "openid"
            }), ct);
            if (reponse.StatusCode is HttpStatusCode.BadRequest or HttpStatusCode.Unauthorized)
                throw new LectureFluxException("PISTE refuse les identifiants (client_id / client_secret). Vérifiez-les, ainsi que le choix production / bac à sable.");
            if (!reponse.IsSuccessStatusCode)
                throw new LectureFluxException($"Le serveur d'authentification PISTE a répondu par une erreur {(int)reponse.StatusCode}.");

            using var doc = JsonDocument.Parse(await reponse.Content.ReadAsStringAsync(ct));
            _jeton = doc.RootElement.GetProperty("access_token").GetString();
            var duree = doc.RootElement.TryGetProperty("expires_in", out var e) && e.TryGetInt32(out var s) ? s : 3600;
            _expiration = DateTime.UtcNow.AddSeconds(Math.Max(60, duree - 60));
            _jetonPour = UrlJeton;
            return _jeton!;
        }
        catch (HttpRequestException ex)
        {
            throw new LectureFluxException("Serveur d'authentification PISTE injoignable : " + ex.Message, ex);
        }
        finally
        {
            Verrou.Release();
        }
    }

    /// <summary>Lit la réponse de /search : results[].titles[].{id, cid, title}, nature, dates et extraits.</summary>
    private static List<ElementFlux> Analyser(JsonNode? json)
    {
        var liste = new List<ElementFlux>();
        if (json?["results"] is not JsonArray resultats) return liste;

        foreach (var r in resultats.OfType<JsonObject>())
        {
            var titre0 = (r["titles"] as JsonArray)?.OfType<JsonObject>().FirstOrDefault();
            var titre = Texte(titre0?["title"]) ?? Texte(r["title"]);
            var id = Texte(titre0?["cid"]) ?? Texte(titre0?["id"]) ?? Texte(r["id"]);
            if (string.IsNullOrWhiteSpace(titre) || string.IsNullOrWhiteSpace(id)) continue;

            var extraits = (r["sections"] as JsonArray)?.OfType<JsonObject>()
                .SelectMany(s => (s["extracts"] as JsonArray)?.OfType<JsonObject>() ?? [])
                .SelectMany(x => (x["values"] as JsonArray)?.Select(Texte) ?? [])
                .Where(v => !string.IsNullOrWhiteSpace(v))
                .Take(3)
                .ToList() ?? [];
            var resume = Nettoyer(string.Join(" … ", extraits));
            liste.Add(new ElementFlux(
                Nettoyer(titre)!,
                "https://www.legifrance.gouv.fr/jorf/id/" + Uri.EscapeDataString(id),
                resume,
                LireDate(r["datePublication"]) ?? LireDate(r["dateSignature"]) ?? LireDate(r["date"]))
            {
                Reference = Texte(r["nor"])?.Trim()
            });
        }
        return liste;
    }

    private static string? Texte(JsonNode? n) =>
        n is JsonValue v && v.TryGetValue<string>(out var s) && !string.IsNullOrWhiteSpace(s) ? s : null;

    private static DateTime? LireDate(JsonNode? n)
    {
        if (n is not JsonValue v) return null;
        if (v.TryGetValue<long>(out var ms)) return DateTimeOffset.FromUnixTimeMilliseconds(ms).LocalDateTime.Date;
        if (v.TryGetValue<string>(out var s) && DateTime.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces, out var d)) return d.Date;
        return null;
    }

    private static string? Nettoyer(string? texte)
    {
        if (string.IsNullOrWhiteSpace(texte)) return null;
        var brut = Espaces().Replace(WebUtility.HtmlDecode(Balises().Replace(texte, " ")), " ").Trim();
        return brut.Length > TailleMaxResume ? brut[..TailleMaxResume] + "…" : brut;
    }

    [GeneratedRegex("<[^>]+>")] private static partial Regex Balises();
    [GeneratedRegex(@"\s+")] private static partial Regex Espaces();
}
