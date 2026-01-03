using MujDomecek.Domain.Abstractions;
using MujDomecek.Domain.ValueObjects;

namespace MujDomecek.Domain.Aggregates.Property;

public sealed class Property : SoftDeletableEntity<Guid>
{
    public Guid ProjectId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public decimal? Latitude { get; set; }

    public decimal? Longitude { get; set; }

    public int GeoRadius { get; set; } = 100;

    public SyncMode SyncMode { get; set; } = SyncMode.Synced;

    public SyncStatus SyncStatus { get; set; } = SyncStatus.Synced;

    public DateTime? LastSyncAt { get; set; }

    public Guid? CoverMediaId { get; set; }

    public List<Unit> Units { get; set; } = [];

    public List<PropertyMember> Members { get; set; } = [];

    public List<Activity> Activities { get; set; } = [];
}
