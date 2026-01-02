namespace MujDomecek.Application.DTOs;

public sealed record ActivityDto(
    Guid Id,
    string Type,
    ActivityActorDto Actor,
    string? TargetType,
    Guid? TargetId,
    IReadOnlyDictionary<string, object>? Metadata,
    DateTime CreatedAt
);

public sealed record ActivityActorDto(
    Guid Id,
    string Name,
    string? AvatarUrl
);

public sealed record ActivityListResponse(
    IReadOnlyList<ActivityDto> Items,
    int Total
);
