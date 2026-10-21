using ECommerce.Domain.Common;

namespace ECommerce.Domain.Entities;

public sealed class Kategorie : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Beschreibung { get; set; } = string.Empty;
    public bool Aktiv { get; set; } = true;
    public ICollection<Produkt> Produkte { get; set; } = new List<Produkt>();
}
