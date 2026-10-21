namespace ECommerce.Application.Common.DTOs;

/// <summary>Seitenergebnis einer Ressourcensammlung.</summary>
public sealed record Seitenergebnis<T>(
    IReadOnlyList<T> Eintraege,
    int Seite,
    int Seitengroesse,
    int Gesamtanzahl,
    int Gesamtseiten);
