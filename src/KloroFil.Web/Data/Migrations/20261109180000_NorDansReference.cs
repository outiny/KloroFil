using KloroFil.Web.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KloroFil.Web.Data.Migrations
{
    /// <summary>
    /// Les premiers imports Légifrance rangeaient le NOR au début du résumé (« NOR : XXXX – extrait »).
    /// On le déplace dans la colonne Reference, utilisée par le filtre de la veille. Aucun changement de schéma.
    /// </summary>
    [DbContext(typeof(KloroFilDbContext))]
    [Migration("20261109180000_NorDansReference")]
    public partial class NorDansReference : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                UPDATE Textes
                SET Reference = LEFT(SUBSTRING(Resume, 7, CHARINDEX(N' ', Resume + N' ', 7) - 7), 100)
                WHERE Reference IS NULL AND Resume LIKE N'NOR : %';

                UPDATE Textes
                SET Resume = CASE WHEN CHARINDEX(N' – ', Resume) > 0
                                  THEN SUBSTRING(Resume, CHARINDEX(N' – ', Resume) + 3, LEN(Resume))
                                  ELSE NULL END
                WHERE Resume LIKE N'NOR : %';
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Données uniquement : rien à défaire.
        }
    }
}
