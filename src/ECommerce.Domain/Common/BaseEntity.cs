namespace ECommerce.Domain.Common;

public abstract class BaseEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTimeOffset ErstelltAm { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? GeaendertAm { get; set; }
    public bool Geloescht { get; set; }
}
