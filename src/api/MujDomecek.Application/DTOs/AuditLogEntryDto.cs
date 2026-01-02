namespace MujDomecek.Application.DTOs;

public sealed record AuditLogEntryDto(
    Guid Id,
    string EntityType,
    Guid EntityId,
    string Action,
    Guid ActorUserId,
    string? DiffSummaryJson,
    DateTime CreatedAt
);
