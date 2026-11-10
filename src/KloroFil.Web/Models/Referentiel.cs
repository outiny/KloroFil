using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KloroFil.Web.Models;

// ---------------------------------------------------------------------------------------------
// Sites et situation administrative ICPE
// ---------------------------------------------------------------------------------------------

/// <summary>Établissement (site industriel) soumis ou non à la réglementation ICPE.</summary>
public class Site
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Le nom est obligatoire."), MaxLength(150), Display(Name = "Nom du site")]
    public string Nom { get; set; } = "";

    [MaxLength(250), Display(Name = "Adresse")]
    public string? Adresse { get; set; }

    [MaxLength(10), Display(Name = "Code postal")]
    public string? CodePostal { get; set; }

    [MaxLength(100), Display(Name = "Commune")]
    public string? Commune { get; set; }

    [MaxLength(30), Display(Name = "Code AIOT (Géorisques)")]
    public string? CodeAiot { get; set; }

    [Display(Name = "Régime ICPE le plus élevé")]
    public RegimeIcpe Regime { get; set; }

    [Display(Name = "Statut Seveso")]
    public StatutSeveso Seveso { get; set; }

    [Display(Name = "Soumis à la directive IED")]
    public bool Ied { get; set; }

    [MaxLength(200), Display(Name = "Acte administratif de référence")]
    public string? ActeReference { get; set; }

    [DataType(DataType.Date), Display(Name = "Date de l'acte")]
    public DateTime? DateActe { get; set; }

    [MaxLength(150), Display(Name = "Inspecteur / DREAL")]
    public string? Inspecteur { get; set; }

    [Display(Name = "Notes")]
    public string? Notes { get; set; }

    public List<Classement> Classements { get; set; } = [];
    public List<Prescription> Prescriptions { get; set; } = [];
}

/// <summary>Rubrique de la nomenclature ICPE dans laquelle une activité du site est classée.</summary>
public class Classement
{
    public int Id { get; set; }
    public int SiteId { get; set; }
    public Site? Site { get; set; }

    [Required(ErrorMessage = "La rubrique est obligatoire."), MaxLength(10), Display(Name = "Rubrique")]
    public string Rubrique { get; set; } = "";

    [MaxLength(20), Display(Name = "Alinéa")]
    public string? Alinea { get; set; }

    [Required(ErrorMessage = "La désignation est obligatoire."), MaxLength(400), Display(Name = "Désignation de l'activité")]
    public string Designation { get; set; } = "";

    [MaxLength(100), Display(Name = "Volume / capacité")]
    public string? Volume { get; set; }

    [Display(Name = "Régime")]
    public RegimeIcpe Regime { get; set; }

    [MaxLength(300), Display(Name = "Arrêté ministériel applicable")]
    public string? ArreteMinisteriel { get; set; }
}

/// <summary>Prescription d'un arrêté (préfectoral ou ministériel) : contrôle, mesure, rapport, etc.</summary>
public class Prescription
{
    public int Id { get; set; }

    [Display(Name = "Site")]
    public int SiteId { get; set; }
    public Site? Site { get; set; }

    [Required(ErrorMessage = "Indiquez l'arrêté d'origine."), MaxLength(200), Display(Name = "Arrêté d'origine")]
    public string Source { get; set; } = "";

    [MaxLength(50), Display(Name = "Article")]
    public string? Article { get; set; }

    [Required(ErrorMessage = "L'intitulé est obligatoire."), MaxLength(300), Display(Name = "Intitulé")]
    public string Intitule { get; set; } = "";

    [Display(Name = "Exigence détaillée")]
    public string? Description { get; set; }

    [Display(Name = "Domaine")]
    public Domaine Domaine { get; set; } = Domaine.Icpe;

    [Display(Name = "Périodicité")]
    public Periodicite Periodicite { get; set; }

    [DataType(DataType.Date), Display(Name = "Dernière réalisation")]
    public DateTime? DerniereRealisation { get; set; }

    [DataType(DataType.Date), Display(Name = "Prochaine échéance")]
    public DateTime? ProchaineEcheance { get; set; }

    [MaxLength(150), Display(Name = "Responsable")]
    public string? Responsable { get; set; }

    [Display(Name = "Conformité")]
    public Conformite Conformite { get; set; }

    [Display(Name = "Commentaire / preuves")]
    public string? Commentaire { get; set; }

    [NotMapped] public bool EnRetard => ProchaineEcheance is { } e && e.Date < DateTime.Today;
}

