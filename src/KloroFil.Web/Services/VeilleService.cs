using System.Globalization;
using System.Text.RegularExpressions;
using KloroFil.Web.Data;
using KloroFil.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace KloroFil.Web.Services;

public record ResultatVeille(int SourcesLues, int TextesAjoutes, List<string> Erreurs);

/// <summary>
/// Interroge les sources de veille actives, filtre les nouveautés par mots-clés et les enregistre
/// comme textes « À analyser ». Les rubriques ICPE du parc citées dans le texte sont repérées automatiquement.
/// </summary>
public class VeilleService(KloroFilDbContext db, LecteurFlux lecteur, LegifranceClient legifrance, IConfiguration config, ILogger<VeilleService> journal)
{
    private static readonly CompareInfo Comparaison = CultureInfo.GetCultureInfo("fr-FR").CompareInfo;
    private const CompareOptions SansCasseNiAccent = CompareOptions.IgnoreCase | CompareOptions.IgnoreNonSpace;

    /// <summary>
    /// Textes administratifs sans portée réglementaire qui citent les ICPE dans leur contenu (offres d'emploi d'inspecteurs,
    /// nominations, concours…). Un titre contenant l'une de ces expressions est écarté. Remplaçable par Veille:Exclusions.
    /// </summary>
    public static readonly string[] ExclusionsParDefaut =
    [
        "avis de vacance", "vacance d'emploi", "vacance de poste", "vacance d'un emploi",
        "avis de concours", "concours externe", "concours interne", "examen professionnel", "recrutement",
        "portant nomination", "nomination", "cessation de fonctions", "tableau d'avancement", "liste d'aptitude",
        "titularisation", "admission à la retraite", "composition du jury", "Documents et publications",
        "ordre indemnitaire", "régime indemnitaire", "délégation de signature", "liste des emplois", "baccalauréat"
    ];

    private string[] Exclusions =>
        config.GetSection("Veille:Exclusions").Get<string[]>() is { Length: > 0 } e ? e : ExclusionsParDefaut;

    /// <summary>Expression d'exclusion présente dans le titre (sans tenir compte des accents, majuscules et apostrophes typographiques), sinon null.</summary>
    public string? Exclusion(string titre)
    {
        var t = titre.Replace('’', '\'');
        return Exclusions.FirstOrDefault(x => !string.IsNullOrWhiteSpace(x) && Contient(t, x.Replace('’', '\'').Trim()));
    }

    public async Task<ResultatVeille> LancerAsync(int? sourceId = null, CancellationToken ct = default)
    {
        await ReclasserHorsSujetAsync(ct);

        var sources = await db.SourcesVeille
            .Where(s => sourceId == null ? s.Active : s.Id == sourceId)
            .ToListAsync(ct);
        var rubriques = await db.Classements.Select(c => c.Rubrique).Distinct().ToListAsync(ct);

        var ajoutes = 0;
        var erreurs = new List<string>();
        foreach (var source in sources)
        {
            try
            {
                var (lus, retenus, n, fluxDetecte, ecartes) = await LireSourceAsync(source, rubriques, ct);
                ajoutes += n;
                var resultat = $"{lus} élément(s) lu(s), {retenus} retenu(s) par les mots-clés"
                    + (ecartes > 0 ? $", {ecartes} écarté(s) car hors sujet (nominations, avis de vacance…)" : "")
                    + $", {n} nouveau(x) texte(s).";
                if (fluxDetecte is not null) resultat = $"Flux détecté dans la page et enregistré : {fluxDetecte}. " + resultat;
                source.DernierResultat = Tronquer(resultat, 500);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                journal.LogWarning(ex, "Lecture de la source de veille {Source} impossible", source.Nom);
                source.DernierResultat = Tronquer("Erreur : " + ex.Message, 500);
                erreurs.Add($"{source.Nom} : {ex.Message}");
            }
            source.DerniereLecture = DateTime.Now;
            await db.SaveChangesAsync(ct);
        }

        return new ResultatVeille(sources.Count, ajoutes, erreurs);
    }

    /// <summary>
    /// Textes déjà importés et encore « À analyser » dont le titre correspond à une exclusion : classés « Non applicable »
    /// avec la raison (rien n'est supprimé ; un texte déjà analysé n'est jamais modifié).
    /// </summary>
    public async Task<int> ReclasserHorsSujetAsync(CancellationToken ct = default)
    {
        var enAttente = await db.Textes.Where(t => t.Statut == StatutVeille.AAnalyser && !t.Exigences.Any()).ToListAsync(ct);
        var n = 0;
        foreach (var t in enAttente)
        {
            if (Exclusion(t.Titre) is not { } motif) continue;
            Ecarter(t, motif);
            n++;
        }
        if (n > 0)
        {
            await db.SaveChangesAsync(ct);
            journal.LogInformation("Veille : {N} texte(s) hors sujet reclassé(s) « Non applicable »", n);
        }
        return n;
    }

    private static void Ecarter(TexteReglementaire t, string motif)
    {
        t.Statut = StatutVeille.NonApplicable;
        t.Analyse = $"Écarté automatiquement par KloroFil : texte administratif hors sujet (« {motif} »).";
        t.AnalyseLe = DateTime.Now;
        t.AnalysePar = "KloroFil";
    }

