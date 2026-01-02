using MujDomecek.Domain.Abstractions;
using MujDomecek.Domain.ValueObjects;

namespace MujDomecek.Domain.Aggregates.Zaznam;

public sealed class Zaznam : SoftDeletableEntity<Guid>
{
    public Guid PropertyId { get; set; }

    public Guid? UnitId { get; set; }

    public string? Title { get; set; }

    public string? Description { get; set; }

    public DateOnly Date { get; set; } = DateOnly.FromDateTime(DateTime.UtcNow);

    public int? Cost { get; set; }

    public ZaznamStatus Status { get; set; } = ZaznamStatus.Draft;

    public ZaznamFlags Flags { get; set; } = ZaznamFlags.None;

    public List<ZaznamDokument> Documents { get; set; } = [];

    public List<ZaznamTag> Tags { get; set; } = [];

    public List<Comment> Comments { get; set; } = [];
}
