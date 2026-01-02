using MujDomecek.Domain.Events;

namespace MujDomecek.Domain.Abstractions;

public abstract class Entity<TId>
{
    private readonly List<IDomainEvent> _domainEvents = [];

    protected Entity() { }

    protected Entity(TId id)
    {
        Id = id;
    }

    public TId Id { get; set; } = default!;

    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    protected void AddDomainEvent(IDomainEvent domainEvent) => _domainEvents.Add(domainEvent);

    public void ClearDomainEvents() => _domainEvents.Clear();
}
