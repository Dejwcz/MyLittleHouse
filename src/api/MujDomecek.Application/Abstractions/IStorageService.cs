namespace MujDomecek.Application.Abstractions;

public interface IStorageService
{
    Task<PresignedUrlResult> GetUploadUrlAsync(
        string storageKey,
        string contentType,
        TimeSpan expiresIn,
        CancellationToken ct = default);

    Task<PresignedUrlResult> GetDownloadUrlAsync(
        string storageKey,
        TimeSpan expiresIn,
        CancellationToken ct = default);

    string? GetPublicUrl(string storageKey);

    string? GetThumbnailUrl(string storageKey);

    Task UploadAsync(
        string storageKey,
        Stream content,
        string contentType,
        CancellationToken ct = default);

    Task DeleteAsync(string storageKey, CancellationToken ct = default);

    Task<Stream?> OpenReadAsync(string storageKey, CancellationToken ct = default);
}

public sealed record PresignedUrlResult(string Url, DateTime ExpiresAt);
