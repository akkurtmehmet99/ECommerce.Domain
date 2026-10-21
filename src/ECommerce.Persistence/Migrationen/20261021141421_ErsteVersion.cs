using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ECommerce.Persistence.Migrationen
{
    /// <inheritdoc />
    public partial class ErsteVersion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Kategorien",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Beschreibung = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    Aktiv = table.Column<bool>(type: "INTEGER", nullable: false),
                    ErstelltAm = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    GeaendertAm = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    Geloescht = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Kategorien", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Kunden",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Vorname = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Nachname = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    EMail = table.Column<string>(type: "TEXT", maxLength: 254, nullable: false),
                    PasswortHash = table.Column<string>(type: "TEXT", maxLength: 512, nullable: false),
                    Rolle = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    Aktiv = table.Column<bool>(type: "INTEGER", nullable: false),
                    ErstelltAm = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    GeaendertAm = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    Geloescht = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Kunden", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Produkte",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Beschreibung = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    Preis = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    Lagerbestand = table.Column<int>(type: "INTEGER", nullable: false),
                    Aktiv = table.Column<bool>(type: "INTEGER", nullable: false),
                    KategorieId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ErstelltAm = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    GeaendertAm = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    Geloescht = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Produkte", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Produkte_Kategorien_KategorieId",
                        column: x => x.KategorieId,
                        principalTable: "Kategorien",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Bestellungen",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    KundeId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    Gesamtbetrag = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    ReservierungLaeuftAbAm = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    Liefername = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Lieferstrasse = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    LieferPostleitzahl = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    LieferStadt = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    LieferLand = table.Column<string>(type: "TEXT", maxLength: 2, nullable: false),
                    ErstelltAm = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    GeaendertAm = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    Geloescht = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Bestellungen", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Bestellungen_Kunden_KundeId",
                        column: x => x.KundeId,
                        principalTable: "Kunden",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Sitzungstoken",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    KundeId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Hash = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    Ablaufzeitpunkt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    WiderrufenAm = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    ErsetztDurchHash = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true),
                    ErstelltAm = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    GeaendertAm = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    Geloescht = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sitzungstoken", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Sitzungstoken_Kunden_KundeId",
                        column: x => x.KundeId,
                        principalTable: "Kunden",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Warenkoerbe",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    KundeId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ErstelltAm = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    GeaendertAm = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    Geloescht = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Warenkoerbe", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Warenkoerbe_Kunden_KundeId",
                        column: x => x.KundeId,
                        principalTable: "Kunden",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Bestellpositionen",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    BestellungId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ProduktId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Produktname = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Menge = table.Column<int>(type: "INTEGER", nullable: false),
                    Einzelpreis = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    ErstelltAm = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    GeaendertAm = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    Geloescht = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Bestellpositionen", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Bestellpositionen_Bestellungen_BestellungId",
                        column: x => x.BestellungId,
                        principalTable: "Bestellungen",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Bestellpositionen_Produkte_ProduktId",
                        column: x => x.ProduktId,
                        principalTable: "Produkte",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Zahlungen",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    BestellungId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    Art = table.Column<int>(type: "INTEGER", nullable: false),
                    Transaktionsreferenz = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    BezahltAm = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    ErstelltAm = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    GeaendertAm = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    Geloescht = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Zahlungen", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Zahlungen_Bestellungen_BestellungId",
                        column: x => x.BestellungId,
                        principalTable: "Bestellungen",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Warenkorbpositionen",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    WarenkorbId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ProduktId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Menge = table.Column<int>(type: "INTEGER", nullable: false),
                    ErstelltAm = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    GeaendertAm = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    Geloescht = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Warenkorbpositionen", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Warenkorbpositionen_Produkte_ProduktId",
                        column: x => x.ProduktId,
                        principalTable: "Produkte",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Warenkorbpositionen_Warenkoerbe_WarenkorbId",
                        column: x => x.WarenkorbId,
                        principalTable: "Warenkoerbe",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Bestellpositionen_BestellungId",
                table: "Bestellpositionen",
                column: "BestellungId");

            migrationBuilder.CreateIndex(
                name: "IX_Bestellpositionen_ProduktId",
                table: "Bestellpositionen",
                column: "ProduktId");

            migrationBuilder.CreateIndex(
                name: "IX_Bestellungen_KundeId_ErstelltAm",
                table: "Bestellungen",
                columns: new[] { "KundeId", "ErstelltAm" });

            migrationBuilder.CreateIndex(
                name: "IX_Bestellungen_Status_ReservierungLaeuftAbAm",
                table: "Bestellungen",
                columns: new[] { "Status", "ReservierungLaeuftAbAm" });

            migrationBuilder.CreateIndex(
                name: "IX_Kategorien_Name",
                table: "Kategorien",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Kunden_EMail",
                table: "Kunden",
                column: "EMail",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Produkte_KategorieId",
                table: "Produkte",
                column: "KategorieId");

            migrationBuilder.CreateIndex(
                name: "IX_Sitzungstoken_Hash",
                table: "Sitzungstoken",
                column: "Hash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Sitzungstoken_KundeId_Ablaufzeitpunkt",
                table: "Sitzungstoken",
                columns: new[] { "KundeId", "Ablaufzeitpunkt" });

            migrationBuilder.CreateIndex(
                name: "IX_Warenkoerbe_KundeId",
                table: "Warenkoerbe",
                column: "KundeId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Warenkorbpositionen_ProduktId",
                table: "Warenkorbpositionen",
                column: "ProduktId");

            migrationBuilder.CreateIndex(
                name: "IX_Warenkorbpositionen_WarenkorbId_ProduktId",
                table: "Warenkorbpositionen",
                columns: new[] { "WarenkorbId", "ProduktId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Zahlungen_BestellungId",
                table: "Zahlungen",
                column: "BestellungId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Bestellpositionen");

            migrationBuilder.DropTable(
                name: "Sitzungstoken");

            migrationBuilder.DropTable(
                name: "Warenkorbpositionen");

            migrationBuilder.DropTable(
                name: "Zahlungen");

            migrationBuilder.DropTable(
                name: "Produkte");

            migrationBuilder.DropTable(
                name: "Warenkoerbe");

            migrationBuilder.DropTable(
                name: "Bestellungen");

            migrationBuilder.DropTable(
                name: "Kategorien");

            migrationBuilder.DropTable(
                name: "Kunden");
        }
    }
}
