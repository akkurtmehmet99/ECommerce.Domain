using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using ECommerce.Application.Common.Interfaces;
using ECommerce.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace ECommerce.API.Tests;

[CollectionDefinition("ECommerceIntegration", DisableParallelization = true)]
public sealed class ECommerceIntegrationCollection { }

[Collection("ECommerceIntegration")]
public sealed class CommerceAblaufTests : IAsyncLifetime
{
    private static readonly string[] Umgebungsvariablen =
    [
        "ConnectionStrings__ECommerce",
        "Authentifizierung__Signaturschluessel",
        "Administrator__EMail",
        "Administrator__Passwort",
        "Administrator__Vorname",
        "Administrator__Nachname"
    ];

    private readonly string _datenbankpfad = Path.Combine(
        Path.GetTempPath(),
        $"ecommerce-tests-{Guid.NewGuid():N}.db");
    private readonly Dictionary<string, string?> _vorherigeUmgebung =
        Umgebungsvariablen.ToDictionary(
            schluessel => schluessel,
            Environment.GetEnvironmentVariable);
    private WebApplicationFactory<Program> _anwendung = null!;
    private HttpClient _client = null!;

    public Task InitializeAsync()
    {
        Environment.SetEnvironmentVariable(
            "ConnectionStrings__ECommerce",
            $"Data Source={_datenbankpfad}");
        Environment.SetEnvironmentVariable(
            "Authentifizierung__Signaturschluessel",
            "Integrationstest-Geheimnis-mit-ausreichender-Laenge-123456");
        Environment.SetEnvironmentVariable("Administrator__EMail", "admin@example.de");
        Environment.SetEnvironmentVariable("Administrator__Passwort", "Admin-Sicheres-Passwort-2026!");
        Environment.SetEnvironmentVariable("Administrator__Vorname", "Test");
        Environment.SetEnvironmentVariable("Administrator__Nachname", "Administrator");

        _anwendung = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Testing");
            });
        _client = _anwendung.CreateClient();
        return Task.CompletedTask;
    }

    [Fact]
    public async Task KundeKannKaufenUndAktualisierungstokenWerdenEinmaligRotiert()
    {
        var nichtAutorisiert = await _client.PostAsJsonAsync(
            "/api/kategorien",
            new { name = "Unbefugt", beschreibung = "" });
        Assert.Equal(HttpStatusCode.Unauthorized, nichtAutorisiert.StatusCode);

        var admin = await AnmeldenAsync("admin@example.de", "Admin-Sicheres-Passwort-2026!");
        var kundeRegistrierung = await _client.PostAsJsonAsync(
            "/api/authentifizierung/registrieren",
            new
            {
                vorname = "Anna",
                nachname = "Beispiel",
                email = "anna@example.de",
                passwort = "Kunden-Passwort-2026!"
            });
        Assert.Equal(HttpStatusCode.Created, kundeRegistrierung.StatusCode);
        using var kundenJson = JsonDocument.Parse(await kundeRegistrierung.Content.ReadAsStringAsync());
        var kundenToken = kundenJson.RootElement.GetProperty("zugriffstoken").GetString()!;
        var altesAktualisierungstoken =
            kundenJson.RootElement.GetProperty("aktualisierungstoken").GetString()!;

        var tokenErneuerung = await _client.PostAsJsonAsync(
            "/api/authentifizierung/token-erneuern",
            new { aktualisierungstoken = altesAktualisierungstoken });
        Assert.Equal(HttpStatusCode.OK, tokenErneuerung.StatusCode);
        using var tokenJson = JsonDocument.Parse(await tokenErneuerung.Content.ReadAsStringAsync());
        kundenToken = tokenJson.RootElement.GetProperty("zugriffstoken").GetString()!;
        var neuesAktualisierungstoken =
            tokenJson.RootElement.GetProperty("aktualisierungstoken").GetString()!;
        Assert.NotEqual(altesAktualisierungstoken, neuesAktualisierungstoken);

        var wiederverwendetesToken = await _client.PostAsJsonAsync(
            "/api/authentifizierung/token-erneuern",
            new { aktualisierungstoken = altesAktualisierungstoken });
        Assert.True(
            wiederverwendetesToken.StatusCode == HttpStatusCode.Unauthorized,
            await wiederverwendetesToken.Content.ReadAsStringAsync());

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", kundenToken);
        var kundenAdminVersuch = await _client.PostAsJsonAsync(
            "/api/kategorien",
            new { name = "Unbefugte Kategorie", beschreibung = "" });
        Assert.Equal(HttpStatusCode.Forbidden, kundenAdminVersuch.StatusCode);

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", admin);
        var kategorieAntwort = await _client.PostAsJsonAsync(
            "/api/kategorien",
            new { name = "Elektronik", beschreibung = "Elektronische Geräte" });
        Assert.Equal(HttpStatusCode.Created, kategorieAntwort.StatusCode);
        using var kategorieJson = JsonDocument.Parse(await kategorieAntwort.Content.ReadAsStringAsync());
        var kategorieId = kategorieJson.RootElement.GetProperty("id").GetGuid();

        var produktAntwort = await _client.PostAsJsonAsync(
            "/api/produkte",
            new
            {
                name = "Kaffeemaschine",
                beschreibung = "Filterkaffeemaschine",
                preis = 79.99m,
                lagerbestand = 3,
                kategorieId
            });
        Assert.True(
            produktAntwort.StatusCode == HttpStatusCode.Created,
            await produktAntwort.Content.ReadAsStringAsync());
        using var produktJson = JsonDocument.Parse(await produktAntwort.Content.ReadAsStringAsync());
        var produktId = produktJson.RootElement.GetProperty("id").GetGuid();
        Assert.NotNull(produktAntwort.Headers.Location);

        var gefilterteProdukte = await _client.GetFromJsonAsync<JsonElement>(
            $"/api/produkte?suchbegriff=kaffee&kategorieId={kategorieId}");
        Assert.Equal(1, gefilterteProdukte.GetProperty("gesamtanzahl").GetInt32());

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", kundenToken);
        var warenkorbAntwort = await _client.PostAsJsonAsync(
            $"/api/warenkorb/positionen/{produktId}",
            new { menge = 2 });
        Assert.Equal(HttpStatusCode.OK, warenkorbAntwort.StatusCode);

        var bestellungAntwort = await _client.PostAsJsonAsync(
            "/api/bestellungen",
            new
            {
                liefername = "Anna Beispiel",
                lieferstrasse = "Musterstraße 1",
                lieferpostleitzahl = "10115",
                lieferstadt = "Berlin",
                lieferland = "DE"
            });
        Assert.Equal(HttpStatusCode.Created, bestellungAntwort.StatusCode);
        using var bestellungJson = JsonDocument.Parse(await bestellungAntwort.Content.ReadAsStringAsync());
        var bestellungId = bestellungJson.RootElement.GetProperty("id").GetGuid();
        Assert.Equal(159.98m, bestellungJson.RootElement.GetProperty("gesamtbetrag").GetDecimal());

        var zahlungAntwort = await _client.PostAsJsonAsync(
            $"/api/zahlungen/{bestellungId}/sandbox",
            new { erfolgreich = true });
        Assert.Equal(HttpStatusCode.OK, zahlungAntwort.StatusCode);
        using var zahlungJson = JsonDocument.Parse(await zahlungAntwort.Content.ReadAsStringAsync());
        Assert.Equal("Erfolgreich", zahlungJson.RootElement.GetProperty("status").GetString());

        var meineBestellung = await _client.GetAsync($"/api/bestellungen/{bestellungId}");
        Assert.Equal(HttpStatusCode.OK, meineBestellung.StatusCode);
        using var meineBestellungJson = JsonDocument.Parse(await meineBestellung.Content.ReadAsStringAsync());
        Assert.Equal("Bezahlt", meineBestellungJson.RootElement.GetProperty("status").GetString());

        await _client.PostAsJsonAsync(
            $"/api/warenkorb/positionen/{produktId}",
            new { menge = 1 });
        var fehlgeschlageneBestellungAntwort = await _client.PostAsJsonAsync(
            "/api/bestellungen",
            new
            {
                liefername = "Anna Beispiel",
                lieferstrasse = "Musterstraße 1",
                lieferpostleitzahl = "10115",
                lieferstadt = "Berlin",
                lieferland = "DE"
            });
        Assert.Equal(HttpStatusCode.Created, fehlgeschlageneBestellungAntwort.StatusCode);
        using var fehlgeschlageneBestellungJson =
            JsonDocument.Parse(await fehlgeschlageneBestellungAntwort.Content.ReadAsStringAsync());
        var fehlgeschlageneBestellungId =
            fehlgeschlageneBestellungJson.RootElement.GetProperty("id").GetGuid();

        var fehlgeschlageneZahlungAntwort = await _client.PostAsJsonAsync(
            $"/api/zahlungen/{fehlgeschlageneBestellungId}/sandbox",
            new { erfolgreich = false });
        Assert.Equal(HttpStatusCode.OK, fehlgeschlageneZahlungAntwort.StatusCode);
        using var fehlgeschlageneZahlungJson =
            JsonDocument.Parse(await fehlgeschlageneZahlungAntwort.Content.ReadAsStringAsync());
        Assert.Equal("Fehlgeschlagen", fehlgeschlageneZahlungJson.RootElement.GetProperty("status").GetString());

        var produktNachErstattung = await _client.GetAsync($"/api/produkte/{produktId}");
        Assert.Equal(HttpStatusCode.OK, produktNachErstattung.StatusCode);
        using var produktNachErstattungJson =
            JsonDocument.Parse(await produktNachErstattung.Content.ReadAsStringAsync());
        Assert.Equal(1, produktNachErstattungJson.RootElement.GetProperty("lagerbestand").GetInt32());

        await _client.PostAsJsonAsync(
            $"/api/warenkorb/positionen/{produktId}",
            new { menge = 1 });
        var reservierteBestellungAntwort = await _client.PostAsJsonAsync(
            "/api/bestellungen",
            new
            {
                liefername = "Anna Beispiel",
                lieferstrasse = "Musterstraße 1",
                lieferpostleitzahl = "10115",
                lieferstadt = "Berlin",
                lieferland = "DE"
            });
        Assert.Equal(HttpStatusCode.Created, reservierteBestellungAntwort.StatusCode);
        using var reservierteBestellungJson =
            JsonDocument.Parse(await reservierteBestellungAntwort.Content.ReadAsStringAsync());
        var reservierteBestellungId =
            reservierteBestellungJson.RootElement.GetProperty("id").GetGuid();

        await using (var bereich = _anwendung.Services.CreateAsyncScope())
        {
            var kontext = bereich.ServiceProvider.GetRequiredService<AnwendungDbContext>();
            var bestellung = await kontext.Bestellungen.SingleAsync(
                eintrag => eintrag.Id == reservierteBestellungId);
            bestellung.ReservierungLaeuftAbAm = DateTimeOffset.UtcNow.AddMinutes(-1);
            await kontext.SaveChangesAsync();

            var reservierungsDienst =
                bereich.ServiceProvider.GetRequiredService<IReservierungsDienst>();
            Assert.True(await reservierungsDienst.AbgelaufeneBestellungFreigebenAsync(
                reservierteBestellungId,
                CancellationToken.None));
        }

        var abgelaufeneBestellungAntwort =
            await _client.GetAsync($"/api/bestellungen/{reservierteBestellungId}");
        Assert.Equal(HttpStatusCode.OK, abgelaufeneBestellungAntwort.StatusCode);
        using var abgelaufeneBestellungJson =
            JsonDocument.Parse(await abgelaufeneBestellungAntwort.Content.ReadAsStringAsync());
        Assert.Equal("Abgelaufen", abgelaufeneBestellungJson.RootElement.GetProperty("status").GetString());
    }

    [Fact]
    public async Task WeboberflaecheErmoeglichtRegistrierungWarenkorbUndSandboxBestellung()
    {
        using var webClient = _anwendung.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        var startseite = await webClient.GetAsync("/");
        Assert.Equal(HttpStatusCode.OK, startseite.StatusCode);
        Assert.Contains("Morgenmarkt", await startseite.Content.ReadAsStringAsync());

        var geschuetzteVerwaltung = await webClient.GetAsync("/Verwaltung/Index");
        Assert.Equal(HttpStatusCode.Redirect, geschuetzteVerwaltung.StatusCode);
        Assert.Contains("/Konto/Anmelden", geschuetzteVerwaltung.Headers.Location?.ToString());

        var adminToken = await AnmeldenAsync("admin@example.de", "Admin-Sicheres-Passwort-2026!");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
        var kategorieAntwort = await _client.PostAsJsonAsync(
            "/api/kategorien",
            new { name = "Wohnen", beschreibung = "Schöne Dinge für zu Hause" });
        Assert.Equal(HttpStatusCode.Created, kategorieAntwort.StatusCode);
        using var kategorieJson = JsonDocument.Parse(await kategorieAntwort.Content.ReadAsStringAsync());
        var kategorieId = kategorieJson.RootElement.GetProperty("id").GetGuid();

        var produktAntwort = await _client.PostAsJsonAsync(
            "/api/produkte",
            new
            {
                name = "Leselampe",
                beschreibung = "Eine angenehme Lampe für den Abend.",
                preis = 42.50m,
                lagerbestand = 4,
                kategorieId
            });
        Assert.Equal(HttpStatusCode.Created, produktAntwort.StatusCode);
        using var produktJson = JsonDocument.Parse(await produktAntwort.Content.ReadAsStringAsync());
        var produktId = produktJson.RootElement.GetProperty("id").GetGuid();

        using var adminWebClient = _anwendung.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
        var anmeldeseite = await adminWebClient.GetAsync("/Konto/Anmelden");
        var anmeldeseiteHtml = await anmeldeseite.Content.ReadAsStringAsync();
        using var adminAnmeldung = await adminWebClient.PostAsync(
            "/Konto/Anmelden",
            FormularMitToken(anmeldeseiteHtml, new Dictionary<string, string>
            {
                ["Eingabe.EMail"] = "admin@example.de",
                ["Eingabe.Passwort"] = "Admin-Sicheres-Passwort-2026!"
            }));
        Assert.Equal(HttpStatusCode.Redirect, adminAnmeldung.StatusCode);

        var produktverwaltung = await adminWebClient.GetAsync("/Verwaltung/Produkte");
        var produktverwaltungHtml = await produktverwaltung.Content.ReadAsStringAsync();
        using var produktAnlage = await adminWebClient.PostAsync(
            "/Verwaltung/Produkte?handler=ProduktErstellen",
            FormularMitToken(produktverwaltungHtml, new Dictionary<string, string>
            {
                ["name"] = "Handgemachte Vase",
                ["beschreibung"] = "Eine Vase für frische Blumen.",
                ["preis"] = "29,90",
                ["lagerbestand"] = "5",
                ["kategorieId"] = kategorieId.ToString()
            }));
        Assert.True(
            produktAnlage.StatusCode == HttpStatusCode.Redirect,
            await produktAnlage.Content.ReadAsStringAsync());
        var sortimentVerwaltung = await adminWebClient.GetAsync("/Verwaltung/Produkte");
        Assert.Contains("Handgemachte Vase", await sortimentVerwaltung.Content.ReadAsStringAsync());

        var registrierung = await webClient.GetAsync("/Konto/Registrieren");
        var registrierungsHtml = await registrierung.Content.ReadAsStringAsync();
        using var registrierungsAntwort = await webClient.PostAsync(
            "/Konto/Registrieren",
            FormularMitToken(registrierungsHtml, new Dictionary<string, string>
            {
                ["Eingabe.Vorname"] = "Mia",
                ["Eingabe.Nachname"] = "Muster",
                ["Eingabe.EMail"] = "mia@example.de",
                ["Eingabe.Passwort"] = "Sicheres-Passwort-2026!"
            }));
        Assert.Equal(HttpStatusCode.Redirect, registrierungsAntwort.StatusCode);

        var kundenVerwaltung = await webClient.GetAsync("/Verwaltung/Index");
        Assert.Equal(HttpStatusCode.Redirect, kundenVerwaltung.StatusCode);
        Assert.Contains("/Konto/KeinZugriff", kundenVerwaltung.Headers.Location?.ToString());

        var sortiment = await webClient.GetAsync("/");
        Assert.True(sortiment.IsSuccessStatusCode, await sortiment.Content.ReadAsStringAsync());
        var sortimentHtml = await sortiment.Content.ReadAsStringAsync();
        Assert.Contains("Leselampe", sortimentHtml);
        using var warenkorbAntwort = await webClient.PostAsync(
            "/?handler=InWarenkorb",
            FormularMitToken(sortimentHtml, new Dictionary<string, string>
            {
                ["produktId"] = produktId.ToString()
            }));
        Assert.Equal(HttpStatusCode.Redirect, warenkorbAntwort.StatusCode);

        var warenkorb = await webClient.GetAsync("/Warenkorb/Index");
        Assert.Contains("Leselampe", await warenkorb.Content.ReadAsStringAsync());

        var kasse = await webClient.GetAsync("/Bestellungen/Checkout");
        var kassenHtml = await kasse.Content.ReadAsStringAsync();
        using var bestellAntwort = await webClient.PostAsync(
            "/Bestellungen/Checkout",
            FormularMitToken(kassenHtml, new Dictionary<string, string>
            {
                ["Adresse.Liefername"] = "Mia Muster",
                ["Adresse.Lieferstrasse"] = "Musterstraße 4",
                ["Adresse.LieferPostleitzahl"] = "10115",
                ["Adresse.LieferStadt"] = "Berlin",
                ["Adresse.LieferLand"] = "DE"
            }));
        Assert.Equal(HttpStatusCode.Redirect, bestellAntwort.StatusCode);
        var detailsUrl = bestellAntwort.Headers.Location!.ToString();

        var bestelldetails = await webClient.GetAsync(detailsUrl);
        var detailsHtml = await bestelldetails.Content.ReadAsStringAsync();
        Assert.Contains("Zahlung ausstehend", detailsHtml);
        using var zahlungsAntwort = await webClient.PostAsync(
            $"{detailsUrl}?handler=Zahlen",
            FormularMitToken(detailsHtml, new Dictionary<string, string>()));
        Assert.Equal(HttpStatusCode.Redirect, zahlungsAntwort.StatusCode);

        var bezahlteBestellung = await webClient.GetAsync(detailsUrl);
        var bezahlteBestellungHtml = await bezahlteBestellung.Content.ReadAsStringAsync();
        Assert.Contains("Bezahlt", bezahlteBestellungHtml);

        var bestellverwaltung = await adminWebClient.GetAsync("/Verwaltung/Bestellungen");
        var bestellverwaltungHtml = await bestellverwaltung.Content.ReadAsStringAsync();
        var bestellungId = Guid.Parse(detailsUrl.TrimEnd('/').Split('/').Last());
        using var statusAenderung = await adminWebClient.PostAsync(
            "/Verwaltung/Bestellungen?handler=StatusAendern",
            FormularMitToken(produktverwaltungHtml, new Dictionary<string, string>
            {
                ["bestellungId"] = bestellungId.ToString(),
                ["status"] = "InBearbeitung"
            }));
        Assert.Equal(HttpStatusCode.Redirect, statusAenderung.StatusCode);
        var aktualisierteBestellverwaltung = await adminWebClient.GetAsync("/Verwaltung/Bestellungen");
        var aktualisierteBestellverwaltungHtml = await aktualisierteBestellverwaltung.Content.ReadAsStringAsync();
        Assert.True(
            aktualisierteBestellverwaltung.IsSuccessStatusCode,
            aktualisierteBestellverwaltungHtml);
        Assert.Contains("In Bearbeitung", aktualisierteBestellverwaltungHtml);

        using var abmeldeAntwort = await webClient.PostAsync(
            "/Konto/Abmelden",
            FormularMitToken(kassenHtml, new Dictionary<string, string>()));
        Assert.Equal(HttpStatusCode.Redirect, abmeldeAntwort.StatusCode);
        var abgemeldeterWarenkorb = await webClient.GetAsync("/Warenkorb/Index");
        Assert.Equal(HttpStatusCode.Redirect, abgemeldeterWarenkorb.StatusCode);
        Assert.Contains("/Konto/Anmelden", abgemeldeterWarenkorb.Headers.Location?.ToString());
    }

    [Fact]
    public async Task UngueltigeAnmeldungUndValidierungLiefernProblemDetails()
    {
        var anmeldung = await _client.PostAsJsonAsync(
            "/api/authentifizierung/anmelden",
            new { email = "unbekannt@example.de", passwort = "Falsches-Passwort-2026!" });
        Assert.Equal(HttpStatusCode.Unauthorized, anmeldung.StatusCode);
        Assert.Contains("application/problem+json", anmeldung.Content.Headers.ContentType?.MediaType);

        var registrierung = await _client.PostAsJsonAsync(
            "/api/authentifizierung/registrieren",
            new
            {
                vorname = "",
                nachname = "Beispiel",
                email = "ungültig",
                passwort = "kurz"
            });
        Assert.Equal(HttpStatusCode.BadRequest, registrierung.StatusCode);
        using var problem = JsonDocument.Parse(await registrierung.Content.ReadAsStringAsync());
        Assert.Equal("Ungültige Anfrage.", problem.RootElement.GetProperty("title").GetString());
    }

    public async Task DisposeAsync()
    {
        _client.Dispose();
        await _anwendung.DisposeAsync();
        foreach (var endung in new[] { "", "-shm", "-wal" })
        {
            var datei = _datenbankpfad + endung;
            if (File.Exists(datei))
            {
                File.Delete(datei);
            }
        }

        foreach (var (schluessel, wert) in _vorherigeUmgebung)
        {
            Environment.SetEnvironmentVariable(schluessel, wert);
        }
    }

    private async Task<string> AnmeldenAsync(string email, string passwort)
    {
        var antwort = await _client.PostAsJsonAsync(
            "/api/authentifizierung/anmelden",
            new { email, passwort });
        Assert.Equal(HttpStatusCode.OK, antwort.StatusCode);
        using var json = JsonDocument.Parse(await antwort.Content.ReadAsStringAsync());
        return json.RootElement.GetProperty("zugriffstoken").GetString()!;
    }

    private static FormUrlEncodedContent FormularMitToken(
        string html,
        IDictionary<string, string> werte)
    {
        var token = Regex.Match(
            html,
            "name=\"__RequestVerificationToken\" type=\"hidden\" value=\"([^\"]+)\"");
        Assert.True(token.Success, "Das Antifälschungstoken fehlt im Formular.");
        werte["__RequestVerificationToken"] = token.Groups[1].Value;
        return new FormUrlEncodedContent(werte);
    }
}
