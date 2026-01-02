namespace MujDomecek.Application.DTOs;

public sealed record ContactGroupDto(
    Guid Id,
    string Name,
    int MemberCount,
    IReadOnlyList<ContactDto> Members,
    DateTime CreatedAt
);

public sealed record CreateContactGroupRequest(
    string Name,
    IReadOnlyList<Guid>? ContactIds
);

public sealed record UpdateContactGroupRequest(
    string? Name
);

public sealed record AddGroupMemberRequest(
    Guid ContactId
);
