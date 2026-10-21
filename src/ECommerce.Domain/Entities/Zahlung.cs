using ECommerce.Domain.Common;
using ECommerce.Domain.Enums;

namespace ECommerce.Domain.Entities;

public sealed class Zahlung : BaseEntity
{
    public Guid BestellungId { get; set; }
    public Bestellung Bestellung { get; set; } = null!;
    public Zahlungsstatus Status { get; set; } = Zahlungsstatus.Ausstehend;
    public Zahlungsart Art { get; set; } = Zahlungsart.Sandbox;
    public string Transaktionsreferenz { get; set; } = string.Empty;
    public DateTimeOffset? BezahltAm { get; set; }
}
