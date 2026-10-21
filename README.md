# ECommerce

Deutschsprachiger E-Commerce-Backend-Kern mit .NET 8, Clean Architecture, CQRS, MediatR, FluentValidation, EF Core und SQLite.

## Architektur

- `ECommerce.Domain`: Deutsche Geschäftsobjekte, Bestell-/Zahlungsstatus und Entitätsbasis.
- `ECommerce.Application`: CQRS-Befehle und -Abfragen, Validierung und Datenbank-/Token-Schnittstellen.
- `ECommerce.Persistence`: EF-Core-Konfiguration, SQLite, Migrationen, Passwort-Hashing und JWT-Erstellung.
- `ECommerce.API`: Geschützte Controller, deutsche Problem-Details, OpenAPI und HTTP-Beispiele.
- `tests/ECommerce.API.Tests`: End-to-End-Tests mit einer isolierten SQLite-Datenbank.

## Funktionen

- Kundenregistrierung und Anmeldung mit ASP.NET-Core-Passwort-Hasher.
- 20 Minuten gültige JWT-Zugriffstoken sowie 30 Tage gültige, gehashte und rotierende Aktualisierungstoken. Wiederverwendung eines widerrufenen Aktualisierungstokens widerruft die aktiven Sitzungen des Kontos.
- Rollen `Kunde` und `Administrator`. Die Rolle wird serverseitig festgelegt und kann nicht bei der Registrierung gewählt werden.
- Öffentlicher, paginierter Produktkatalog; Kategorien und Produkte werden administrativ verwaltet.
- Warenkorb pro Kunde mit Mengen- und Lagerprüfung.
- Checkout mit transaktionaler Lagerreservierung für 15 Minuten, Preis-Snapshot und Lieferadress-Snapshot; abgelaufene Reservierungen werden automatisch freigegeben.
- Sandbox-Zahlung ohne Erfassung echter Zahlungsdaten. Bei fehlgeschlagener Zahlung wird die Reservierung zurückgegeben.
- RFC-7807-Fehlerantworten, deutsche Validierungsmeldungen, Authentifizierungsratenbegrenzung und Datenbankbereitschaft unter `/gesundheit`.
- EF-Core-Migrationen statt `EnsureCreated`.

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

Im Entwicklungsmodus erzeugt die Anwendung bei fehlendem Schlüssel einen temporären Schlüssel und gibt eine Warnung aus. Zugriffstoken werden dadurch nach einem Neustart ungültig. Für stabile lokale Sitzungen den User-Secrets-Befehl verwenden.

Die Datenbank wird standardmäßig als `src/ECommerce.API/ecommerce.db` angelegt und beim Start migriert. Swagger ist im Entwicklungsmodus unter `/swagger` verfügbar. Für geschützte Endpunkte zuerst ein Kundenkonto registrieren oder einen initialen Administrator konfigurieren. Der Administrator wird nur angelegt, wenn **beide** Werte gesetzt sind:

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

Die API wendet vorhandene Migrationen beim Start an. Integrationstests verwenden getrennte temporäre SQLite-Dateien und testen Anmeldung, Rollen, Tokenrotation, Produktverwaltung, Warenkorb, Bestellung, Sandbox-Zahlung und Bestandsrückgabe.

## Betriebsgrenzen

Dies ist ein belastbarer Lern-/Portfolio-Backend-Kern, aber **noch kein vollständig produktionsreifes Marktplatzsystem**. SQLite eignet sich für lokale Nutzung und kleine Einzelinstanzen, nicht als Datenbank für horizontal skalierte Verkaufslast. Für Live-Betrieb sind unter anderem PostgreSQL/SQL Server, verteilte Ratenbegrenzung, strukturierte Observability, Backup-/Wiederherstellungstests, Datenschutz-/Kontolöschprozesse, E-Mail-Verifikation, Warenkorb-/Bestell-Paginierung und echte Zahlungs-/Versandintegrationen gesondert umzusetzen und abzusichern. Die enthaltene Zahlungsschnittstelle ist ausdrücklich nur eine Sandbox.
