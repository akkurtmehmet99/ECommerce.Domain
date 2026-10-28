# ECommerce

Vollständige deutschsprachige E-Commerce-Demoanwendung mit Shop-Oberfläche, Administrationsbereich und API auf .NET 8, Clean Architecture, CQRS, MediatR, FluentValidation, EF Core und SQLite.

## Architektur

- `ECommerce.Domain`: Deutsche Geschäftsobjekte, Bestell-/Zahlungsstatus und Entitätsbasis.
- `ECommerce.Application`: CQRS-Befehle und -Abfragen, Validierung und Datenbank-/Token-Schnittstellen.
- `ECommerce.Persistence`: EF-Core-Konfiguration, SQLite, Migrationen, Passwort-Hashing und JWT-Erstellung.
- `ECommerce.API`: Responsive Razor-Pages-Anwendung, geschützte API-Controller, deutsche Problem-Details, OpenAPI und HTTP-Beispiele.
- `tests/ECommerce.API.Tests`: End-to-End-Tests für API und Webabläufe mit einer isolierten SQLite-Datenbank.

## Funktionen

- Kundenregistrierung und Anmeldung mit ASP.NET-Core-Passwort-Hasher.
- 20 Minuten gültige JWT-Zugriffstoken sowie 30 Tage gültige, gehashte und rotierende Aktualisierungstoken. Wiederverwendung eines widerrufenen Aktualisierungstokens widerruft die aktiven Sitzungen des Kontos.
- Rollen `Kunde` und `Administrator`. Die Rolle wird serverseitig festgelegt und kann nicht bei der Registrierung gewählt werden.
- Deutsche Shop-Oberfläche mit Produktkatalog, Suche, Kategorien, Registrierung, Anmeldung, Warenkorb, Lieferadresse, Bestellübersicht und Sandbox-Zahlung.
- Geschützter Administrationsbereich für Produkte, Kategorien und Bestellstatus.
- Öffentlicher, paginierter Produktkatalog; Suche und Kategorie-Filter stehen auch über die API zur Verfügung.
- Warenkorb pro Kunde mit Mengen- und Lagerprüfung.
- Checkout mit transaktionaler Lagerreservierung für 15 Minuten, Preis-Snapshot und Lieferadress-Snapshot; abgelaufene Reservierungen werden automatisch freigegeben.
- Sandbox-Zahlung ohne Erfassung echter Zahlungsdaten. Bei fehlgeschlagener Zahlung wird die Reservierung zurückgegeben.
- RFC-7807-Fehlerantworten, deutsche Validierungsmeldungen, Authentifizierungsratenbegrenzung und Datenbankbereitschaft unter `/gesundheit`.
- EF-Core-Migrationen statt `EnsureCreated`.
- SQLite-Datenbankregeln gegen ungültige Lagerbestände und Mengen sowie zusätzliche Indizes für Katalog- und Kategorieabfragen.
- Im Entwicklungsmodus werden beim ersten Start deutsche Beispieldaten angelegt, damit Shop und Suche sofort ausprobiert werden können. Produktions- und Testdatenbanken werden nicht automatisch mit Beispielen befüllt.

## Lokal starten

Voraussetzung: .NET 8 SDK.

```bash
dotnet restore ECommerce.sln
dotnet build ECommerce.sln
dotnet test ECommerce.sln
dotnet user-secrets init --project src/ECommerce.API
dotnet user-secrets set "Authentifizierung:Signaturschluessel" "$(openssl rand -base64 48)" --project src/ECommerce.API
dotnet run --project src/ECommerce.API/ECommerce.API.csproj
```

Im Entwicklungsmodus erzeugt die Anwendung bei fehlendem Schlüssel einen temporären Schlüssel und gibt eine Warnung aus. API-Zugriffstoken werden dadurch nach einem Neustart ungültig. Für stabile lokale Sitzungen den User-Secrets-Befehl verwenden.

Die Datenbank wird standardmäßig als `src/ECommerce.API/ecommerce.db` angelegt und beim Start migriert. Nach dem Start ist der Shop unter der von ASP.NET Core ausgegebenen lokalen Adresse erreichbar; Swagger steht im Entwicklungsmodus unter `/swagger` bereit. Ein Kundenkonto lässt sich direkt in der Oberfläche anlegen. Den Administrationsbereich erreichst du erst nach der Konfiguration eines initialen Administrators. Dieser wird nur angelegt, wenn **beide** Werte gesetzt sind:

```bash
dotnet user-secrets set "Administrator:EMail" "admin@example.de" --project src/ECommerce.API
dotnet user-secrets set "Administrator:Passwort" "Ein-langes-einmaliges-Admin-Passwort!" --project src/ECommerce.API
```

