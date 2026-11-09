using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KloroFil.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class Initiale : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AspNetRoles",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUsers",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    NomComplet = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    Fonction = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    UserName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedUserName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    EmailConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SecurityStamp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PhoneNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PhoneNumberConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    TwoFactorEnabled = table.Column<bool>(type: "bit", nullable: false),
                    LockoutEnd = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LockoutEnabled = table.Column<bool>(type: "bit", nullable: false),
                    AccessFailedCount = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUsers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Objectifs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Intitule = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Domaine = table.Column<int>(type: "int", nullable: false),
                    Indicateur = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ValeurReference = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Cible = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ValeurActuelle = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Echeance = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Responsable = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    Avancement = table.Column<int>(type: "int", nullable: false),
                    Statut = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Objectifs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Sites",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nom = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Adresse = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    CodePostal = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    Commune = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CodeAiot = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    Regime = table.Column<int>(type: "int", nullable: false),
                    Seveso = table.Column<int>(type: "int", nullable: false),
                    Ied = table.Column<bool>(type: "bit", nullable: false),
                    ActeReference = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    DateActe = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Inspecteur = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sites", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SourcesVeille",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nom = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Url = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    MotsCles = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Domaine = table.Column<int>(type: "int", nullable: false),
                    Active = table.Column<bool>(type: "bit", nullable: false),
                    DerniereLecture = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DernierResultat = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SourcesVeille", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetRoleClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RoleId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ClaimType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClaimValue = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoleClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetRoleClaims_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ClaimType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClaimValue = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetUserClaims_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserLogins",
                columns: table => new
                {
                    LoginProvider = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ProviderKey = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ProviderDisplayName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserLogins", x => new { x.LoginProvider, x.ProviderKey });
                    table.ForeignKey(
                        name: "FK_AspNetUserLogins_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserRoles",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    RoleId = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserRoles", x => new { x.UserId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserTokens",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    LoginProvider = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Value = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserTokens", x => new { x.UserId, x.LoginProvider, x.Name });
                    table.ForeignKey(
                        name: "FK_AspNetUserTokens_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Aspects",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SiteId = table.Column<int>(type: "int", nullable: true),
                    Activite = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Aspect = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Impact = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Domaine = table.Column<int>(type: "int", nullable: false),
                    Situation = table.Column<int>(type: "int", nullable: false),
                    Gravite = table.Column<int>(type: "int", nullable: false),
                    Frequence = table.Column<int>(type: "int", nullable: false),
                    Maitrise = table.Column<int>(type: "int", nullable: false),
                    ExigenceLegale = table.Column<bool>(type: "bit", nullable: false),
                    MesuresMaitrise = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Aspects", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Aspects_Sites_SiteId",
                        column: x => x.SiteId,
                        principalTable: "Sites",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "Audits",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    SiteId = table.Column<int>(type: "int", nullable: true),
                    Auditeur = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    Perimetre = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    NcMajeures = table.Column<int>(type: "int", nullable: false),
                    NcMineures = table.Column<int>(type: "int", nullable: false),
                    Observations = table.Column<int>(type: "int", nullable: false),
                    Conclusion = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Rapport = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Audits", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Audits_Sites_SiteId",
                        column: x => x.SiteId,
                        principalTable: "Sites",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "Classements",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SiteId = table.Column<int>(type: "int", nullable: false),
                    Rubrique = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Alinea = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Designation = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: false),
                    Volume = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Regime = table.Column<int>(type: "int", nullable: false),
                    ArreteMinisteriel = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Classements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Classements_Sites_SiteId",
                        column: x => x.SiteId,
                        principalTable: "Sites",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Prescriptions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SiteId = table.Column<int>(type: "int", nullable: false),
                    Source = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Article = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Intitule = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Domaine = table.Column<int>(type: "int", nullable: false),
                    Periodicite = table.Column<int>(type: "int", nullable: false),
                    DerniereRealisation = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ProchaineEcheance = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Responsable = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    Conformite = table.Column<int>(type: "int", nullable: false),
                    Commentaire = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Prescriptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Prescriptions_Sites_SiteId",
                        column: x => x.SiteId,
                        principalTable: "Sites",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Textes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Titre = table.Column<string>(type: "nvarchar(600)", maxLength: 600, nullable: false),
                    Reference = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Type = table.Column<int>(type: "int", nullable: false),
                    Domaine = table.Column<int>(type: "int", nullable: false),
                    DatePublication = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DateApplication = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Url = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Resume = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Statut = table.Column<int>(type: "int", nullable: false),
                    Analyse = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AnalyseLe = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AnalysePar = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    RubriquesConcernees = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    DateDetection = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SourceVeilleId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Textes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Textes_SourcesVeille_SourceVeilleId",
                        column: x => x.SourceVeilleId,
                        principalTable: "SourcesVeille",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "Exigences",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TexteReglementaireId = table.Column<int>(type: "int", nullable: false),
                    Article = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Conformite = table.Column<int>(type: "int", nullable: false),
                    DateEvaluation = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EvaluePar = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    Preuves = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ProchaineEvaluation = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Exigences", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Exigences_Textes_TexteReglementaireId",
                        column: x => x.TexteReglementaireId,
                        principalTable: "Textes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Actions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Titre = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Type = table.Column<int>(type: "int", nullable: false),
                    Origine = table.Column<int>(type: "int", nullable: false),
                    Priorite = table.Column<int>(type: "int", nullable: false),
                    Responsable = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    DateCreation = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Echeance = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Statut = table.Column<int>(type: "int", nullable: false),
                    DateRealisation = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Efficacite = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TexteReglementaireId = table.Column<int>(type: "int", nullable: true),
                    ExigenceId = table.Column<int>(type: "int", nullable: true),
                    PrescriptionId = table.Column<int>(type: "int", nullable: true),
                    AuditId = table.Column<int>(type: "int", nullable: true),
                    AspectEnvironnementalId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Actions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Actions_Aspects_AspectEnvironnementalId",
                        column: x => x.AspectEnvironnementalId,
                        principalTable: "Aspects",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Actions_Audits_AuditId",
                        column: x => x.AuditId,
                        principalTable: "Audits",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Actions_Exigences_ExigenceId",
                        column: x => x.ExigenceId,
                        principalTable: "Exigences",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Actions_Prescriptions_PrescriptionId",
                        column: x => x.PrescriptionId,
                        principalTable: "Prescriptions",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Actions_Textes_TexteReglementaireId",
                        column: x => x.TexteReglementaireId,
                        principalTable: "Textes",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Actions_AspectEnvironnementalId",
                table: "Actions",
                column: "AspectEnvironnementalId");

            migrationBuilder.CreateIndex(
                name: "IX_Actions_AuditId",
                table: "Actions",
                column: "AuditId");

            migrationBuilder.CreateIndex(
                name: "IX_Actions_ExigenceId",
                table: "Actions",
                column: "ExigenceId");

            migrationBuilder.CreateIndex(
                name: "IX_Actions_PrescriptionId",
                table: "Actions",
                column: "PrescriptionId");

            migrationBuilder.CreateIndex(
                name: "IX_Actions_Statut_Echeance",
                table: "Actions",
                columns: new[] { "Statut", "Echeance" });

            migrationBuilder.CreateIndex(
                name: "IX_Actions_TexteReglementaireId",
                table: "Actions",
                column: "TexteReglementaireId");

            migrationBuilder.CreateIndex(
                name: "IX_Aspects_SiteId",
                table: "Aspects",
                column: "SiteId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetRoleClaims_RoleId",
                table: "AspNetRoleClaims",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "RoleNameIndex",
                table: "AspNetRoles",
                column: "NormalizedName",
                unique: true,
                filter: "[NormalizedName] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserClaims_UserId",
                table: "AspNetUserClaims",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserLogins_UserId",
                table: "AspNetUserLogins",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserRoles_RoleId",
                table: "AspNetUserRoles",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "EmailIndex",
                table: "AspNetUsers",
                column: "NormalizedEmail");

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                table: "AspNetUsers",
                column: "NormalizedUserName",
                unique: true,
                filter: "[NormalizedUserName] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Audits_SiteId",
                table: "Audits",
                column: "SiteId");

            migrationBuilder.CreateIndex(
                name: "IX_Classements_SiteId_Rubrique",
                table: "Classements",
                columns: new[] { "SiteId", "Rubrique" });

            migrationBuilder.CreateIndex(
                name: "IX_Exigences_TexteReglementaireId",
                table: "Exigences",
                column: "TexteReglementaireId");

            migrationBuilder.CreateIndex(
                name: "IX_Prescriptions_ProchaineEcheance",
                table: "Prescriptions",
                column: "ProchaineEcheance");

            migrationBuilder.CreateIndex(
                name: "IX_Prescriptions_SiteId",
                table: "Prescriptions",
                column: "SiteId");

            migrationBuilder.CreateIndex(
                name: "IX_Sites_Nom",
                table: "Sites",
                column: "Nom",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SourcesVeille_Url",
                table: "SourcesVeille",
                column: "Url",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Textes_SourceVeilleId",
                table: "Textes",
                column: "SourceVeilleId");

            migrationBuilder.CreateIndex(
                name: "IX_Textes_Statut",
                table: "Textes",
                column: "Statut");

            migrationBuilder.CreateIndex(
                name: "IX_Textes_Url",
                table: "Textes",
                column: "Url");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Actions");

            migrationBuilder.DropTable(
                name: "AspNetRoleClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserLogins");

            migrationBuilder.DropTable(
                name: "AspNetUserRoles");

            migrationBuilder.DropTable(
                name: "AspNetUserTokens");

            migrationBuilder.DropTable(
                name: "Classements");

            migrationBuilder.DropTable(
                name: "Objectifs");

            migrationBuilder.DropTable(
                name: "Aspects");

            migrationBuilder.DropTable(
                name: "Audits");

            migrationBuilder.DropTable(
                name: "Exigences");

            migrationBuilder.DropTable(
                name: "Prescriptions");

            migrationBuilder.DropTable(
                name: "AspNetRoles");

            migrationBuilder.DropTable(
                name: "AspNetUsers");

            migrationBuilder.DropTable(
                name: "Textes");

            migrationBuilder.DropTable(
                name: "Sites");

            migrationBuilder.DropTable(
                name: "SourcesVeille");
        }
    }
}
