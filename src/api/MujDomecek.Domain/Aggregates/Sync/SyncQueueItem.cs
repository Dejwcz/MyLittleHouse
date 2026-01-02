using MujDomecek.Domain.Abstractions;
using MujDomecek.Domain.ValueObjects;

namespace MujDomecek.Domain.Aggregates.Sync;

public sealed class SyncQueueItem : AuditableEntity<Guid>
{
    public Guid UserId { get; set; }

    public string EntityType { get; set; } = string.Empty;

    public Guid EntityId { get; set; }

    public string Operation { get; set; } = string.Empty;

    public string? PayloadJson { get; set; }

    public SyncQueueStatus Status { get; set; } = SyncQueueStatus.Pending;

    public int RetryCount { get; set; }

    public DateTime? NextRetryAt { get; set; }

    public DateTime? LastAttemptAt { get; set; }

    public string? LastError { get; set; }
}
