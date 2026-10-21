using ECommerce.Domain.Common;

namespace ECommerce.Domain.Entities;

public sealed class Warenkorbposition : BaseEntity
{
    public Guid WarenkorbId { get; set; }
    public Warenkorb Warenkorb { get; set; } = null!;
    public Guid ProduktId { get; set; }
    public Produkt Produkt { get; set; } = null!;
    public int Menge { get; set; }
}
