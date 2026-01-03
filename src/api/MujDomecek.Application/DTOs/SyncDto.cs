using System.Text.Json;

namespace MujDomecek.Application.DTOs;

public sealed record SyncStatusResponse(
    DateTime? LastSyncAt,
    int PendingChanges,
    DateTime ServerTime
);

public sealed record SyncPushRequest(
    string CorrelationId,
    string ScopeType,
    Guid ScopeId,
    IReadOnlyList<SyncChange> Changes
);

public sealed record SyncChange(
    string Id,
    string EntityType,
    Guid EntityId,
    string Operation,
    JsonElement? Data,
    DateTime ClientTimestamp
);

public sealed record SyncRejectedChange(
    string Id,
    string Reason
);

public sealed record SyncConflict(
    string Id,
    JsonElement ServerVersion
);

public sealed record SyncPushResponse(
    string CorrelationId,
    IReadOnlyList<string> Accepted,
    IReadOnlyList<SyncRejectedChange> Rejected,
    IReadOnlyList<SyncConflict> Conflicts,
    DateTime ServerTimestamp
);

public sealed record SyncPullChange(
    string EntityType,
    Guid EntityId,
    string Operation,
    JsonElement? Data,
    DateTime ServerTimestamp
);

public sealed record SyncPullResponse(
    IReadOnlyList<SyncPullChange> Changes,
    DateTime ServerTimestamp,
    bool HasMore
);
