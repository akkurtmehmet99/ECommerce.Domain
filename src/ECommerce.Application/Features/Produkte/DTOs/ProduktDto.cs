namespace ECommerce.Application.Features.Produkte.DTOs;

/// <summary>Ein Produkt in der öffentlichen Produktübersicht.</summary>
public sealed record ProduktDto(
    Guid Id,
    string Name,
    string Beschreibung,
    decimal Preis,
    int Lagerbestand,
    Guid KategorieId,
    string KategorieName);

/// <summary>Seitenergebnis einer Ressourcensammlung.</summary>
