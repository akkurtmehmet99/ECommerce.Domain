using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ECommerce.Persistence.Migrationen
{
    /// <inheritdoc />
    public partial class KatalogsucheUndDatenbankregeln : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddCheckConstraint(
                name: "CK_Warenkorbpositionen_Menge_Gueltig",
                table: "Warenkorbpositionen",
                sql: "\"Menge\" BETWEEN 1 AND 100");

            migrationBuilder.CreateIndex(
                name: "IX_Produkte_Aktiv_Geloescht_KategorieId_Name",
                table: "Produkte",
                columns: new[] { "Aktiv", "Geloescht", "KategorieId", "Name" });

            migrationBuilder.AddCheckConstraint(
                name: "CK_Produkte_Lagerbestand_NichtNegativ",
                table: "Produkte",
                sql: "\"Lagerbestand\" >= 0");

            migrationBuilder.CreateIndex(
                name: "IX_Kategorien_Aktiv_Geloescht_Name",
                table: "Kategorien",
                columns: new[] { "Aktiv", "Geloescht", "Name" });

            migrationBuilder.AddCheckConstraint(
                name: "CK_Bestellpositionen_Menge_Positiv",
                table: "Bestellpositionen",
                sql: "\"Menge\" > 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Warenkorbpositionen_Menge_Gueltig",
                table: "Warenkorbpositionen");

            migrationBuilder.DropIndex(
                name: "IX_Produkte_Aktiv_Geloescht_KategorieId_Name",
                table: "Produkte");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Produkte_Lagerbestand_NichtNegativ",
                table: "Produkte");

            migrationBuilder.DropIndex(
                name: "IX_Kategorien_Aktiv_Geloescht_Name",
                table: "Kategorien");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Bestellpositionen_Menge_Positiv",
                table: "Bestellpositionen");
        }
    }
}
