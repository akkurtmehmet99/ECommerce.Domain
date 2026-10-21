using ECommerce.Domain.Common;

namespace ECommerce.Domain.Entities;

public sealed class Sitzungstoken : BaseEntity
{
    public Guid KundeId { get; set; }
    public Kunde Kunde { get; set; } = null!;
    public string Hash { get; set; } = string.Empty;
    public DateTimeOffset Ablaufzeitpunkt { get; set; }
    public DateTimeOffset? WiderrufenAm { get; set; }
    public string? ErsetztDurchHash { get; set; }
}
