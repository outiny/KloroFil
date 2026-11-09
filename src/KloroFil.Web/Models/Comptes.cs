using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Identity;

namespace KloroFil.Web.Models;

public class Utilisateur : IdentityUser
{
    [MaxLength(150)] public string? NomComplet { get; set; }
    [MaxLength(100)] public string? Fonction { get; set; }

    public string Affichage => string.IsNullOrWhiteSpace(NomComplet) ? Email ?? UserName ?? "" : NomComplet;
}

public static class RolesKloroFil
{
    public const string Administrateur = "Administrateur";
    public const string ResponsableQse = "ResponsableQSE";
    public const string Contributeur = "Contributeur";
    public const string Lecteur = "Lecteur";

    /// <summary>Pour [Authorize(Roles = ...)] : gère le référentiel (sites, textes, prescriptions, aspects, objectifs, audits).</summary>
    public const string Gestion = Administrateur + "," + ResponsableQse;

    /// <summary>Pour [Authorize(Roles = ...)] : met à jour les actions et les évaluations de conformité.</summary>
    public const string Saisie = Administrateur + "," + ResponsableQse + "," + Contributeur;

    public static readonly string[] Tous = [Administrateur, ResponsableQse, Contributeur, Lecteur];

    public static string Libelle(string role) => role switch
    {
        ResponsableQse => "Responsable QSE",
        _ => role
    };

    public static string Description(string role) => role switch
    {
        Administrateur => "Administrateur : tout, y compris les utilisateurs et les sources de veille",
        ResponsableQse => "Responsable QSE : gère le système de management, la veille et la conformité",
        Contributeur => "Contributeur : met à jour les actions et les évaluations de conformité",
        _ => "Lecteur : consultation uniquement"
    };

    public static bool PeutGerer(this ClaimsPrincipal u) => u.IsInRole(Administrateur) || u.IsInRole(ResponsableQse);

    public static bool PeutSaisir(this ClaimsPrincipal u) => u.PeutGerer() || u.IsInRole(Contributeur);
}

public class ConnexionVm
{
    [Required(ErrorMessage = "Indiquez votre adresse email."), EmailAddress(ErrorMessage = "Adresse email invalide."), Display(Name = "Email")]
    public string Email { get; set; } = "";

    [Required(ErrorMessage = "Indiquez votre mot de passe."), DataType(DataType.Password), Display(Name = "Mot de passe")]
    public string MotDePasse { get; set; } = "";

    [Display(Name = "Se souvenir de moi")]
    public bool SeSouvenir { get; set; }
}

public class NouvelUtilisateurVm
{
    [Required(ErrorMessage = "L'adresse email est obligatoire."), EmailAddress(ErrorMessage = "Adresse email invalide."), MaxLength(256), Display(Name = "Email")]
    public string Email { get; set; } = "";

    [MaxLength(150), Display(Name = "Nom complet")]
    public string? NomComplet { get; set; }

    [MaxLength(100), Display(Name = "Fonction")]
    public string? Fonction { get; set; }

    [Display(Name = "Rôle")]
    public string Role { get; set; } = RolesKloroFil.Lecteur;

    [Required(ErrorMessage = "Le mot de passe est obligatoire."), DataType(DataType.Password), Display(Name = "Mot de passe")]
    public string MotDePasse { get; set; } = "";

    [Compare(nameof(MotDePasse), ErrorMessage = "Les deux mots de passe sont différents."), DataType(DataType.Password), Display(Name = "Confirmation")]
    public string Confirmation { get; set; } = "";
}

public class EditionUtilisateurVm
{
    public string Id { get; set; } = "";
    public string Email { get; set; } = "";

    [MaxLength(150), Display(Name = "Nom complet")]
    public string? NomComplet { get; set; }

    [MaxLength(100), Display(Name = "Fonction")]
    public string? Fonction { get; set; }

    [Display(Name = "Rôle")]
    public string Role { get; set; } = RolesKloroFil.Lecteur;

    [Display(Name = "Compte actif")]
    public bool Actif { get; set; }
}

public class NouveauMotDePasseVm
{
    public string Id { get; set; } = "";
    public string Email { get; set; } = "";

    [Required(ErrorMessage = "Le mot de passe est obligatoire."), DataType(DataType.Password), Display(Name = "Nouveau mot de passe")]
    public string MotDePasse { get; set; } = "";

    [Compare(nameof(MotDePasse), ErrorMessage = "Les deux mots de passe sont différents."), DataType(DataType.Password), Display(Name = "Confirmation")]
    public string Confirmation { get; set; } = "";
}

public class ChangerMotDePasseVm
{
    [Required(ErrorMessage = "Indiquez votre mot de passe actuel."), DataType(DataType.Password), Display(Name = "Mot de passe actuel")]
    public string Actuel { get; set; } = "";

    [Required(ErrorMessage = "Le mot de passe est obligatoire."), DataType(DataType.Password), Display(Name = "Nouveau mot de passe")]
    public string Nouveau { get; set; } = "";

    [Compare(nameof(Nouveau), ErrorMessage = "Les deux mots de passe sont différents."), DataType(DataType.Password), Display(Name = "Confirmation")]
    public string Confirmation { get; set; } = "";
}

public record LigneUtilisateur(string Id, string Email, string? NomComplet, string? Fonction, string Role, bool Actif);

/// <summary>Messages d'Identity en français (seuls ceux que l'application peut afficher).</summary>
public class ErreursIdentiteFr : IdentityErrorDescriber
{
    private static IdentityError Erreur(string code, string message) => new() { Code = code, Description = message };

    public override IdentityError DuplicateUserName(string userName) => Erreur(nameof(DuplicateUserName), $"L'adresse « {userName} » est déjà utilisée.");
    public override IdentityError DuplicateEmail(string email) => Erreur(nameof(DuplicateEmail), $"L'adresse « {email} » est déjà utilisée.");
    public override IdentityError InvalidUserName(string? userName) => Erreur(nameof(InvalidUserName), "L'adresse email contient des caractères non autorisés.");
    public override IdentityError InvalidEmail(string? email) => Erreur(nameof(InvalidEmail), "Adresse email invalide.");
    public override IdentityError PasswordMismatch() => Erreur(nameof(PasswordMismatch), "Le mot de passe actuel est incorrect.");
    public override IdentityError PasswordTooShort(int length) => Erreur(nameof(PasswordTooShort), $"Le mot de passe doit contenir au moins {length} caractères.");
    public override IdentityError PasswordRequiresUniqueChars(int uniqueChars) => Erreur(nameof(PasswordRequiresUniqueChars), $"Le mot de passe doit contenir au moins {uniqueChars} caractères différents.");
    public override IdentityError InvalidToken() => Erreur(nameof(InvalidToken), "Opération expirée, veuillez recommencer.");
    public override IdentityError ConcurrencyFailure() => Erreur(nameof(ConcurrencyFailure), "Ce compte vient d'être modifié par ailleurs, veuillez recommencer.");
}
