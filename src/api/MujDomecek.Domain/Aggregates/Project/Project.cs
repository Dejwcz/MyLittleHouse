using MujDomecek.Domain.Abstractions;
using MujDomecek.Domain.ValueObjects;

namespace MujDomecek.Domain.Aggregates.Project;

public sealed class Project : SoftDeletableEntity<Guid>
{
    public Guid OwnerId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public SyncMode SyncMode { get; set; } = SyncMode.Synced;

    public SyncStatus SyncStatus { get; set; } = SyncStatus.Synced;

    public DateTime? LastSyncAt { get; set; }

    public List<ProjectMember> Members { get; set; } = [];
}