// ---------------------------------------------------------------------------------------------
// Veille réglementaire et évaluation de conformité
// ---------------------------------------------------------------------------------------------

/// <summary>Flux RSS / Atom interrogé automatiquement par la veille.</summary>
public class SourceVeille
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Le nom est obligatoire."), MaxLength(150), Display(Name = "Nom")]
    public string Nom { get; set; } = "";

    [Required(ErrorMessage = "L'adresse du flux est obligatoire."), MaxLength(1000), Url(ErrorMessage = "Adresse invalide."), Display(Name = "Adresse du flux RSS / Atom")]
    public string Url { get; set; } = "";

    [MaxLength(1000), Display(Name = "Mots-clés (séparés par des virgules)")]
    public string? MotsCles { get; set; }

    [Display(Name = "Domaine par défaut")]
    public Domaine Domaine { get; set; } = Domaine.Icpe;

    [Display(Name = "Active")]
    public bool Active { get; set; } = true;

    [Display(Name = "Dernière lecture")]
    public DateTime? DerniereLecture { get; set; }

    [MaxLength(500), Display(Name = "Résultat de la dernière lecture")]
    public string? DernierResultat { get; set; }
}

/// <summary>Texte réglementaire suivi par la veille.</summary>
public class TexteReglementaire
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Le titre est obligatoire."), MaxLength(600), Display(Name = "Titre")]
    public string Titre { get; set; } = "";

    [MaxLength(100), Display(Name = "Référence (NOR, n°)")]
    public string? Reference { get; set; }

    [Display(Name = "Type de texte")]
    public TypeTexte Type { get; set; } = TypeTexte.ArreteMinisteriel;

    [Display(Name = "Domaine")]
    public Domaine Domaine { get; set; } = Domaine.Icpe;

    [DataType(DataType.Date), Display(Name = "Date de publication")]
    public DateTime? DatePublication { get; set; }

    [DataType(DataType.Date), Display(Name = "Date d'application")]
    public DateTime? DateApplication { get; set; }

    [MaxLength(1000), Display(Name = "Lien")]
    public string? Url { get; set; }

    [Display(Name = "Résumé")]
    public string? Resume { get; set; }

    [Display(Name = "Statut")]
    public StatutVeille Statut { get; set; }

    [Display(Name = "Analyse / impact pour l'entreprise")]
    public string? Analyse { get; set; }

    [Display(Name = "Analysé le")]
    public DateTime? AnalyseLe { get; set; }

    [MaxLength(150), Display(Name = "Analysé par")]
    public string? AnalysePar { get; set; }

    [MaxLength(200), Display(Name = "Rubriques ICPE concernées")]
    public string? RubriquesConcernees { get; set; }

    [Display(Name = "Détecté le")]
    public DateTime DateDetection { get; set; } = DateTime.Now;

    public int? SourceVeilleId { get; set; }
    public SourceVeille? SourceVeille { get; set; }

    public List<Exigence> Exigences { get; set; } = [];
}

/// <summary>
/// Exigence issue d'un texte applicable (un article ou un point d'annexe), évaluée en deux temps (ISO 14001 § 9.1.2) :
/// « Législation » quand le texte devient applicable, puis « Récépissé » une fois le dossier déposé.
/// Conformite / DateEvaluation / EvaluePar sont la synthèse de ces évaluations (voir <see cref="Synthetiser"/>).
/// </summary>
public class Exigence
{
    public int Id { get; set; }
    public int TexteReglementaireId { get; set; }
    public TexteReglementaire? Texte { get; set; }

    [MaxLength(50), Display(Name = "Article")]
    public string? Article { get; set; }

    [Required(ErrorMessage = "L'exigence est obligatoire."), Display(Name = "Exigence")]
    public string Description { get; set; } = "";

    /// <summary>Texte repris tel quel de Légifrance (false : saisi ou modifié dans KloroFil).</summary>
    public bool Importee { get; set; }

    [Display(Name = "Conformité")]
    public Conformite Conformite { get; set; }

    [Display(Name = "Évaluée le")]
    public DateTime? DateEvaluation { get; set; }

    [MaxLength(150), Display(Name = "Évaluée par")]
    public string? EvaluePar { get; set; }

    public List<EvaluationExigence> Evaluations { get; set; } = [];

    public EvaluationExigence? Evaluation(PhaseEvaluation phase) => Evaluations.FirstOrDefault(e => e.Phase == phase);