Das Administratorpasswort muss mindestens 16 Zeichen lang sein. Geheimnisse gehören weder in `appsettings.json` noch in Versionskontrolle.

## Container

```bash
cp .env.example .env
```

`JWT_SIGNING_KEY` in `.env` mit einem zufälligen Wert von mindestens 32 Bytes befüllen; optional zusätzlich `ADMIN_EMAIL` und ein mindestens 16 Zeichen langes `ADMIN_PASSWORD` setzen. Anschließend:

```bash
docker compose up --build
```

Die SQLite-Datei liegt im persistenten Docker-Volume `ecommerce-daten`; der Container läuft als nicht privilegierter Benutzer. TLS sollte am vorgeschalteten Reverse Proxy terminiert werden. `/gesundheit` prüft die Datenbankverbindung.

## API-Übersicht

Alle Routen und realistischen Beispielaufrufe stehen in [ECommerce.API.http](./src/ECommerce.API/ECommerce.API.http). Die wichtigsten Routen:

| Methode | Route | Berechtigung |
|---|---|---|
| `POST` | `/api/authentifizierung/registrieren` | Öffentlich, begrenzt |
| `POST` | `/api/authentifizierung/anmelden` | Öffentlich, begrenzt |
| `POST` | `/api/authentifizierung/token-erneuern` | Aktualisierungstoken, begrenzt |
| `POST` | `/api/authentifizierung/abmelden` | Aktualisierungstoken, begrenzt |
| `GET` | `/api/authentifizierung/profil` | Kunde |
| `GET` | `/api/kategorien` | Öffentlich |
| `POST`, `PUT`, `DELETE` | `/api/kategorien` | Administrator |
| `GET` | `/api/produkte?seite=1&seitengroesse=20` | Öffentlich, maximal 100 Einträge pro Seite |
| `GET` | `/api/produkte?suchbegriff=lampe&kategorieId=<guid>` | Öffentlich, optionale Katalogsuche und Kategorie |
| `GET` | `/api/produkte/{id}` | Öffentlich |
| `POST`, `PUT`, `DELETE` | `/api/produkte` | Administrator |
| `GET`, `POST`, `PUT`, `DELETE` | `/api/warenkorb` | Angemeldeter Kunde |
| `GET`, `POST` | `/api/bestellungen` | Angemeldeter Kunde |
| `GET` | `/api/bestellungen/verwaltung` | Administrator, paginiert |
| `PATCH` | `/api/bestellungen/{id}/status` | Administrator |
| `POST` | `/api/zahlungen/{bestellungId}/sandbox` | Angemeldeter Kunde, Sandbox |

Kunden dürfen nur ihre eigenen Warenkörbe und Bestellungen lesen oder ändern. Bestellstatus können nur über erlaubte Übergänge geändert werden. Geldbeträge verwenden EUR mit zwei Nachkommastellen; das Lieferland wird als zweistelliger ISO-Code erwartet.

## Migrationen und Tests

Eine neue Migration erstellen:

```bash
dotnet tool install --global dotnet-ef --version 8.0.32
dotnet ef migrations add DeutscheAenderung \
  --project src/ECommerce.Persistence/ECommerce.Persistence.csproj \
  --startup-project src/ECommerce.API/ECommerce.API.csproj \
  --output-dir Migrationen
```

Die Anwendung wendet vorhandene Migrationen beim Start an. Integrationstests verwenden getrennte temporäre SQLite-Dateien und testen Anmeldung, Rollen, Tokenrotation, Produktverwaltung, Katalogfilter, Warenkorb, Bestellung, Sandbox-Zahlung und Bestandsrückgabe. Ein zusätzlicher Webtest durchläuft Registrierung, Warenkorb, Checkout und simulierte Zahlung.

## Betriebsgrenzen

Dies ist eine vollständige Lern-/Portfolio-Shopanwendung, aber **noch kein produktionsreifes Marktplatzsystem**. SQLite eignet sich für lokale Nutzung und kleine Einzelinstanzen, nicht als Datenbank für horizontal skalierte Verkaufslast. Für Live-Betrieb sind unter anderem PostgreSQL/SQL Server, verteilte Ratenbegrenzung, strukturierte Observability, Backup-/Wiederherstellungstests, Datenschutz-/Kontolöschprozesse, E-Mail-Verifikation, Warenkorb-/Bestell-Paginierung und echte Zahlungs-/Versandintegrationen gesondert umzusetzen und abzusichern. Die enthaltene Zahlungsschnittstelle ist ausdrücklich nur eine Sandbox.
