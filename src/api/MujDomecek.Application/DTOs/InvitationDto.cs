namespace MujDomecek.Application.DTOs;

public sealed record InvitationDto(
    Guid Id,
    string Type,
    Guid TargetId,
    string TargetName,
    string Email,
    string Role,
    IDictionary<string, bool>? Permissions,
    string Status,
    InvitationActorDto InvitedBy,
    DateTime CreatedAt,
    DateTime ExpiresAt
);

public sealed record InvitationActorDto(
    Guid UserId,
    string Email,
    string DisplayName
);
