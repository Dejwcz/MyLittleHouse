namespace MujDomecek.Application.DTOs;

public sealed record MediaDto(
    Guid Id,
    string OwnerType,
    Guid OwnerId,
    string MediaType,
    string StorageKey,
    string? OriginalFileName,
    string MimeType,
    long SizeBytes,
    string? Caption,
    string? ThumbnailUrl,
    DateTime CreatedAt
);

public sealed record MediaListResponse(
    IReadOnlyList<MediaDto> Items
);

public sealed record AddMediaRequest(
    string OwnerType,
    Guid OwnerId,
    string StorageKey,
    string MediaType,
    string? OriginalFileName,
    string MimeType,
    long SizeBytes,
    string? Caption
);

public sealed record UpdateMediaRequest(
    string? Caption
);

public sealed record MediaUrlResponse(
    string Url,
    DateTime ExpiresAt
);
