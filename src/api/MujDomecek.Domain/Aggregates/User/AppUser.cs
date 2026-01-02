using Microsoft.AspNetCore.Identity;

namespace MujDomecek.Domain.Aggregates.User;

public sealed class AppUser : IdentityUser<Guid>
{
    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    public string? AvatarStorageKey { get; set; }

    public string? GoogleId { get; set; }

    public string? AppleId { get; set; }

    public string PreferredLanguage { get; set; } = "cs";

    public ThemePreference ThemePreference { get; set; } = ThemePreference.System;

    public DateTime CreatedAt { get; set; }

    public DateTime? LastLoginAt { get; set; }

    public bool IsBlocked { get; set; }

    public string? BlockedReason { get; set; }

    public bool IsDeleted { get; set; }

    public DateTime? DeletedAt { get; set; }

    public UserPreferences? Preferences { get; set; }
}
