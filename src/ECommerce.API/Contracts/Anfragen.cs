using System.ComponentModel.DataAnnotations;

namespace ECommerce.API.Contracts;

/// <summary>Anfrage zur Registrierung eines Kundenkontos.</summary>
public sealed record RegistrierenAnfrage
{
    /// <summary>Vorname des Kunden.</summary>
    [Required(ErrorMessage = "Der Vorname ist erforderlich."),
     StringLength(100, MinimumLength = 1, ErrorMessage = "Der Vorname muss zwischen 1 und 100 Zeichen lang sein.")]
    public required string Vorname { get; init; }

    /// <summary>Nachname des Kunden.</summary>
    [Required(ErrorMessage = "Der Nachname ist erforderlich."),
     StringLength(100, MinimumLength = 1, ErrorMessage = "Der Nachname muss zwischen 1 und 100 Zeichen lang sein.")]
    public required string Nachname { get; init; }

    /// <summary>E-Mail-Adresse des Kunden.</summary>
    [Required(ErrorMessage = "Die E-Mail-Adresse ist erforderlich."),
     EmailAddress(ErrorMessage = "Die E-Mail-Adresse ist ungültig."),
     StringLength(254, ErrorMessage = "Die E-Mail-Adresse darf maximal 254 Zeichen lang sein.")]
    public required string EMail { get; init; }

    /// <summary>Passwort mit mindestens zwölf Zeichen.</summary>
    [Required(ErrorMessage = "Das Passwort ist erforderlich."),
     StringLength(256, MinimumLength = 12, ErrorMessage = "Das Passwort muss zwischen 12 und 256 Zeichen lang sein.")]
    public required string Passwort { get; init; }
}

/// <summary>Anfrage zur Anmeldung eines Kunden.</summary>
public sealed record AnmeldenAnfrage
{
    /// <summary>E-Mail-Adresse des Kunden.</summary>
    [Required(ErrorMessage = "Die E-Mail-Adresse ist erforderlich."),
     EmailAddress(ErrorMessage = "Die E-Mail-Adresse ist ungültig."),
     StringLength(254, ErrorMessage = "Die E-Mail-Adresse darf maximal 254 Zeichen lang sein.")]
    public required string EMail { get; init; }

    /// <summary>Passwort des Kunden.</summary>
    [Required(ErrorMessage = "Das Passwort ist erforderlich."),
     StringLength(256, MinimumLength = 1, ErrorMessage = "Das Passwort darf maximal 256 Zeichen lang sein.")]
    public required string Passwort { get; init; }
}

/// <summary>Anfrage zur Verlängerung oder Beendigung einer Sitzung.</summary>
public sealed record AktualisierungstokenAnfrage
{
    /// <summary>Geheimer, nur gehasht gespeicherter Aktualisierungstoken.</summary>
    [Required(ErrorMessage = "Das Aktualisierungstoken ist erforderlich."),
     StringLength(128, MinimumLength = 40, ErrorMessage = "Das Aktualisierungstoken hat ein ungültiges Format.")]
    public required string Aktualisierungstoken { get; init; }
}

/// <summary>Anfrage zum Anlegen einer Produktkategorie.</summary>
public sealed record KategorieAnfrage
{
    /// <summary>Name der Kategorie.</summary>
    [Required(ErrorMessage = "Der Kategoriename ist erforderlich."),
     StringLength(100, MinimumLength = 1, ErrorMessage = "Der Kategoriename muss zwischen 1 und 100 Zeichen lang sein.")]
    public required string Name { get; init; }

    /// <summary>Beschreibung der Kategorie.</summary>
    [StringLength(500, ErrorMessage = "Die Beschreibung darf maximal 500 Zeichen lang sein.")]
    public string Beschreibung { get; init; } = string.Empty;
}

