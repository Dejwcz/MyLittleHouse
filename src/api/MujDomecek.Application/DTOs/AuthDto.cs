namespace MujDomecek.Application.DTOs;

public sealed record RegisterRequest(
    string FirstName,
    string LastName,
    string Email,
    string Password
);

public sealed record RegisterResponse(
    bool Success,
    string Message,
    Guid? UserId
);

public sealed record LoginRequest(
    string Email,
    string Password
);

public sealed record LoginResponse(
    string AccessToken,
    int ExpiresIn,
    UserProfileResponse User
);

public sealed record ForgotPasswordRequest(string Email);

public sealed record ResendConfirmationRequest(string Email);

public sealed record ConfirmEmailResponse(bool Success, string Message);

public sealed record ValidateResetTokenResponse(bool Valid);

public sealed record ResetPasswordRequest(string Token, string NewPassword);

public sealed record ChangePasswordRequest(string CurrentPassword, string NewPassword);

public sealed record SessionDto(
    Guid Id,
    string? DeviceInfo,
    DateTime CreatedAt,
    DateTime LastUsedAt,
    bool IsCurrent
);

public sealed record SessionsResponse(IReadOnlyList<SessionDto> Sessions);
