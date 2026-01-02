using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Options;
using MujDomecek.Application.Abstractions;
using MujDomecek.Infrastructure.Options;

namespace MujDomecek.Infrastructure.Services;

public sealed class S3StorageService : IStorageService
{
    private readonly IAmazonS3 _s3;
    private readonly StorageOptions _options;

    public S3StorageService(IAmazonS3 s3, IOptions<StorageOptions> options)
    {
        _s3 = s3;
        _options = options.Value;
    }

    public Task<PresignedUrlResult> GetUploadUrlAsync(
        string storageKey,
        string contentType,
        TimeSpan expiresIn,
        CancellationToken ct = default)
    {
        var key = NormalizeKey(storageKey);
        var bucket = ResolveBucket(key);
        var expiresAt = DateTime.UtcNow.Add(expiresIn);

        var request = new GetPreSignedUrlRequest
        {
            BucketName = bucket,
            Key = key,
            Verb = HttpVerb.PUT,
            Expires = expiresAt,
            ContentType = string.IsNullOrWhiteSpace(contentType) ? "application/octet-stream" : contentType
        };

        var url = _s3.GetPreSignedURL(request);
        return Task.FromResult(new PresignedUrlResult(url, expiresAt));
    }

    public Task<PresignedUrlResult> GetDownloadUrlAsync(
        string storageKey,
        TimeSpan expiresIn,
        CancellationToken ct = default)
    {
        var key = NormalizeKey(storageKey);
        var bucket = ResolveBucket(key);
        var expiresAt = DateTime.UtcNow.Add(expiresIn);

        var request = new GetPreSignedUrlRequest
        {
            BucketName = bucket,
            Key = key,
            Verb = HttpVerb.GET,
            Expires = expiresAt
        };

        var url = _s3.GetPreSignedURL(request);
        return Task.FromResult(new PresignedUrlResult(url, expiresAt));
    }

    public string? GetPublicUrl(string storageKey)
    {
        if (string.IsNullOrWhiteSpace(_options.PublicBaseUrl))
            return BuildServiceUrl(_options.PublicBucket, storageKey);

        return $"{_options.PublicBaseUrl.TrimEnd('/')}/{NormalizeKey(storageKey)}";
    }

    public string? GetThumbnailUrl(string storageKey)
    {
        if (string.IsNullOrWhiteSpace(_options.ImageBaseUrl))
            return null;

        return $"{_options.ImageBaseUrl.TrimEnd('/')}/{NormalizeKey(storageKey)}";
    }

    public async Task UploadAsync(
        string storageKey,
        Stream content,
        string contentType,
        CancellationToken ct = default)
    {
        var key = NormalizeKey(storageKey);
        var bucket = ResolveBucket(key);

        var request = new PutObjectRequest
        {
            BucketName = bucket,
            Key = key,
            InputStream = content,
            ContentType = string.IsNullOrWhiteSpace(contentType) ? "application/octet-stream" : contentType
        };

        if (IsPublicKey(key))
            request.CannedACL = S3CannedACL.PublicRead;

        await _s3.PutObjectAsync(request, ct);
    }

    public async Task DeleteAsync(string storageKey, CancellationToken ct = default)
    {
        var key = NormalizeKey(storageKey);
        var bucket = ResolveBucket(key);
        await _s3.DeleteObjectAsync(bucket, key, ct);
    }

    public async Task<Stream?> OpenReadAsync(string storageKey, CancellationToken ct = default)
    {
        var key = NormalizeKey(storageKey);
        var bucket = ResolveBucket(key);

        try
        {
            var response = await _s3.GetObjectAsync(bucket, key, ct);
            return new S3ObjectStream(response);
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    private string ResolveBucket(string key)
    {
        return IsPublicKey(key) ? _options.PublicBucket : _options.PrivateBucket;
    }

    private static bool IsPublicKey(string key)
    {
        return key.StartsWith("avatars/", StringComparison.OrdinalIgnoreCase);
    }

    private string? BuildServiceUrl(string bucket, string storageKey)
    {
        var serviceUrl = _options.S3.ServiceUrl;
        if (string.IsNullOrWhiteSpace(serviceUrl))
            return null;

        var normalizedKey = NormalizeKey(storageKey);
        if (_options.S3.ForcePathStyle)
            return $"{serviceUrl.TrimEnd('/')}/{bucket}/{normalizedKey}";

        var uri = new Uri(serviceUrl, UriKind.Absolute);
        return $"{uri.Scheme}://{bucket}.{uri.Host.TrimEnd('/')}/{normalizedKey}";
    }

    private static string NormalizeKey(string storageKey)
    {
        var normalized = storageKey.Replace('\\', '/').TrimStart('/');
        return normalized.Replace("..", string.Empty, StringComparison.Ordinal);
    }

    private sealed class S3ObjectStream : Stream
    {
        private readonly GetObjectResponse _response;
        private readonly Stream _inner;

        public S3ObjectStream(GetObjectResponse response)
        {
            _response = response;
            _inner = response.ResponseStream;
        }

        public override bool CanRead => _inner.CanRead;
        public override bool CanSeek => _inner.CanSeek;
        public override bool CanWrite => _inner.CanWrite;
        public override long Length => _inner.Length;
        public override long Position
        {
            get => _inner.Position;
            set => _inner.Position = value;
        }

        public override void Flush() => _inner.Flush();
        public override int Read(byte[] buffer, int offset, int count) => _inner.Read(buffer, offset, count);
        public override long Seek(long offset, SeekOrigin origin) => _inner.Seek(offset, origin);
        public override void SetLength(long value) => _inner.SetLength(value);
        public override void Write(byte[] buffer, int offset, int count) => _inner.Write(buffer, offset, count);

        public override async ValueTask DisposeAsync()
        {
            await _inner.DisposeAsync();
            _response.Dispose();
            await base.DisposeAsync();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _inner.Dispose();
                _response.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
