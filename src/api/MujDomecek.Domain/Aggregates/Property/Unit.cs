using MujDomecek.Domain.Abstractions;
using MujDomecek.Domain.ValueObjects;

namespace MujDomecek.Domain.Aggregates.Property;

public sealed class Unit : SoftDeletableEntity<Guid>
{
    public Guid PropertyId { get; set; }

    public Guid? ParentUnitId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public UnitType UnitType { get; set; }
}
