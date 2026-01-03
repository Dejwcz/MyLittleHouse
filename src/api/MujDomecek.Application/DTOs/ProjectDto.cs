namespace MujDomecek.Application.DTOs;

public sealed record ProjectDto(
    Guid Id,
    string Name,
    string? Description,
    int PropertyCount,
    int MemberCount,
    string MyRole,
    string SyncMode,
    string SyncStatus,
    DateTime CreatedAt,
    DateTime UpdatedAt
);

public sealed record ProjectDetailDto(
    Guid Id,
    string Name,
    string? Description,
    int PropertyCount,
    int MemberCount,
    string MyRole,
    string SyncMode,
    string SyncStatus,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    IReadOnlyList<PropertyDto> Properties,
    IReadOnlyList<MemberDto> Members
);

public sealed record ProjectListResponse(
    IReadOnlyList<ProjectDto> Items,
    int Total
);

public sealed record CreateProjectRequest(
    string Name,
    string? Description
);

public sealed record UpdateProjectRequest(
    string? Name,
    string? Description
);
