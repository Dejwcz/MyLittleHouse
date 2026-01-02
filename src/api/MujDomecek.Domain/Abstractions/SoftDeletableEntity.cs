namespace MujDomecek.Domain.Abstractions;

public abstract class SoftDeletableEntity<TId> : AuditableEntity<TId>
{
    protected SoftDeletableEntity() { }

    protected SoftDeletableEntity(TId id) : base(id) { }

    public bool IsDeleted { get; set; }

    public DateTime? DeletedAt { get; set; }
}
