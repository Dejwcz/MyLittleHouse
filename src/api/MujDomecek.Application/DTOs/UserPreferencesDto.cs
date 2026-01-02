namespace MujDomecek.Application.DTOs;

public sealed record UserPreferencesDto(
    PushPreferencesDto Push,
    EmailPreferencesDto Email,
    SyncPreferencesDto Sync
);

public sealed record PushPreferencesDto(
    bool NewComments,
    bool Mentions,
    bool SharedActivity,
    bool DraftReminders
);

public sealed record EmailPreferencesDto(
    bool WeeklySummary,
    bool Invitations
);

public sealed record SyncPreferencesDto(
    bool Enabled,
    bool OnMobileData
);

public sealed record UpdatePreferencesRequest(
    PushPreferencesUpdateDto? Push,
    EmailPreferencesUpdateDto? Email,
    SyncPreferencesUpdateDto? Sync
);

public sealed record PushPreferencesUpdateDto(
    bool? NewComments,
    bool? Mentions,
    bool? SharedActivity,
    bool? DraftReminders
);

public sealed record EmailPreferencesUpdateDto(
    bool? WeeklySummary,
    bool? Invitations
);

public sealed record SyncPreferencesUpdateDto(
    bool? Enabled,
    bool? OnMobileData
);
