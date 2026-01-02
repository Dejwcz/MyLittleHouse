namespace MujDomecek.Application.DTOs;

public sealed record UserProfileResponse(
    Guid Id,
    string FirstName,
    string LastName,
    string Email,
    string? Phone,
    string? AvatarUrl,
    string PreferredLanguage,
    string ThemePreference,
    bool HasPassword,
    LinkedAccountsDto LinkedAccounts,
    DateTime CreatedAt
);

public sealed record LinkedAccountsDto(string? Google, string? Apple);

public sealed record UpdateProfileRequest(
    string? FirstName,
    string? LastName,
    string? Phone,
    string? PreferredLanguage,
    string? ThemePreference
);

public sealed record DeleteAccountRequest(
    string Password,
    string Confirmation
);

public sealed record AvatarUploadResponse(string AvatarUrl);
