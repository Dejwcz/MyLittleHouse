namespace MujDomecek.Domain.Events;

public interface IDomainEvent
{
    DateTime OccurredAtUtc { get; }
}
