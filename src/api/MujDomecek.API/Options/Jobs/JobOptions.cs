namespace MujDomecek.API.Options.Jobs;

public sealed class DraftCleanupOptions
{
    public const string SectionName = "Jobs:DraftCleanup";

    public bool Enabled { get; set; } = true;

    public string CronSchedule { get; set; } = "0 0 3 * * ?";

    public int WarningDaysBeforeDelete { get; set; } = 7;

    public int DeleteAfterDays { get; set; } = 30;

    public int BatchSize { get; set; } = 100;
}

public sealed class DraftReminderOptions
{
    public const string SectionName = "Jobs:DraftReminder";

    public bool Enabled { get; set; } = true;

    public string CronSchedule { get; set; } = "0 0 10 * * ?";

    public int ReminderAfterDays { get; set; } = 3;

    public int ExpirationWarningDays { get; set; } = 7;

    public int BatchSize { get; set; } = 500;
}

public sealed class InvitationExpirationOptions
{
    public const string SectionName = "Jobs:InvitationExpiration";

    public bool Enabled { get; set; } = true;

    public string CronSchedule { get; set; } = "0 0 4 * * ?";

    public int BatchSize { get; set; } = 200;
}

public sealed class ActivityCleanupOptions
{
    public const string SectionName = "Jobs:ActivityCleanup";

    public bool Enabled { get; set; } = true;

    public string CronSchedule { get; set; } = "0 0 2 ? * SUN";

    public int RetentionDays { get; set; } = 90;

    public int BatchSize { get; set; } = 1000;
}

public sealed class NotificationCleanupOptions
{
    public const string SectionName = "Jobs:NotificationCleanup";

    public bool Enabled { get; set; } = true;

    public string CronSchedule { get; set; } = "0 0 2 ? * SUN";

    public int RetentionDaysRead { get; set; } = 30;

    public int RetentionDaysUnread { get; set; } = 90;

    public int BatchSize { get; set; } = 1000;
}

public sealed class RefreshTokenCleanupOptions
{
    public const string SectionName = "Jobs:RefreshTokenCleanup";

    public bool Enabled { get; set; } = true;

    public string CronSchedule { get; set; } = "0 0 5 * * ?";

    public int BatchSize { get; set; } = 500;
}

public sealed class WeeklySummaryEmailOptions
{
    public const string SectionName = "Jobs:WeeklySummaryEmail";

    public bool Enabled { get; set; } = true;

    public string CronSchedule { get; set; } = "0 0 9 ? * MON";

    public int BatchSize { get; set; } = 100;
}

public sealed class ExportDataOptions
{
    public const string SectionName = "Jobs:ExportData";

    public int ExpirationHours { get; set; } = 24;

    public long MaxSizeBytes { get; set; } = 1_073_741_824;

    public bool IncludePhotos { get; set; } = true;
}

public sealed class SyncRetryOptions
{
    public const string SectionName = "Jobs:SyncRetry";

    public bool Enabled { get; set; } = true;

    public string CronSchedule { get; set; } = "0 */5 * * * ?";

    public int MaxRetries { get; set; } = 5;

    public int[] RetryDelayMinutes { get; set; } = new[] { 5, 15, 60, 240, 1440 };

    public int BatchSize { get; set; } = 50;
}
