namespace MujDomecek.Domain.Aggregates.Audit;

public sealed class AuditLog
{
    public Guid Id { get; set; }

    public string EntityType { get; set; } = string.Empty;

    public Guid EntityId { get; set; }

    public string Action { get; set; } = string.Empty;

    public Guid ActorUserId { get; set; }

    public string? DiffSummaryJson { get; set; }

    public DateTime CreatedAt { get; set; }
}
