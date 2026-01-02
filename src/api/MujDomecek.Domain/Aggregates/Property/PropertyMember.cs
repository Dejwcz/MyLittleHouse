using MujDomecek.Domain.ValueObjects;

namespace MujDomecek.Domain.Aggregates.Property;

public sealed class PropertyMember
{
    public Guid Id { get; set; }

    public Guid PropertyId { get; set; }

    public Guid UserId { get; set; }

    public MemberRole Role { get; set; }

    public string? PermissionsJson { get; set; }

    public DateTime CreatedAt { get; set; }
}
