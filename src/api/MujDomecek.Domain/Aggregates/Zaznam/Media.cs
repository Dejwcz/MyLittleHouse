using MujDomecek.Domain.Abstractions;
using MujDomecek.Domain.ValueObjects;

namespace MujDomecek.Domain.Aggregates.Zaznam;

public sealed class Media : SoftDeletableEntity<Guid>
{
    public OwnerType OwnerType { get; set; }

    public Guid OwnerId { get; set; }

    public MediaType Type { get; set; }

    public string StorageKey { get; set; } = string.Empty;

    public string? OriginalFileName { get; set; }

    public string MimeType { get; set; } = string.Empty;

    public long SizeBytes { get; set; }

    public string? Description { get; set; }
}
