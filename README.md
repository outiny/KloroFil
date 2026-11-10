# KloroFil

Application web de pilotage du **système de management environnemental ISO 14001** de Guéry, centrée sur la
réglementation des **Installations Classées pour la Protection de l'Environnement (ICPE)** et la **veille réglementaire**.

ASP.NET Core MVC (.NET 10) · Entity Framework Core · SQL Server · ASP.NET Core Identity (rôles).

## Fonctionnalités

| Module | Contenu |
|---|---|
| **Tableau de bord** | Textes à analyser, taux de conformité, contrôles ICPE en retard, échéances à 60 jours, actions ouvertes / en retard, aspects significatifs, objectifs |
| **Sites ICPE** | Situation administrative (régime A / E / DC / D, Seveso, IED, code AIOT, arrêté), tableau des rubriques de la nomenclature |
| **Prescriptions** | Obligations des arrêtés préfectoraux / ministériels avec périodicité et échéancier ; bouton « Réalisé » qui reporte l'échéance |
| **Veille réglementaire** | Registre des textes, qualification (applicable, pour information, non applicable), analyse d'impact, exigences et **registre de conformité** (§ 9.1.2) |
| **Veille automatisée** | Lecture planifiée de flux **RSS / Atom** filtrés par mots-clés ; les nouveautés arrivent « À analyser » et les rubriques ICPE du parc citées sont repérées |
| **Analyse environnementale** | Aspects / impacts, cotation gravité × fréquence × maîtrise, aspects significatifs (§ 6.1.2) |
| **Objectifs** | Objectifs, indicateurs, cibles et avancement (§ 6.2) |
| **Plan d'actions** | Actions correctives / préventives / de mise en conformité liées à leur origine (exigence, prescription, audit, aspect), vérification d'efficacité (§ 10.2) |
| **Audits** | Audits internes, de certification, inspections DREAL, revues de direction (§ 9.2, 9.3) |

## Rôles

| Rôle | Droits |
|---|---|
| Administrateur | Tout, y compris les utilisateurs et les sources de veille |
| ResponsableQSE | Gère tout le contenu (sites, textes, prescriptions, aspects, objectifs, audits, sources) |
| Contributeur | Met à jour les actions, les évaluations de conformité et les réalisations de contrôles |
| Lecteur | Consultation uniquement |

Au premier lancement (base vide), l'écran **Premier démarrage** crée le compte administrateur.
Ensuite, les comptes sont créés par l'administrateur (menu *Utilisateurs*). La connexion se fait par adresse email.

## Installation

1. Ouvrir `KloroFil.slnx` dans Visual Studio 2026.
2. Serveur et base dans `src/KloroFil.Web/appsettings.json` (`ConnectionStrings:KloroFil`, par défaut
   `srv-data\sqlapiguery`, base `KloroFil`).
3. Authentification SQL : le login reste **hors de GitHub**, dans les User Secrets ou des variables d'environnement :
   ```bash
   dotnet user-secrets --project src/KloroFil.Web set "Sql:Utilisateur" "klorofil"
   dotnet user-secrets --project src/KloroFil.Web set "Sql:MotDePasse" "…"
   ```
   Sans login SQL, la connexion se fait avec le compte Windows (sécurité intégrée).
4. Lancer : la base et les rôles sont créés / mis à jour automatiquement au démarrage (migrations EF Core).

Nouvelle migration après modification du modèle :
```bash
dotnet ef migrations add NomDeLaMigration --project src/KloroFil.Web -o Data/Migrations
```

## Veille automatisée

Menu *Sources de veille* : déclarer un flux RSS ou Atom et des mots-clés (sans tenir compte des accents ni des
majuscules ; vide = tout le flux). Exemple fonctionnel :
`https://news.google.com/rss/search?q=ICPE+installations+class%C3%A9es&hl=fr&gl=FR&ceid=FR:fr`.
Le flux de votre prestataire de veille, de la DREAL ou d'un organisme professionnel peut être ajouté de la même façon.

Si l'adresse saisie est une page web (ex. `https://aida.ineris.fr/`), KloroFil cherche le flux RSS qu'elle annonce
et le retient à la place de la page.

### Textes hors sujet

