using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using MujDomecek.API.Options.Jobs;
using MujDomecek.API.Services;
using MujDomecek.Application.Abstractions;
using MujDomecek.Domain.ValueObjects;
using MujDomecek.Infrastructure.Persistence;
using Quartz;

namespace MujDomecek.API.Jobs;

public sealed class WeeklySummaryEmailJob : IJob
{
    private readonly ApplicationDbContext _dbContext;
    private readonly WeeklySummaryEmailOptions _options;
    private readonly DraftCleanupOptions _draftCleanupOptions;
    private readonly DraftReminderOptions _draftReminderOptions;
    private readonly IEmailSender _emailSender;
    private readonly EmailTemplateService _emailTemplates;
    private readonly ILogger<WeeklySummaryEmailJob> _logger;

    public WeeklySummaryEmailJob(
        ApplicationDbContext dbContext,
        IOptions<WeeklySummaryEmailOptions> options,
        IOptions<DraftCleanupOptions> draftCleanupOptions,
        IOptions<DraftReminderOptions> draftReminderOptions,
        IEmailSender emailSender,
        EmailTemplateService emailTemplates,
        ILogger<WeeklySummaryEmailJob> logger)
    {
        _dbContext = dbContext;
        _options = options.Value;
        _draftCleanupOptions = draftCleanupOptions.Value;
        _draftReminderOptions = draftReminderOptions.Value;
        _emailSender = emailSender;
        _emailTemplates = emailTemplates;
        _logger = logger;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        if (!_options.Enabled)
            return;

        var now = DateTime.UtcNow;
        var periodStart = now.AddDays(-7);

        var users = await _dbContext.Users
            .Where(u => !u.IsDeleted && !u.IsBlocked && u.Email != null)
            .Join(_dbContext.UserPreferences, u => u.Id, p => p.UserId, (u, p) => new { u, p })
            .Where(x => x.p.EmailWeeklySummary)
            .OrderBy(x => x.u.CreatedAt)
            .Take(_options.BatchSize)
            .ToListAsync(context.CancellationToken);

        var sent = 0;

        foreach (var entry in users)
        {
            var user = entry.u;
            var email = user.Email ?? string.Empty;
            if (string.IsNullOrWhiteSpace(email))
                continue;

            var myCreated = await _dbContext.Activities.CountAsync(
                a => a.ActorUserId == user.Id
                    && a.Type == ActivityType.ZaznamCreated
                    && a.CreatedAt >= periodStart,
                context.CancellationToken);

            var myUpdated = await _dbContext.Activities.CountAsync(
                a => a.ActorUserId == user.Id
                    && a.Type == ActivityType.ZaznamUpdated
                    && a.CreatedAt >= periodStart,
                context.CancellationToken);

            var myComments = await _dbContext.Activities.CountAsync(
                a => a.ActorUserId == user.Id
                    && a.Type == ActivityType.CommentAdded
                    && a.CreatedAt >= periodStart,
                context.CancellationToken);

            var sharedPropertyIds = await GetSharedPropertyIdsAsync(user.Id, context.CancellationToken);

            var sharedZaznamy = sharedPropertyIds.Count == 0
                ? 0
                : await _dbContext.Activities.CountAsync(
                    a => sharedPropertyIds.Contains(a.PropertyId)
                        && a.Type == ActivityType.ZaznamCreated
                        && a.CreatedAt >= periodStart,
                    context.CancellationToken);

            var sharedComments = sharedPropertyIds.Count == 0
                ? 0
                : await _dbContext.Activities.CountAsync(
                    a => sharedPropertyIds.Contains(a.PropertyId)
                        && a.Type == ActivityType.CommentAdded
                        && a.CreatedAt >= periodStart,
                    context.CancellationToken);

            var sharedMembers = sharedPropertyIds.Count == 0
                ? 0
                : await _dbContext.Activities.CountAsync(
                    a => sharedPropertyIds.Contains(a.PropertyId)
                        && a.Type == ActivityType.MemberJoined
                        && a.CreatedAt >= periodStart,
                    context.CancellationToken);

            var pendingDrafts = await CountUserDraftsAsync(user.Id, null, context.CancellationToken);
            var expiringCutoff = now.AddDays(-Math.Max(0, _draftCleanupOptions.DeleteAfterDays - _draftReminderOptions.ExpirationWarningDays));
            var expiringDrafts = await CountUserDraftsAsync(user.Id, expiringCutoff, context.CancellationToken);

            var pendingInvitations = await CountPendingInvitationsAsync(email, now, context.CancellationToken);

            if (myCreated == 0
                && myUpdated == 0
                && myComments == 0
                && sharedZaznamy == 0
                && sharedComments == 0
                && sharedMembers == 0
                && pendingDrafts == 0
                && expiringDrafts == 0
                && pendingInvitations == 0)
            {
                continue;
            }

            var summaryEmail = _emailTemplates.Render(
                "weekly-summary",
                new Dictionary<string, string?>
                {
                    ["userName"] = BuildDisplayName(user.FirstName, user.LastName, email),
                    ["periodStart"] = periodStart.ToString("yyyy-MM-dd"),
                    ["periodEnd"] = now.ToString("yyyy-MM-dd"),
                    ["myCreated"] = myCreated.ToString(),
                    ["myUpdated"] = myUpdated.ToString(),
                    ["myComments"] = myComments.ToString(),
                    ["sharedZaznamy"] = sharedZaznamy.ToString(),
                    ["sharedComments"] = sharedComments.ToString(),
                    ["sharedMembers"] = sharedMembers.ToString(),
                    ["pendingDrafts"] = pendingDrafts.ToString(),
                    ["expiringDrafts"] = expiringDrafts.ToString(),
                    ["pendingInvitations"] = pendingInvitations.ToString()
                });

            await _emailSender.SendAsync(email, summaryEmail.Subject, summaryEmail.Body, context.CancellationToken);
            sent++;
        }

        if (sent > 0)
        {
            _logger.LogInformation("WeeklySummaryEmail: Sent {Count} summaries", sent);
        }
    }

