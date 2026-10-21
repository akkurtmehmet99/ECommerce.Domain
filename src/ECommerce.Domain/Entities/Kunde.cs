using ECommerce.Domain.Common;

namespace ECommerce.Domain.Entities;

public sealed class Kunde : BaseEntity
{
    public string Vorname { get; set; } = string.Empty;
    public string Nachname { get; set; } = string.Empty;
    public string EMail { get; set; } = string.Empty;
    public string PasswortHash { get; set; } = string.Empty;
    public string Rolle { get; set; } = "Kunde";
    public bool Aktiv { get; set; } = true;
    public ICollection<Bestellung> Bestellungen { get; set; } = new List<Bestellung>();
    public Warenkorb? Warenkorb { get; set; }
    public ICollection<Sitzungstoken> Sitzungstoken { get; set; } = new List<Sitzungstoken>();
}