Les titres contenant certaines expressions sont écartés : avis de vacance, nominations, concours, régime
indemnitaire, délégations de signature… Ces textes citent les ICPE sans avoir de portée réglementaire pour
l'entreprise. Les textes de ce type déjà importés et encore « À analyser » sont classés « Non applicable », avec la
raison (rien n'est supprimé). La liste est dans `VeilleService.ExclusionsParDefaut` ; on peut la remplacer dans
`appsettings.json` :
```json
"Veille": { "Exclusions": [ "avis de vacance", "nomination", "concours" ] }
```

### Légifrance (Journal officiel) via l'API PISTE

Légifrance refuse les lectures automatiques de son site : une source dont l'adresse est sur `legifrance.gouv.fr`
est interrogée par l'**API Légifrance de la DILA** (portail [PISTE](https://piste.gouv.fr)).
Chaque mot-clé de la source donne une recherche dans le fonds **JORF** sur les 12 derniers mois
(`Veille:ProfondeurJours`, 365 par défaut), 200 textes au plus par mot-clé (`Piste:MaxParMotCle`).
Les textes déjà connus sont ignorés : seuls les nouveaux arrivent « À analyser ».

**Textes de référence par rubrique** : pour chaque rubrique ICPE suivie (celles des sites, menu *Sites ICPE*, et
les mots-clés à 4 chiffres de la source, ex. « 2565 »), la lecture recherche aussi, **sans limite de date**, les
textes dont le titre contient le numéro de rubrique : arrêtés de prescriptions générales (déclaration,
enregistrement, autorisation) et leurs modifications. Il n'est donc pas nécessaire de connaître les NOR ; le bouton
« Importer de Légifrance » reste disponible pour ajouter un texte précis.

**Socle commun aux ICPE** (`Services/SocleIcpe.cs`) : textes qui s'appliquent sans citer de rubrique dans leur titre,
recherchés sans limite de date selon le régime le plus élevé des sites (avec leurs arrêtés modificatifs) :

| Régime | Textes |
|---|---|
| Déclaration et plus | Bruit (AM 23/01/1997), registres déchets (AM 31/05/2021), sols pollués et cessation d'activité, décrets de nomenclature (5 ans) |
| Déclaration avec contrôle | Contrôle périodique |
| Enregistrement et plus | GEREP (AM 31/01/2008), garanties financières (AM 31/05/2012) |
| Autorisation | Prélèvements et émissions (AM 02/02/1998), risques accidentels (AM 04/10/2010) |
| Seveso | Prévention des accidents majeurs (AM 26/05/2014) |

**Modifications des textes applicables** : pour chaque arrêté classé « Applicable », la lecture recherche les arrêtés
« modifiant l'arrêté du … » qui reprennent son objet, quelle que soit leur date.

**Import des articles et annexes** : sur la fiche d'un texte Légifrance, le bouton « Importer les articles et annexes »
lit la version **en vigueur** du texte (API `/consult/lawDecree`) et crée une exigence par article et par paragraphe
numéroté des annexes (« Annexe I – 5.1 », avec l'intitulé des niveaux supérieurs). Les articles abrogés, l'article
d'exécution et les points vides du modèle d'arrêté sont ignorés. « Mettre à jour depuis Légifrance » ajoute les
nouveaux points et met à jour ceux dont la rédaction a changé : ils repassent « À évaluer », l'évaluation précédente
étant notée dans les preuves ; rien n'est supprimé. Si une annexe n'est publiée qu'au Bulletin officiel du ministère
(non reproduite sur Légifrance), un message invite à la consulter sur AIDA.

La page *Sources de veille* affiche ce que couvre la lecture (rubriques suivies, socle retenu, nombre d'arrêtés suivis).
En complément, un contrôle annuel reste conseillé : pages par rubrique d'AIDA (INERIS) et visas (« Vu… ») des
récépissés et arrêtés préfectoraux du site.

1. Sur piste.gouv.fr, dans votre application, ajoutez l'API **Légifrance** et acceptez ses conditions d'utilisation.
2. Copiez la *client_id* et le *client_secret* (OAuth) de l'application dans les secrets de KloroFil :
   ```bash
   dotnet user-secrets --project src/KloroFil.Web set "Piste:ClientId" "…"
   dotnet user-secrets --project src/KloroFil.Web set "Piste:ClientSecret" "…"
   ```
   Pour le bac à sable PISTE, ajoutez `Piste:Sandbox` = `true`. Facultatif : `Piste:TokenUrl`, `Piste:ApiBaseUrl`
   (et leurs variantes `…Sandbox`) remplacent les adresses PISTE par défaut.
   Le journal de l'application indique, pour chaque mot-clé, le nombre de textes trouvés.
3. Ajoutez une source d'adresse `https://www.legifrance.gouv.fr/` avec vos mots-clés (ex. « installations classées, 2565 »).

Réglages (`appsettings.json`, section `Veille`) : `IntervalleHeures` (24 par défaut, 0 = désactivée) et
`DelaiDemarrageMinutes`. Les boutons « Lancer la veille » et « Lire maintenant » déclenchent une lecture immédiate.

## Charte graphique

Dégradé de vert KloroFil, logo Guéry et police Jost (charte Guéry). Styles dans `wwwroot/css/site.css`,
logos et icône dans `wwwroot/img` et `wwwroot/favicon.ico`.
