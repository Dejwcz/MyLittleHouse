using System.Text.Json;

namespace MujDomecek.Application.DTOs;

public sealed record AdminDashboardResponse(
    AdminDashboardUsers Users,
    AdminDashboardContent Content,
    AdminDashboardStorage Storage,
    AdminDashboardInvitations Invitations
);

public sealed record AdminDashboardUsers(int Total, int Active7d, int Blocked);
public sealed record AdminDashboardContent(int Projects, int Properties, int Zaznamy, int Zaznamy24h);
public sealed record AdminDashboardStorage(long TotalBytes, int FileCount);
public sealed record AdminDashboardInvitations(int Pending);

public sealed record HealthCheckResponse(
    string Status,
    HealthCheckDetails Checks
);

public sealed record HealthCheckDetails(
    HealthCheckItem Database,
    HealthCheckItem Storage,
    HealthCheckItem Email,
    HealthCheckJobs BackgroundJobs
);

public sealed record HealthCheckItem(string Status, int? ResponseTimeMs);
public sealed record HealthCheckJobs(string Status, int Pending);

public sealed record AdminUserDto(
    Guid Id,
    string FirstName,
    string LastName,
    string Email,
    bool EmailVerified,
    string? AvatarUrl,
    string Status,
    int ProjectCount,
    DateTime? LastLoginAt,
    DateTime CreatedAt
);

public sealed record AdminUserListResponse(
    IReadOnlyList<AdminUserDto> Items,
    int Total,
    int Page,
    int PageSize
);

public sealed record BlockUserRequest(string Reason);

public sealed record AdminSessionDto(
    Guid Id,
    string? DeviceInfo,
    DateTime CreatedAt,
    DateTime ExpiresAt,
    DateTime? RevokedAt
);

public sealed record AdminSessionListResponse(
    IReadOnlyList<AdminSessionDto> Items
);

public sealed record TagDto(
    short Id,
    string Name,
    string Icon,
    short SortOrder,
    bool IsActive,
    int UsageCount
);

public sealed record CreateTagRequest(
    string Name,
    string Icon,
    short? SortOrder
);

public sealed record UpdateTagRequest(
    string? Name,
    string? Icon,
    short? SortOrder
);

public sealed record TagOrderItem(short Id, short SortOrder);

public sealed record ReorderTagsRequest(IReadOnlyList<TagOrderItem> Order);

public sealed record AdminSettingUpdatedBy(
    Guid Id,
    string Name
);

public sealed record AdminSettingDto(
    string Key,
    JsonElement Value,
    string Source,
    DateTime? UpdatedAt,
    AdminSettingUpdatedBy? UpdatedBy
);

public sealed record AdminSettingsResponse(
    IReadOnlyList<AdminSettingDto> Items
);

public sealed record AdminSettingUpdateItem(
    string Key,
    JsonElement Value
);

public sealed record UpdateAdminSettingsRequest(
    IReadOnlyList<AdminSettingUpdateItem> Items
);

public sealed record AuditActorDto(
    Guid Id,
    string Name,
    string Email
);

public sealed record AuditDiff(
    JsonElement? Old,
    JsonElement? New
);

public sealed record AuditLogDto(
    Guid Id,
    string EntityType,
    Guid EntityId,
    string Action,
    AuditActorDto Actor,
    Dictionary<string, AuditDiff>? DiffSummary,
    DateTime CreatedAt
);

public sealed record AuditLogListResponse(
    IReadOnlyList<AuditLogDto> Items,
    int Total,
    int Page,
    int PageSize
);
