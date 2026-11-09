using System.Collections.Concurrent;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Reflection;
using KloroFil.Web.Models;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace KloroFil.Web;

/// <summary>Formats d'affichage en français et libellés des énumérations.</summary>
public static class Affichage
{
    private static readonly CultureInfo Fr = CultureInfo.GetCultureInfo("fr-FR");
    private static readonly ConcurrentDictionary<Enum, string> Libelles = new();

    public static string Date(this DateTime? d) => d?.ToString("dd/MM/yyyy", Fr) ?? "–";
    public static string Date(this DateTime d) => d.ToString("dd/MM/yyyy", Fr);
    public static string DateHeure(this DateTime? d) => d?.ToString("dd/MM/yyyy HH:mm", Fr) ?? "–";
    public static string Texte(this string? s) => string.IsNullOrWhiteSpace(s) ? "–" : s;
    public static string Pourcent(this double v) => v.ToString("0", Fr) + " %";

    public static string Libelle(this Enum valeur) => Libelles.GetOrAdd(valeur, v =>
        v.GetType().GetField(v.ToString())?.GetCustomAttribute<DisplayAttribute>()?.GetName() ?? v.ToString());

    /// <summary>Liste déroulante d'une énumération (libellés français).</summary>
    public static IEnumerable<SelectListItem> Choix<T>(T? selection = null) where T : struct, Enum =>
        Enum.GetValues<T>().Select(v => new SelectListItem(v.Libelle(), v.ToString(), selection.HasValue && v.Equals(selection.Value)));

    public static IEnumerable<SelectListItem> Choix<T>(T selection) where T : struct, Enum => Choix<T>((T?)selection);

    public static string Abrege(this RegimeIcpe r) => r switch
    {
        RegimeIcpe.Autorisation => "A",
        RegimeIcpe.Enregistrement => "E",
        RegimeIcpe.DeclarationControle => "DC",
        RegimeIcpe.Declaration => "D",
        _ => "NC"
    };

    // ---- Couleurs des pastilles (g-badge--vert / bleu / orange / rouge / gris) ----

    public static string Couleur(this Conformite c) => c switch
    {
        Conformite.Conforme => "vert",
        Conformite.Partielle => "orange",
        Conformite.NonConforme => "rouge",
        Conformite.SansObjet => "gris",
        _ => "bleu"
    };

    public static string Couleur(this StatutVeille s) => s switch
    {
        StatutVeille.AAnalyser => "orange",
        StatutVeille.Applicable => "vert",
        StatutVeille.PourInformation => "bleu",
        _ => "gris"
    };

    public static string Couleur(this StatutAction s) => s switch
    {
        StatutAction.AFaire => "gris",
        StatutAction.EnCours => "bleu",
        StatutAction.Realisee => "vert-clair",
        StatutAction.Verifiee => "vert",
        _ => "gris"
    };

    public static string Couleur(this StatutObjectif s) => s switch
    {
        StatutObjectif.Atteint => "vert",
        StatutObjectif.NonAtteint => "rouge",
        StatutObjectif.Abandonne => "gris",
        _ => "bleu"
    };

    public static string Couleur(this Priorite p) => p switch
    {
        Priorite.Haute => "rouge",
        Priorite.Basse => "gris",
        _ => "bleu"
    };

    public static string Couleur(this RegimeIcpe r) => r switch
    {
        RegimeIcpe.Autorisation => "rouge",
        RegimeIcpe.Enregistrement => "orange",
        RegimeIcpe.DeclarationControle or RegimeIcpe.Declaration => "bleu",
        _ => "gris"
    };

    /// <summary>Barre d'avancement : du rouge (0 %) au vert (100 %).</summary>
    public static string CouleurAvancement(int pourcent) => $"hsl({Math.Clamp(pourcent, 0, 100) * 1.2:0}, 70%, 42%)";

    /// <summary>Prochaine échéance d'après la dernière réalisation et la périodicité.</summary>
    public static DateTime? Suivante(DateTime? derniere, Periodicite p) =>
        derniere is { } d && p != Periodicite.Aucune ? d.AddMonths((int)p) : null;
}
