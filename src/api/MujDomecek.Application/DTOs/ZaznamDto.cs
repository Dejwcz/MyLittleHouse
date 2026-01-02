namespace MujDomecek.Application.DTOs;

public sealed record ZaznamDto(
    Guid Id,
    Guid PropertyId,
    string PropertyName,
    Guid? UnitId,
    string? UnitName,
    string? Title,
    string? Description,
    DateOnly Date,
    int? Cost,
    string Status,
    IReadOnlyList<string> Flags,
    IReadOnlyList<string> Tags,
    int DocumentCount,
    int CommentCount,
    string? ThumbnailUrl,
    string SyncStatus,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    SimpleUserDto CreatedBy
);

public sealed record ZaznamDetailDto(
    Guid Id,
    Guid PropertyId,
    string PropertyName,
    Guid? UnitId,
    string? UnitName,
    string? Title,
    string? Description,
    DateOnly Date,
    int? Cost,
    string Status,
    IReadOnlyList<string> Flags,
    IReadOnlyList<string> Tags,
    int DocumentCount,
    int CommentCount,
    string? ThumbnailUrl,
    string SyncStatus,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    SimpleUserDto CreatedBy,
    IReadOnlyList<DocumentDto> Documents,
    IReadOnlyList<CommentDto> Comments,
    IReadOnlyList<AuditLogEntryDto> AuditLog
);

public sealed record ZaznamListResponse(
    IReadOnlyList<ZaznamDto> Items,
    int Total,
    int Page,
    int PageSize
);

public sealed record CreateZaznamRequest(
    Guid PropertyId,
    Guid? UnitId,
    string? Title,
    string? Description,
    DateOnly? Date,
    int? Cost,
    string? Status,
    IReadOnlyList<string>? Flags,
    IReadOnlyList<string>? Tags
);

public sealed record UpdateZaznamRequest(
    Guid? UnitId,
    string? Title,
    string? Description,
    DateOnly? Date,
    int? Cost,
    IReadOnlyList<string>? Flags,
    IReadOnlyList<string>? Tags
);

public sealed record SimpleUserDto(Guid Id, string Name);