    /// <summary>Synthèse : l'évaluation « Récépissé » dès qu'elle est faite, sinon l'évaluation « Législation ».</summary>
    public void Synthetiser()
    {
        var retenue = Evaluation(PhaseEvaluation.Recepisse) is { Conformite: not Conformite.EnCoursAnalyse } r ? r : Evaluation(PhaseEvaluation.Legislation);
        Conformite = retenue?.Conformite ?? Conformite.EnCoursAnalyse;
        DateEvaluation = retenue?.DateEvaluation;
        EvaluePar = retenue?.EvaluePar;
    }
}

/// <summary>Écran d'évaluation : l'exigence, l'évaluation de la phase affichée et, pour mémoire, toutes ses évaluations.</summary>
public class ExigenceVm
{
    public Exigence Exigence { get; set; } = new();
    public EvaluationExigence Evaluation { get; set; } = new();
    public PhaseEvaluation Phase { get; set; }
    public string? TitreTexte { get; set; }
    public List<EvaluationExigence> Evaluations { get; set; } = [];
}

public enum PhaseEvaluation
{
    [Display(Name = "Législation")] Legislation = 0,
    [Display(Name = "Récépissé")] Recepisse = 1
}

/// <summary>Évaluation d'une exigence pour une phase : à l'entrée en application du texte, puis après le dépôt du dossier (récépissé).</summary>
public class EvaluationExigence
{
    public int Id { get; set; }
    public int ExigenceId { get; set; }
    public Exigence? Exigence { get; set; }

    public PhaseEvaluation Phase { get; set; }

    [Display(Name = "Conformité")]
    public Conformite Conformite { get; set; }

    [DataType(DataType.Date), Display(Name = "Évaluée le")]
    public DateTime? DateEvaluation { get; set; }

    [MaxLength(150), Display(Name = "Évaluée par")]
    public string? EvaluePar { get; set; }

    [Display(Name = "Critères d'évaluation")]
    public string? CriteresEvaluation { get; set; }

    [Display(Name = "Moyens de surveillance")]
    public string? MoyensSurveillance { get; set; }

    [Display(Name = "Actions correctives")]
    public string? ActionsCorrectives { get; set; }

    [Display(Name = "Preuves / commentaire")]
    public string? Preuves { get; set; }
}

// ---------------------------------------------------------------------------------------------
// Système de management ISO 14001
// ---------------------------------------------------------------------------------------------

/// <summary>Analyse environnementale (ISO 14001 § 6.1.2) : aspect, impact et cotation.</summary>
public class AspectEnvironnemental
{
    public const int SeuilSignificatif = 27;

    public int Id { get; set; }

    [Display(Name = "Site")]
    public int? SiteId { get; set; }
    public Site? Site { get; set; }

    [Required(ErrorMessage = "L'activité est obligatoire."), MaxLength(200), Display(Name = "Activité / processus")]
    public string Activite { get; set; } = "";

    [Required(ErrorMessage = "L'aspect est obligatoire."), MaxLength(300), Display(Name = "Aspect environnemental")]
    public string Aspect { get; set; } = "";

    [Required(ErrorMessage = "L'impact est obligatoire."), MaxLength(300), Display(Name = "Impact")]
    public string Impact { get; set; } = "";

    [Display(Name = "Compartiment")]
    public Domaine Domaine { get; set; } = Domaine.Air;

    [Display(Name = "Situation")]
    public Situation Situation { get; set; }

    [Range(1, 5, ErrorMessage = "Note de 1 à 5."), Display(Name = "Gravité (1-5)")]
    public int Gravite { get; set; } = 1;

    [Range(1, 5, ErrorMessage = "Note de 1 à 5."), Display(Name = "Fréquence (1-5)")]
    public int Frequence { get; set; } = 1;

    [Range(1, 5, ErrorMessage = "Note de 1 à 5."), Display(Name = "Maîtrise (1 = bonne, 5 = nulle)")]
    public int Maitrise { get; set; } = 1;

    [Display(Name = "Soumis à une exigence légale")]
    public bool ExigenceLegale { get; set; }

    [Display(Name = "Mesures de maîtrise")]
    public string? MesuresMaitrise { get; set; }

    [NotMapped, Display(Name = "Criticité")] public int Criticite => Gravite * Frequence * Maitrise;

    /// <summary>Significatif si la criticité atteint le seuil, si la gravité est maximale ou si une exigence légale s'y applique.</summary>
    [NotMapped] public bool Significatif => Criticite >= SeuilSignificatif || Gravite == 5 || ExigenceLegale;
}

/// <summary>Objectif environnemental (ISO 14001 § 6.2).</summary>
public class ObjectifEnvironnemental
{
    public int Id { get; set; }

