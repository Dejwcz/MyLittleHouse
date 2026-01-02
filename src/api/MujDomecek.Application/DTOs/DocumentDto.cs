namespace MujDomecek.Application.DTOs;

public sealed record DocumentDto(
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

public sealed record AddDocumentRequest(
    string StorageKey,
    string Type,
    string? OriginalFileName,
    string MimeType,
    long SizeBytes,
    string? Description
);

public sealed record UpdateDocumentRequest(
    string? Description
);

public sealed record DocumentUrlResponse(
    string Url,
    DateTime ExpiresAt
);
