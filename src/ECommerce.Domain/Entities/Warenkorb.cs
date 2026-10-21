using ECommerce.Domain.Common;

namespace ECommerce.Domain.Entities;

public sealed class Warenkorb : BaseEntity
{
    public Guid KundeId { get; set; }
    public Kunde Kunde { get; set; } = null!;
    public ICollection<Warenkorbposition> Positionen { get; set; } = new List<Warenkorbposition>();
}