    private async Task<List<Guid>> GetSharedPropertyIdsAsync(Guid userId, CancellationToken ct)
    {
        return await _dbContext.Properties
            .Where(p => !_dbContext.Projects.Any(pr => pr.Id == p.ProjectId && pr.OwnerId == userId))
            .Where(p =>
                _dbContext.ProjectMembers.Any(pm => pm.ProjectId == p.ProjectId && pm.UserId == userId)
                || _dbContext.PropertyMembers.Any(pm => pm.PropertyId == p.Id && pm.UserId == userId))
            .Select(p => p.Id)
            .Distinct()
            .ToListAsync(ct);
    }

    private async Task<int> CountUserDraftsAsync(Guid userId, DateTime? createdBefore, CancellationToken ct)
    {
        var query = _dbContext.Zaznamy
            .Where(z => z.Status == ZaznamStatus.Draft)
            .Where(z => !z.IsDeleted)
            .Where(z => _dbContext.Activities.Any(a =>
                a.Type == ActivityType.ZaznamCreated
                && a.ActorUserId == userId
                && a.TargetId == z.Id));

        if (createdBefore.HasValue)
            query = query.Where(z => z.CreatedAt <= createdBefore.Value);

        return await query.CountAsync(ct);
    }

    private async Task<int> CountPendingInvitationsAsync(string email, DateTime now, CancellationToken ct)
    {
        var normalized = NormalizeEmail(email);
        if (normalized is null)
            return 0;

        return await _dbContext.Invitations
            .Where(i => i.Status == InvitationStatus.Pending && i.ExpiresAt > now)
            .Where(i => i.Email.ToUpper() == normalized)
            .CountAsync(ct);
    }

    private static string? NormalizeEmail(string? email)
    {
        return string.IsNullOrWhiteSpace(email)
            ? null
            : email.Trim().ToUpperInvariant();
    }

    private static string BuildDisplayName(string? firstName, string? lastName, string? email)
    {
        var name = $"{firstName} {lastName}".Trim();
        return string.IsNullOrWhiteSpace(name) ? email ?? string.Empty : name;
    }
}
