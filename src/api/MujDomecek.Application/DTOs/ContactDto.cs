namespace MujDomecek.Application.DTOs;

public sealed record ContactDto(
    Guid Id,
    string Email,
    string? DisplayName,
    bool IsRegistered,
    Guid? UserId,
    DateTime CreatedAt
);

public sealed record CreateContactRequest(
    string Email,
    string? DisplayName
);

public sealed record UpdateContactRequest(
    string? DisplayName
);
