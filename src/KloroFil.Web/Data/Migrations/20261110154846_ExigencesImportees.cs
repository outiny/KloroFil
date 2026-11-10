using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KloroFil.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class ExigencesImportees : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "Importee",
                table: "Exigences",
                type: "bit",
                nullable: false,
                defaultValue: false);

            // Exigences déjà importées par « Importer les articles et annexes » : numérotation « Art. … » ou « Annexe … »
            // sur un texte lié à Légifrance. Les autres restent considérées comme saisies.
            migrationBuilder.Sql("""
                UPDATE e SET Importee = 1
                FROM Exigences e INNER JOIN Textes t ON t.Id = e.TexteReglementaireId
                WHERE t.Url LIKE N'%legifrance.gouv.fr%' AND (e.Article LIKE N'Art. %' OR e.Article LIKE N'Annexe%');
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Importee",
                table: "Exigences");
        }
    }
}
