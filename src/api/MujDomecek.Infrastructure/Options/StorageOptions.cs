namespace MujDomecek.Infrastructure.Options;

public sealed class StorageOptions
{
    public const string SectionName = "Storage";

    public string Provider { get; set; } = "S3";

    public string PublicBucket { get; set; } = "mujdomecek-public";

    public string PrivateBucket { get; set; } = "mujdomecek-private";

    public string? PublicBaseUrl { get; set; }

    public string? UploadBaseUrl { get; set; }

    public string? ImageBaseUrl { get; set; }

    public int PresignedExpiryMinutes { get; set; } = 60;

    public string? LocalRootPath { get; set; }

    public S3Options S3 { get; set; } = new();
}

public sealed class S3Options
{
    public string? AccessKey { get; set; }

    public string? SecretKey { get; set; }

    public string? Region { get; set; }

    public string? ServiceUrl { get; set; }

    public bool ForcePathStyle { get; set; } = true;
}
