using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KloroFil.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class EvaluationsParPhase : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Evaluations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ExigenceId = table.Column<int>(type: "int", nullable: false),
                    Phase = table.Column<int>(type: "int", nullable: false),
                    Conformite = table.Column<int>(type: "int", nullable: false),
                    DateEvaluation = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EvaluePar = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    CriteresEvaluation = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MoyensSurveillance = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ActionsCorrectives = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Preuves = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Evaluations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Evaluations_Exigences_ExigenceId",
                        column: x => x.ExigenceId,
                        principalTable: "Exigences",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Evaluations_ExigenceId_Phase",
                table: "Evaluations",
                columns: new[] { "ExigenceId", "Phase" },
                unique: true);

            // Les évaluations déjà saisies deviennent l'évaluation « Législation » (Phase = 0) de leur exigence.
            migrationBuilder.Sql("""
                INSERT INTO Evaluations (ExigenceId, Phase, Conformite, DateEvaluation, EvaluePar, Preuves)
                SELECT Id, 0, Conformite, DateEvaluation, EvaluePar, Preuves
                FROM Exigences
                WHERE Conformite <> 0 OR DateEvaluation IS NOT NULL OR Preuves IS NOT NULL;
                """);

            migrationBuilder.DropColumn(
                name: "Preuves",
                table: "Exigences");

            migrationBuilder.DropColumn(
                name: "ProchaineEvaluation",
                table: "Exigences");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Evaluations");

            migrationBuilder.AddColumn<string>(
                name: "Preuves",
                table: "Exigences",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ProchaineEvaluation",
                table: "Exigences",
                type: "datetime2",
                nullable: true);
        }
    }
}
