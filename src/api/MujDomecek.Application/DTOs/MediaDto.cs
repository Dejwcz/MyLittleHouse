namespace MujDomecek.Application.DTOs;

public sealed record MediaDto(
    Guid Id,
    Guid ZaznamId,
    string Type,
    string StorageKey,
    string? OriginalFileName,
    string MimeType,
    long SizeBytes,
    string? Description,
    string? ThumbnailUrl,
    DateTime CreatedAt
);

public sealed record AddMediaRequest(
    string StorageKey,
    string Type,
    string? OriginalFileName,
    string MimeType,
    long SizeBytes,
    string? Description
);

public sealed record UpdateMediaRequest(
    string? Description
);

public sealed record MediaUrlResponse(
    string Url,
    DateTime ExpiresAt
);
