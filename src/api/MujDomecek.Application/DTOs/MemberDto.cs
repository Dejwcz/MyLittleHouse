namespace MujDomecek.Application.DTOs;

public sealed record MemberDto(
    Guid UserId,
    string Email,
    string DisplayName,
    string? AvatarUrl,
    string Role,
    IDictionary<string, bool>? Permissions,
    string Status,
    DateTime? JoinedAt
);

public sealed record AddMemberRequest(
    string Email,
    string Role,
    IDictionary<string, bool>? Permissions
);

public sealed record UpdateMemberRequest(
    string? Role,
    IDictionary<string, bool>? Permissions
);
