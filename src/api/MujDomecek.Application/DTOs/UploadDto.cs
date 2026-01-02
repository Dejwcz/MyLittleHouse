namespace MujDomecek.Application.DTOs;

public sealed record UploadRequestRequest(
    string FileName,
    string MimeType,
    long SizeBytes
);

public sealed record UploadRequestResponse(
    string StorageKey,
    string UploadUrl,
    DateTime ExpiresAt
);

public sealed record UploadConfirmRequest(
    string StorageKey
);

public sealed record UploadConfirmResponse(
    string StorageKey,
    string Url,
    string? ThumbnailUrl
);
