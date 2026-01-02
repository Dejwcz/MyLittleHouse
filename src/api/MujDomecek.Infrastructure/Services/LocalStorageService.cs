using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using MujDomecek.Application.Abstractions;
using MujDomecek.Infrastructure.Options;

namespace MujDomecek.Infrastructure.Services;

public sealed class LocalStorageService : IStorageService
{
    private readonly StorageOptions _options;
    private readonly IHostEnvironment _environment;

    public LocalStorageService(IOptions<StorageOptions> options, IHostEnvironment environment)
    {
        _options = options.Value;
        _environment = environment;
    }

    public Task<PresignedUrlResult> GetUploadUrlAsync(
        string storageKey,
        string contentType,
        TimeSpan expiresIn,
        CancellationToken ct = default)
    {
        var expiresAt = DateTime.UtcNow.Add(expiresIn);
        var url = BuildUrl(_options.UploadBaseUrl ?? _options.PublicBaseUrl, storageKey)
            ?? BuildLocalPath(storageKey);

        return Task.FromResult(new PresignedUrlResult(url, expiresAt));
    }

    public Task<PresignedUrlResult> GetDownloadUrlAsync(
        string storageKey,
        TimeSpan expiresIn,
        CancellationToken ct = default)
    {
        var expiresAt = DateTime.UtcNow.Add(expiresIn);
        var url = BuildUrl(_options.PublicBaseUrl, storageKey)
            ?? BuildLocalPath(storageKey);

        return Task.FromResult(new PresignedUrlResult(url, expiresAt));
    }

    public string? GetPublicUrl(string storageKey)
    {
        return BuildUrl(_options.PublicBaseUrl, storageKey);
    }

    public string? GetThumbnailUrl(string storageKey)
    {
        return BuildUrl(_options.ImageBaseUrl, storageKey);
    }

    public async Task UploadAsync(
        string storageKey,
        Stream content,
        string contentType,
        CancellationToken ct = default)
    {
        var path = BuildLocalPath(storageKey);
        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrWhiteSpace(directory))
            Directory.CreateDirectory(directory);

        await using var fileStream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None);
        await content.CopyToAsync(fileStream, ct);
    }

    public Task DeleteAsync(string storageKey, CancellationToken ct = default)
    {
        var path = BuildLocalPath(storageKey);
        if (File.Exists(path))
            File.Delete(path);

        return Task.CompletedTask;
    }

    public Task<Stream?> OpenReadAsync(string storageKey, CancellationToken ct = default)
    {
        var path = BuildLocalPath(storageKey);
        if (!File.Exists(path))
            return Task.FromResult<Stream?>(null);

        Stream stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        return Task.FromResult<Stream?>(stream);
    }

    private string BuildLocalPath(string storageKey)
    {
        var root = string.IsNullOrWhiteSpace(_options.LocalRootPath)
            ? Path.Combine(_environment.ContentRootPath, "storage")
            : _options.LocalRootPath;

        var safeKey = NormalizeKey(storageKey);
        var relativePath = safeKey.Replace('/', Path.DirectorySeparatorChar);
        return Path.Combine(root, relativePath);
    }

    private static string? BuildUrl(string? baseUrl, string storageKey)
    {
        if (string.IsNullOrWhiteSpace(baseUrl))
            return null;

        return $"{baseUrl.TrimEnd('/')}/{NormalizeKey(storageKey)}";
    }

    private static string NormalizeKey(string storageKey)
    {
        var normalized = storageKey.Replace('\\', '/').TrimStart('/');
        return normalized.Replace("..", string.Empty, StringComparison.Ordinal);
    }
}
