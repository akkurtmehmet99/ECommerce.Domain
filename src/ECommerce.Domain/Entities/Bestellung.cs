using ECommerce.Domain.Common;
using ECommerce.Domain.Enums;

namespace ECommerce.Domain.Entities;

public sealed class Bestellung : BaseEntity
{
    public Guid KundeId { get; set; }
    public Kunde Kunde { get; set; } = null!;
    public Bestellstatus Status { get; set; } = Bestellstatus.Ausstehend;
    public decimal Gesamtbetrag { get; set; }
    public DateTimeOffset ReservierungLaeuftAbAm { get; set; }
    public string Liefername { get; set; } = string.Empty;
    public string Lieferstrasse { get; set; } = string.Empty;
    public string LieferPostleitzahl { get; set; } = string.Empty;
    public string LieferStadt { get; set; } = string.Empty;
    public string LieferLand { get; set; } = string.Empty;
    public ICollection<Bestellposition> Positionen { get; set; } = new List<Bestellposition>();
    public Zahlung? Zahlung { get; set; }
}
