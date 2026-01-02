using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using MujDomecek.API.Options.Jobs;
using MujDomecek.API.Services;
using MujDomecek.Domain.Aggregates.Zaznam;
using MujDomecek.Domain.ValueObjects;
using MujDomecek.Infrastructure.Persistence;
using Quartz;

namespace MujDomecek.API.Jobs;

public sealed class DraftReminderJob : IJob
{
    private readonly ApplicationDbContext _dbContext;
    private readonly DraftReminderOptions _options;
    private readonly DraftCleanupOptions _cleanupOptions;
    private readonly ILogger<DraftReminderJob> _logger;

    public DraftReminderJob(
        ApplicationDbContext dbContext,
        IOptions<DraftReminderOptions> options,
        IOptions<DraftCleanupOptions> cleanupOptions,
        ILogger<DraftReminderJob> logger)
    {
        _dbContext = dbContext;
        _options = options.Value;
        _cleanupOptions = cleanupOptions.Value;
        _logger = logger;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        if (!_options.Enabled)
            return;

        var now = DateTime.UtcNow;
        var reminderCutoff = now.AddDays(-_options.ReminderAfterDays);
        var expirationCutoff = now.AddDays(-Math.Max(0, _cleanupOptions.DeleteAfterDays - _options.ExpirationWarningDays));

        var drafts = await _dbContext.Zaznamy
            .Where(z => z.Status == ZaznamStatus.Draft)
            .Where(z => !z.IsDeleted)
            .Where(z => z.CreatedAt <= reminderCutoff)
            .OrderBy(z => z.CreatedAt)
            .Take(_options.BatchSize)
            .Select(z => new DraftInfo(z.Id, z.PropertyId, z.CreatedAt))
            .ToListAsync(context.CancellationToken);

        if (drafts.Count == 0)
            return;

        var draftIds = drafts.Select(d => d.Id).ToList();
        var propertyIds = drafts.Select(d => d.PropertyId).Distinct().ToList();

        var propertyNames = await _dbContext.Properties
            .Where(p => propertyIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, p => p.Name, context.CancellationToken);

        var creators = await _dbContext.Activities
            .Where(a => a.Type == ActivityType.ZaznamCreated)
            .Where(a => a.TargetId.HasValue && draftIds.Contains(a.TargetId.Value))
            .GroupBy(a => a.TargetId!.Value)
            .Select(g => new { ZaznamId = g.Key, UserId = g.OrderBy(a => a.CreatedAt).Select(a => a.ActorUserId).FirstOrDefault() })
            .ToDictionaryAsync(x => x.ZaznamId, x => x.UserId, context.CancellationToken);

        var creatorIds = creators.Values.Where(id => id != Guid.Empty).Distinct().ToList();
        if (creatorIds.Count == 0)
            return;

        var preferences = await _dbContext.UserPreferences
            .Where(p => creatorIds.Contains(p.UserId))
            .ToDictionaryAsync(p => p.UserId, p => p.PushDraftReminders, context.CancellationToken);

        var notifications = await _dbContext.Notifications
            .Where(n => creatorIds.Contains(n.UserId))
            .Where(n => n.Type == NotificationType.DraftReminder || n.Type == NotificationType.DraftExpiring)
            .Select(n => new NotificationInfo(n.UserId, n.Type, n.PayloadJson))
            .ToListAsync(context.CancellationToken);

        var reminderLookup = BuildExistingLookup(notifications, NotificationType.DraftReminder);
        var expiringLookup = BuildExistingLookup(notifications, NotificationType.DraftExpiring);

        var reminderCount = 0;
        var expiringCount = 0;

        foreach (var draft in drafts)
        {
            if (!creators.TryGetValue(draft.Id, out var creatorId) || creatorId == Guid.Empty)
                continue;

            if (preferences.TryGetValue(creatorId, out var enabled) && !enabled)
                continue;

            var daysOld = (int)Math.Floor((now - draft.CreatedAt).TotalDays);
            var propertyName = propertyNames.TryGetValue(draft.PropertyId, out var name) ? name : string.Empty;

            if (!IsAlreadyNotified(reminderLookup, creatorId, draft.Id))
            {
                ActivityNotificationHelper.AddNotification(
                    _dbContext,
                    creatorId,
                    NotificationType.DraftReminder,
                    new
                    {
                        zaznamId = draft.Id,
                        propertyName,
                        createdAt = draft.CreatedAt,
                        daysOld
                    });
                AddNotified(reminderLookup, creatorId, draft.Id);
                reminderCount++;
            }

            if (draft.CreatedAt > expirationCutoff)
                continue;

            var expiresAt = draft.CreatedAt.AddDays(_cleanupOptions.DeleteAfterDays);
            var daysRemaining = (int)Math.Ceiling((expiresAt - now).TotalDays);
            if (daysRemaining <= 0)
                continue;

            if (IsAlreadyNotified(expiringLookup, creatorId, draft.Id))
                continue;

            ActivityNotificationHelper.AddNotification(
                _dbContext,
                creatorId,
                NotificationType.DraftExpiring,
                new
                {
                    zaznamId = draft.Id,
                    propertyName,
                    expiresAt,
                    daysRemaining
                });
            AddNotified(expiringLookup, creatorId, draft.Id);
            expiringCount++;
        }

        if (reminderCount == 0 && expiringCount == 0)
            return;

        await _dbContext.SaveChangesAsync(context.CancellationToken);

        _logger.LogInformation(
            "DraftReminder: Sent {ReminderCount} reminders and {ExpiringCount} expiring notices",
            reminderCount,
            expiringCount);
    }

    private static Dictionary<Guid, HashSet<Guid>> BuildExistingLookup(
        IReadOnlyList<NotificationInfo> notifications,
        NotificationType type)
    {
        var result = new Dictionary<Guid, HashSet<Guid>>();

        foreach (var notification in notifications)
        {
            if (notification.Type != type)
                continue;

            if (!TryExtractZaznamId(notification.PayloadJson, out var zaznamId))
                continue;

            if (!result.TryGetValue(notification.UserId, out var set))
            {
                set = [];
                result[notification.UserId] = set;
            }

            set.Add(zaznamId);
        }

        return result;
    }

    private static bool IsAlreadyNotified(
        IReadOnlyDictionary<Guid, HashSet<Guid>> lookup,
        Guid userId,
        Guid zaznamId)
    {
        return lookup.TryGetValue(userId, out var set) && set.Contains(zaznamId);
    }

    private static void AddNotified(
        IDictionary<Guid, HashSet<Guid>> lookup,
        Guid userId,
        Guid zaznamId)
    {
        if (!lookup.TryGetValue(userId, out var set))
        {
            set = [];
            lookup[userId] = set;
        }

        set.Add(zaznamId);
    }

    private static bool TryExtractZaznamId(string? payloadJson, out Guid zaznamId)
    {
        zaznamId = Guid.Empty;
        if (string.IsNullOrWhiteSpace(payloadJson))
            return false;

        try
        {
            using var doc = JsonDocument.Parse(payloadJson);
            if (!doc.RootElement.TryGetProperty("zaznamId", out var prop))
                return false;

            if (prop.ValueKind == JsonValueKind.String)
            {
                var value = prop.GetString();
                return Guid.TryParse(value, out zaznamId);
            }
        }
        catch (JsonException)
        {
            return false;
        }

        return false;
    }

    private sealed record DraftInfo(Guid Id, Guid PropertyId, DateTime CreatedAt);

    private sealed record NotificationInfo(Guid UserId, NotificationType Type, string PayloadJson);
}