    private async Task<(int Lus, int Retenus, int Ajoutes, string? FluxDetecte, int Ecartes)> LireSourceAsync(SourceVeille source, List<string> rubriques, CancellationToken ct)
    {
        var motsCles = (source.MotsCles ?? "")
            .Split([',', ';', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        // Légifrance refuse les robots : ses textes sont lus par l'API officielle (PISTE), déjà filtrée par les mots-clés.
        var parApi = LegifranceClient.EstLegifrance(source.Url);
        var lecture = parApi
            ? new LectureFlux(await legifrance.RechercherJorfAsync(motsCles, await DebutRechercheAsync(source, ct), ct), source.Url)
            : await lecteur.LireAsync(source.Url, ct);
        var elements = lecture.Elements;

        // L'adresse saisie était une page web : on retient le flux trouvé pour les prochaines lectures.
        string? fluxDetecte = null;
        if (!string.Equals(lecture.UrlFlux, source.Url, StringComparison.OrdinalIgnoreCase)
            && lecture.UrlFlux.Length <= 1000
            && !await db.SourcesVeille.AnyAsync(s => s.Url == lecture.UrlFlux && s.Id != source.Id, ct))
        {
            source.Url = lecture.UrlFlux;
            fluxDetecte = lecture.UrlFlux;
        }

        var pertinents = elements
            .Where(e => parApi || motsCles.Length == 0 || motsCles.Any(m => Contient(e.Titre + " " + e.Resume, m)))
            .Where(e => !string.IsNullOrWhiteSpace(e.Lien))
            .ToList();
        // Les textes hors sujet ne sont pas importés.
        var retenus = pertinents.Where(e => Exclusion(e.Titre) is null).ToList();
        var ecartes = pertinents.Count - retenus.Count;
        if (retenus.Count == 0) return (elements.Count, pertinents.Count, 0, fluxDetecte, ecartes);

        var liens = retenus.Select(e => Tronquer(e.Lien!, 1000)).ToList();
        var dejaConnus = (await db.Textes.Where(t => t.Url != null && liens.Contains(t.Url)).Select(t => t.Url!).ToListAsync(ct))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var ajoutes = 0;
        foreach (var e in retenus)
        {
            var lien = Tronquer(e.Lien!, 1000);
            if (!dejaConnus.Add(lien)) continue;

            var contenu = e.Titre + " " + e.Resume;
            var concernees = rubriques.Where(r => Regex.IsMatch(contenu, $@"(?<!\d){Regex.Escape(r)}(?!\d)")).ToList();

            db.Textes.Add(new TexteReglementaire
            {
                Titre = Tronquer(e.Titre, 600),
                Resume = e.Resume,
                Reference = e.Reference is null ? null : Tronquer(e.Reference, 100),
                Url = lien,
                DatePublication = e.Date?.Date,
                Type = DevinerType(e.Titre),
                Domaine = source.Domaine,
                Statut = StatutVeille.AAnalyser,
                RubriquesConcernees = concernees.Count == 0 ? null : Tronquer(string.Join(", ", concernees), 200),
                SourceVeilleId = source.Id,
                DateDetection = DateTime.Now
            });
            ajoutes++;
        }
        return (elements.Count, pertinents.Count, ajoutes, fluxDetecte, ecartes);
    }

    /// <summary>
    /// Début de la période interrogée : toujours les Veille:ProfondeurJours derniers jours (365 par défaut).
    /// Les textes déjà connus sont ignorés ; une lecture en erreur ou un mot-clé ajouté ne fait donc rien manquer.
    /// </summary>
    private Task<DateTime> DebutRechercheAsync(SourceVeille source, CancellationToken ct) =>
        Task.FromResult(DateTime.Today.AddDays(-Math.Max(1, config.GetValue("Veille:ProfondeurJours", 365))));

    public static TypeTexte DevinerType(string titre)
    {
        bool Commence(string s) => Comparaison.IsPrefix(titre.TrimStart(), s, SansCasseNiAccent);
        if (Commence("Arrêté préfectoral")) return TypeTexte.ArretePrefectoral;
        if (Commence("Arrêté")) return TypeTexte.ArreteMinisteriel;
        if (Commence("Décret")) return TypeTexte.Decret;
        if (Commence("Loi") || Commence("Ordonnance")) return TypeTexte.Loi;
        if (Commence("Directive") || Commence("Règlement") || Commence("Décision (UE)")) return TypeTexte.Europeen;
        if (Commence("Circulaire") || Commence("Instruction") || Commence("Note")) return TypeTexte.Circulaire;
        return TypeTexte.Autre;
    }

    private static bool Contient(string texte, string motCle) => Comparaison.IndexOf(texte, motCle, SansCasseNiAccent) >= 0;

    private static string Tronquer(string s, int max) => s.Length <= max ? s : s[..max];
}

/// <summary>Lance la veille automatiquement à intervalle régulier (Veille:IntervalleHeures, 0 = désactivé).</summary>
public class VeillePlanifiee(IServiceScopeFactory scopes, IConfiguration config, ILogger<VeillePlanifiee> journal) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        var heures = config.GetValue("Veille:IntervalleHeures", 24.0);
        if (heures <= 0) return;

        await Task.Delay(TimeSpan.FromMinutes(config.GetValue("Veille:DelaiDemarrageMinutes", 2.0)), ct);
        using var minuterie = new PeriodicTimer(TimeSpan.FromHours(heures));
        do
        {
            try
            {
                using var scope = scopes.CreateScope();
                var r = await scope.ServiceProvider.GetRequiredService<VeilleService>().LancerAsync(ct: ct);
                journal.LogInformation("Veille : {Sources} source(s) lue(s), {Textes} nouveau(x) texte(s)", r.SourcesLues, r.TextesAjoutes);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                journal.LogError(ex, "Échec de la veille planifiée");
            }
        } while (await minuterie.WaitForNextTickAsync(ct));
    }
}
