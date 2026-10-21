using System.ComponentModel.DataAnnotations;
using ECommerce.Domain.Common;

namespace ECommerce.Domain.Entities;

public class Produkt : BaseEntity
{
    [Required]
    [StringLength(100, MinimumLength = 2)]
    public string Name { get; set; } = string.Empty;

    [StringLength(500)]
    public string Beschreibung { get; set; } = string.Empty;

    [Range(typeof(decimal), "0.01", "9999999999999999.99", ParseLimitsInInvariantCulture = true)]
    public decimal Preis { get; set; }

    [Range(0, int.MaxValue)]
    public int Lagerbestand { get; set; }

    public bool Aktiv { get; set; } = true;

    public Guid KategorieId { get; set; }
    public Kategorie Kategorie { get; set; } = null!;

    public ICollection<Bestellposition> Bestellpositionen { get; set; } = new List<Bestellposition>();
}
