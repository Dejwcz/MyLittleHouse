using MujDomecek.Domain.Abstractions;
using MujDomecek.Domain.Events;
using MujDomecek.Domain.ValueObjects;

namespace MujDomecek.Domain.Tests;

public sealed class ValueObjectTests
{
    private sealed class TestValueObject : ValueObject
    {
        public TestValueObject(string? name, int number)
        {
            Name = name;
            Number = number;
        }

        public string? Name { get; }

        public int Number { get; }

        protected override IEnumerable<object?> GetEqualityComponents()
        {
            yield return Name;
            yield return Number;
        }
    }

    [Fact]
    public void Equals_WithSameComponents_ReturnsTrue()
    {
        var first = new TestValueObject("alpha", 1);
        var second = new TestValueObject("alpha", 1);

        Assert.Equal(first, second);
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
    }

    [Fact]
    public void Equals_WithDifferentComponents_ReturnsFalse()
    {
        var first = new TestValueObject("alpha", 1);
        var second = new TestValueObject("alpha", 2);

        Assert.NotEqual(first, second);
    }
}

public sealed class EntityDomainEventsTests
{
    private sealed class TestEntity : Entity<Guid>
    {
        public void Raise(IDomainEvent domainEvent) => AddDomainEvent(domainEvent);
    }

    private sealed class TestEvent : IDomainEvent
    {
        public TestEvent(DateTime occurredAtUtc) => OccurredAtUtc = occurredAtUtc;

        public DateTime OccurredAtUtc { get; }
    }

    [Fact]
    public void AddDomainEvent_AppendsEvent()
    {
        var entity = new TestEntity();
        var domainEvent = new TestEvent(DateTime.UtcNow);

        entity.Raise(domainEvent);

        var storedEvent = Assert.Single(entity.DomainEvents);
        Assert.Same(domainEvent, storedEvent);
    }

    [Fact]
    public void ClearDomainEvents_RemovesAll()
    {
        var entity = new TestEntity();
        entity.Raise(new TestEvent(DateTime.UtcNow));
        entity.Raise(new TestEvent(DateTime.UtcNow.AddMinutes(1)));

        entity.ClearDomainEvents();

        Assert.Empty(entity.DomainEvents);
    }
}
