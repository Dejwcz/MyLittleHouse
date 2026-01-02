namespace MujDomecek.Domain.Aggregates.User;

public sealed class UserPreferences
{
    public Guid UserId { get; set; }

    public bool PushNewComments { get; set; } = true;

    public bool PushMentions { get; set; } = true;

    public bool PushSharedActivity { get; set; } = true;

    public bool PushDraftReminders { get; set; } = true;

    public bool EmailWeeklySummary { get; set; }

    public bool EmailInvitations { get; set; } = true;

    public bool SyncEnabled { get; set; } = true;

    public bool SyncOnMobileData { get; set; }
}