    [Required(ErrorMessage = "L'intitulé est obligatoire."), MaxLength(300), Display(Name = "Objectif")]
    public string Intitule { get; set; } = "";

    [Display(Name = "Domaine")]
    public Domaine Domaine { get; set; } = Domaine.Energie;

    [MaxLength(200), Display(Name = "Indicateur")]
    public string? Indicateur { get; set; }

    [MaxLength(100), Display(Name = "Valeur de référence")]
    public string? ValeurReference { get; set; }

    [MaxLength(100), Display(Name = "Cible")]
    public string? Cible { get; set; }

    [MaxLength(100), Display(Name = "Valeur actuelle")]
    public string? ValeurActuelle { get; set; }

    [DataType(DataType.Date), Display(Name = "Échéance")]
    public DateTime? Echeance { get; set; }

    [MaxLength(150), Display(Name = "Responsable")]
    public string? Responsable { get; set; }

    [Range(0, 100, ErrorMessage = "Entre 0 et 100 %."), Display(Name = "Avancement (%)")]
    public int Avancement { get; set; }

    [Display(Name = "Statut")]
    public StatutObjectif Statut { get; set; }
}

/// <summary>Audit interne / externe, inspection ICPE ou revue de direction.</summary>
public class Audit
{
    public int Id { get; set; }

    [DataType(DataType.Date), Display(Name = "Date")]
    public DateTime Date { get; set; } = DateTime.Today;

    [Display(Name = "Type")]
    public TypeAudit Type { get; set; }

    [Display(Name = "Site")]
    public int? SiteId { get; set; }
    public Site? Site { get; set; }

    [MaxLength(150), Display(Name = "Auditeur / organisme")]
    public string? Auditeur { get; set; }

    [MaxLength(300), Display(Name = "Périmètre")]
    public string? Perimetre { get; set; }

    [Range(0, 999), Display(Name = "Non-conformités majeures")]
    public int NcMajeures { get; set; }

    [Range(0, 999), Display(Name = "Non-conformités mineures")]
    public int NcMineures { get; set; }

    [Range(0, 999), Display(Name = "Observations / pistes de progrès")]
    public int Observations { get; set; }

    [Display(Name = "Conclusion")]
    public string? Conclusion { get; set; }

    [MaxLength(1000), Display(Name = "Lien vers le rapport")]
    public string? Rapport { get; set; }
}

/// <summary>Action du plan d'actions environnemental (ISO 14001 § 10.2).</summary>
public class ActionQse
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Le titre est obligatoire."), MaxLength(300), Display(Name = "Action")]
    public string Titre { get; set; } = "";

    [Display(Name = "Description")]
    public string? Description { get; set; }

    [Display(Name = "Type")]
    public TypeAction Type { get; set; }

    [Display(Name = "Origine")]
    public OrigineAction Origine { get; set; }

    [Display(Name = "Priorité")]
    public Priorite Priorite { get; set; } = Priorite.Normale;

    [MaxLength(150), Display(Name = "Responsable")]
    public string? Responsable { get; set; }

    [Display(Name = "Créée le")]
    public DateTime DateCreation { get; set; } = DateTime.Today;

    [DataType(DataType.Date), Display(Name = "Échéance")]
    public DateTime? Echeance { get; set; }

    [Display(Name = "Statut")]
    public StatutAction Statut { get; set; }

    [DataType(DataType.Date), Display(Name = "Réalisée le")]
    public DateTime? DateRealisation { get; set; }

    [Display(Name = "Vérification de l'efficacité")]
    public string? Efficacite { get; set; }

    // Liens facultatifs vers l'origine de l'action
    [Display(Name = "Texte réglementaire")] public int? TexteReglementaireId { get; set; }
    public TexteReglementaire? Texte { get; set; }
    [Display(Name = "Exigence")] public int? ExigenceId { get; set; }
    public Exigence? Exigence { get; set; }
    [Display(Name = "Prescription")] public int? PrescriptionId { get; set; }
    public Prescription? Prescription { get; set; }
    [Display(Name = "Audit")] public int? AuditId { get; set; }
    public Audit? Audit { get; set; }
    [Display(Name = "Aspect")] public int? AspectEnvironnementalId { get; set; }
    public AspectEnvironnemental? Aspect { get; set; }

    [NotMapped] public string Numero => $"A-{Id:0000}";
    [NotMapped] public bool Ouverte => Statut is StatutAction.AFaire or StatutAction.EnCours;
    [NotMapped] public bool EnRetard => Ouverte && Echeance is { } e && e.Date < DateTime.Today;
}
