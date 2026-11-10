using System.ComponentModel.DataAnnotations;

namespace KloroFil.Web.Models;

/// <summary>Régimes de la nomenclature des Installations Classées pour la Protection de l'Environnement.</summary>
public enum RegimeIcpe
{
    [Display(Name = "Non classé")] NonClasse = 0,
    [Display(Name = "Déclaration (D)")] Declaration = 1,
    [Display(Name = "Déclaration avec contrôle (DC)")] DeclarationControle = 2,
    [Display(Name = "Enregistrement (E)")] Enregistrement = 3,
    [Display(Name = "Autorisation (A)")] Autorisation = 4
}

public enum StatutSeveso
{
    [Display(Name = "Non Seveso")] NonSeveso = 0,
    [Display(Name = "Seveso seuil bas")] SeuilBas = 1,
    [Display(Name = "Seveso seuil haut")] SeuilHaut = 2
}

public enum TypeTexte
{
    [Display(Name = "Directive / règlement européen")] Europeen = 0,
    [Display(Name = "Loi / ordonnance")] Loi = 1,
    [Display(Name = "Décret")] Decret = 2,
    [Display(Name = "Arrêté ministériel")] ArreteMinisteriel = 3,
    [Display(Name = "Arrêté préfectoral")] ArretePrefectoral = 4,
    [Display(Name = "Circulaire / instruction")] Circulaire = 5,
    [Display(Name = "Norme / guide")] Norme = 6,
    [Display(Name = "Autre")] Autre = 9
}

public enum Domaine
{
    [Display(Name = "ICPE")] Icpe = 0,
    [Display(Name = "Air")] Air = 1,
    [Display(Name = "Eau")] Eau = 2,
    [Display(Name = "Déchets")] Dechets = 3,
    [Display(Name = "Sols")] Sols = 4,
    [Display(Name = "Bruit / vibrations")] Bruit = 5,
    [Display(Name = "Énergie / climat")] Energie = 6,
    [Display(Name = "Risques / incendie")] Risques = 7,
    [Display(Name = "Produits chimiques")] Produits = 8,
    [Display(Name = "Biodiversité")] Biodiversite = 9,
    [Display(Name = "Général")] General = 10
}

public enum StatutVeille
{
    [Display(Name = "À analyser")] AAnalyser = 0,
    [Display(Name = "Applicable")] Applicable = 1,
    [Display(Name = "Pour information")] PourInformation = 2,
    [Display(Name = "Non applicable")] NonApplicable = 3
}

/// <summary>
/// Statut de conformité d'une exigence ou d'une prescription. Les valeurs numériques sont celles stockées en base
/// (0 = ancien « À évaluer », 2 = ancien « Partiellement conforme », 4 = ancien « Sans objet ») ; Order fixe l'ordre des listes.
/// </summary>
public enum Conformite
{
    [Display(Name = "En cours d'analyse", Order = 3)] EnCoursAnalyse = 0,
    [Display(Name = "Conforme", Order = 1)] Conforme = 1,
    [Display(Name = "En application mais NC en cours", Order = 5)] NcEnCours = 2,
    [Display(Name = "Non conforme", Order = 2)] NonConforme = 3,
    [Display(Name = "Non concerné", Order = 6)] NonConcerne = 4,
    [Display(Name = "À surveiller", Order = 4)] ASurveiller = 5
}

public enum Periodicite
{
    [Display(Name = "Ponctuelle")] Aucune = 0,
    [Display(Name = "Mensuelle")] Mensuelle = 1,
    [Display(Name = "Trimestrielle")] Trimestrielle = 3,
    [Display(Name = "Semestrielle")] Semestrielle = 6,
    [Display(Name = "Annuelle")] Annuelle = 12,
    [Display(Name = "Tous les 2 ans")] Biennale = 24,
    [Display(Name = "Tous les 3 ans")] Triennale = 36,
    [Display(Name = "Tous les 5 ans")] Quinquennale = 60,
    [Display(Name = "Tous les 10 ans")] Decennale = 120
}

public enum Situation
{
    [Display(Name = "Normale")] Normale = 0,
    [Display(Name = "Anormale")] Anormale = 1,
    [Display(Name = "Urgence")] Urgence = 2
}

public enum StatutObjectif
{
    [Display(Name = "En cours")] EnCours = 0,
    [Display(Name = "Atteint")] Atteint = 1,
    [Display(Name = "Non atteint")] NonAtteint = 2,
    [Display(Name = "Abandonné")] Abandonne = 3
}

public enum TypeAudit
{
    [Display(Name = "Audit interne")] Interne = 0,
    [Display(Name = "Audit de certification")] Certification = 1,
    [Display(Name = "Audit de surveillance")] Surveillance = 2,
    [Display(Name = "Audit de renouvellement")] Renouvellement = 3,
    [Display(Name = "Inspection ICPE (DREAL)")] InspectionIcpe = 4,
    [Display(Name = "Revue de direction")] RevueDirection = 5,
    [Display(Name = "Autre")] Autre = 9
}

public enum TypeAction
{
    [Display(Name = "Corrective")] Corrective = 0,
    [Display(Name = "Préventive")] Preventive = 1,
    [Display(Name = "Amélioration")] Amelioration = 2,
    [Display(Name = "Mise en conformité")] MiseEnConformite = 3
}

public enum OrigineAction
{
    [Display(Name = "Veille réglementaire")] Veille = 0,
    [Display(Name = "Évaluation de conformité")] Conformite = 1,
    [Display(Name = "Prescription ICPE")] Prescription = 2,
    [Display(Name = "Audit / inspection")] Audit = 3,
    [Display(Name = "Aspect significatif")] Aspect = 4,
    [Display(Name = "Incident / accident")] Incident = 5,
    [Display(Name = "Réclamation / partie intéressée")] Reclamation = 6,
    [Display(Name = "Revue de direction")] RevueDirection = 7,
    [Display(Name = "Autre")] Autre = 9
}

public enum Priorite
{
    [Display(Name = "Basse")] Basse = 0,
    [Display(Name = "Normale")] Normale = 1,
    [Display(Name = "Haute")] Haute = 2
}

public enum StatutAction
{
    [Display(Name = "À faire")] AFaire = 0,
    [Display(Name = "En cours")] EnCours = 1,
    [Display(Name = "Réalisée")] Realisee = 2,
    [Display(Name = "Efficacité vérifiée")] Verifiee = 3,
    [Display(Name = "Abandonnée")] Abandonnee = 4
}
