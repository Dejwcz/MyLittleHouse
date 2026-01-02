using MujDomecek.Domain.ValueObjects;

namespace MujDomecek.Domain.Aggregates.Property;

public sealed class Activity
{
    public Guid Id { get; set; }

    public Guid PropertyId { get; set; }

    public Guid ActorUserId { get; set; }

    public ActivityType Type { get; set; }

    public string? TargetType { get; set; }

    public Guid? TargetId { get; set; }

    public string? MetadataJson { get; set; }

    public DateTime CreatedAt { get; set; }
}
