namespace MujDomecek.Application.DTOs;

public sealed record ShareMemberDto(
    Guid UserId,
    string Email,
    string DisplayName,
    string Role,
    string Status,
    DateTime? JoinedAt,
    DateTime? InvitedAt,
    bool HasOverrides
);

public sealed record ShareItemDto(
    string Type,
    Guid Id,
    string Name,
    string? ProjectName,
    IReadOnlyList<ShareMemberDto> Members
);

public sealed record MySharesResponse(
    IReadOnlyList<ShareItemDto> Items
);

public sealed record SharedOwnerDto(
    Guid UserId,
    string Email,
    string DisplayName
);

public sealed record SharedWithMeItemDto(
    string Type,
    Guid Id,
    string Name,
    SharedOwnerDto Owner,
    string MyRole,
    DateTime SharedAt
);

public sealed record SharedWithMeResponse(
    IReadOnlyList<SharedWithMeItemDto> Items
);

public sealed record PendingInvitationDto(
    Guid Id,
    string Type,
    Guid TargetId,
    string TargetName,
    string Role,
    InvitationActorDto InvitedBy,
    DateTime CreatedAt,
    DateTime ExpiresAt
);

public sealed record PendingInvitationsResponse(
    IReadOnlyList<PendingInvitationDto> Invitations
);

public sealed record InvitationLinkResponse(
    Guid InvitationId,
    string InviteUrl,
    DateTime ExpiresAt
);
