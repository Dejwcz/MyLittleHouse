using MujDomecek.Domain.Abstractions;
using MujDomecek.Domain.ValueObjects;

namespace MujDomecek.Domain.Aggregates.Zaznam;

public sealed class ZaznamDokument : SoftDeletableEntity<Guid>
{
    public Guid ZaznamId { get; set; }

    public DocumentType Type { get; set; }

    public string StorageKey { get; set; } = string.Empty;

    public string? OriginalFileName { get; set; }

    public string MimeType { get; set; } = string.Empty;

    public long SizeBytes { get; set; }

    public string? Description { get; set; }
}
