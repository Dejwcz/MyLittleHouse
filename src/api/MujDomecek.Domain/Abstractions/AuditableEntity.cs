namespace MujDomecek.Domain.Abstractions;

public abstract class AuditableEntity<TId> : Entity<TId>
{
    protected AuditableEntity() { }

    protected AuditableEntity(TId id) : base(id) { }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public long ServerRevision { get; set; }
}
