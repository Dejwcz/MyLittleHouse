using MujDomecek.Domain.ValueObjects;

namespace MujDomecek.Domain.Aggregates.Project;

public sealed class ProjectMember
{
    public Guid Id { get; set; }

    public Guid ProjectId { get; set; }

    public Guid UserId { get; set; }

    public MemberRole Role { get; set; }

    public string? PermissionsJson { get; set; }

    public DateTime CreatedAt { get; set; }
}