/// <summary>Anfrage zum Anlegen oder Ändern eines Produkts.</summary>
public sealed record ProduktAnfrage
{
    /// <summary>Name des Produkts.</summary>
    [Required(ErrorMessage = "Der Produktname ist erforderlich."),
     StringLength(100, MinimumLength = 2, ErrorMessage = "Der Produktname muss zwischen 2 und 100 Zeichen lang sein.")]
    public required string Name { get; init; }

    /// <summary>Beschreibung des Produkts.</summary>
    [StringLength(500, ErrorMessage = "Die Beschreibung darf maximal 500 Zeichen lang sein.")]
    public string Beschreibung { get; init; } = string.Empty;

    /// <summary>Verkaufspreis in der Systemwährung EUR.</summary>
    [Range(
        typeof(decimal),
        "0.01",
        "9999999999999999.99",
        ErrorMessage = "Der Preis muss größer als 0 sein und darf maximal zwei Nachkommastellen enthalten.",
        ParseLimitsInInvariantCulture = true)]
    public required decimal Preis { get; init; }

    /// <summary>Verfügbare Stückzahl.</summary>
    [Range(0, int.MaxValue, ErrorMessage = "Der Lagerbestand darf nicht negativ sein.")]
    public required int Lagerbestand { get; init; }

    /// <summary>ID der Produktkategorie.</summary>
    [Required(ErrorMessage = "Eine Kategorie muss ausgewählt werden.")]
    public required Guid KategorieId { get; init; }
}

/// <summary>Anfrage zum Hinzufügen oder Ändern der Produktmenge im Warenkorb.</summary>
public sealed record WarenkorbMengeAnfrage
{
    /// <summary>Gewünschte Produktmenge.</summary>
    [Range(1, 100, ErrorMessage = "Die Menge muss zwischen 1 und 100 liegen.")]
    public required int Menge { get; init; }
}

/// <summary>Anfrage zur Bestellung mit Lieferadresse.</summary>
public sealed record BestellungAnfrage
{
    /// <summary>Vollständiger Name des Empfängers.</summary>
    [Required(ErrorMessage = "Der Liefername ist erforderlich."),
     StringLength(200, MinimumLength = 1, ErrorMessage = "Der Liefername muss zwischen 1 und 200 Zeichen lang sein.")]
    public required string Liefername { get; init; }

    /// <summary>Straße und Hausnummer.</summary>
    [Required(ErrorMessage = "Die Lieferstraße ist erforderlich."),
     StringLength(200, MinimumLength = 1, ErrorMessage = "Die Lieferstraße muss zwischen 1 und 200 Zeichen lang sein.")]
    public required string Lieferstrasse { get; init; }

    /// <summary>Postleitzahl.</summary>
    [Required(ErrorMessage = "Die Postleitzahl ist erforderlich."),
     StringLength(20, MinimumLength = 1, ErrorMessage = "Die Postleitzahl muss zwischen 1 und 20 Zeichen lang sein.")]
    public required string LieferPostleitzahl { get; init; }

    /// <summary>Ort der Lieferung.</summary>
    [Required(ErrorMessage = "Die Lieferstadt ist erforderlich."),
     StringLength(100, MinimumLength = 1, ErrorMessage = "Die Lieferstadt muss zwischen 1 und 100 Zeichen lang sein.")]
    public required string LieferStadt { get; init; }

    /// <summary>Zweistelliger ISO-Ländercode.</summary>
    [Required(ErrorMessage = "Der ISO-Ländercode ist erforderlich."),
     StringLength(2, MinimumLength = 2, ErrorMessage = "Der ISO-Ländercode muss genau zwei Zeichen lang sein.")]
    public required string LieferLand { get; init; }
}

/// <summary>Ergebnis einer simulierten Sandbox-Zahlung.</summary>
public sealed record SandboxZahlungAnfrage
{
    /// <summary>Gibt an, ob die simulierte Zahlung erfolgreich sein soll.</summary>
    public required bool Erfolgreich { get; init; }
}
