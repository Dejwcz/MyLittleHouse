using MujDomecek.Domain.Abstractions;
using MujDomecek.Domain.ValueObjects;

namespace MujDomecek.Domain.Aggregates.Zaznam;

public sealed class ZaznamMember : AuditableEntity<Guid>
{
    public Guid ZaznamId { get; set; }

    public Guid UserId { get; set; }

    public MemberRole Role { get; set; } = MemberRole.Viewer;

    public string? PermissionsJson { get; set; }
}
