using MujDomecek.Domain.ValueObjects;

namespace MujDomecek.Domain.Aggregates.Project;

public sealed class Invitation
{
    public Guid Id { get; set; }

    public string TokenHash { get; set; } = string.Empty;

    public InvitationTargetType TargetType { get; set; }

    public Guid TargetId { get; set; }

    public string Email { get; set; } = string.Empty;

    public MemberRole Role { get; set; }

    public string? PermissionsJson { get; set; }

    public InvitationStatus Status { get; set; }

    public DateTime ExpiresAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public Guid CreatedBy { get; set; }
}
