using ECommerce.Domain.Common;

namespace ECommerce.Domain.Entities;

public sealed class Bestellposition : BaseEntity
{
    public Guid BestellungId { get; set; }
    public Bestellung Bestellung { get; set; } = null!;
    public Guid ProduktId { get; set; }
    public Produkt Produkt { get; set; } = null!;
    public string Produktname { get; set; } = string.Empty;
    public int Menge { get; set; }
    public decimal Einzelpreis { get; set; }
    public decimal Gesamtbetrag => Menge * Einzelpreis;
}
