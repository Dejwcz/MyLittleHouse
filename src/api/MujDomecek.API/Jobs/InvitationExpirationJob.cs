using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using MujDomecek.API.Options.Jobs;
using MujDomecek.API.Services;
using MujDomecek.Domain.Aggregates.Project;
using MujDomecek.Domain.ValueObjects;
using MujDomecek.Infrastructure.Persistence;
using Quartz;

namespace MujDomecek.API.Jobs;

public sealed class InvitationExpirationJob : IJob
{
    private readonly ApplicationDbContext _dbContext;
    private readonly InvitationExpirationOptions _options;
    private readonly ILogger<InvitationExpirationJob> _logger;

    public InvitationExpirationJob(
        ApplicationDbContext dbContext,
        IOptions<InvitationExpirationOptions> options,
        ILogger<InvitationExpirationJob> logger)
    {
        _dbContext = dbContext;
        _options = options.Value;
        _logger = logger;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        if (!_options.Enabled)
            return;

        var now = DateTime.UtcNow;
        var invitations = await _dbContext.Invitations
            .Where(i => i.Status == InvitationStatus.Pending)
            .Where(i => i.ExpiresAt < now)
            .OrderBy(i => i.ExpiresAt)
            .Take(_options.BatchSize)
            .ToListAsync(context.CancellationToken);

        if (invitations.Count == 0)
            return;

        var projectIds = invitations
            .Where(i => i.TargetType == InvitationTargetType.Project)
            .Select(i => i.TargetId)
            .Distinct()
            .ToList();

        var propertyIds = invitations
            .Where(i => i.TargetType == InvitationTargetType.Property)
            .Select(i => i.TargetId)
            .Distinct()
            .ToList();

        var zaznamIds = invitations
            .Where(i => i.TargetType == InvitationTargetType.Zaznam)
            .Select(i => i.TargetId)
            .Distinct()
            .ToList();

        var projectNames = projectIds.Count == 0
            ? []
            : await _dbContext.Projects
                .Where(p => projectIds.Contains(p.Id))
                .ToDictionaryAsync(p => p.Id, p => p.Name, context.CancellationToken);

        var propertyNames = propertyIds.Count == 0
            ? []
            : await _dbContext.Properties
                .Where(p => propertyIds.Contains(p.Id))
                .ToDictionaryAsync(p => p.Id, p => p.Name, context.CancellationToken);

        var zaznamNames = zaznamIds.Count == 0
            ? []
            : await _dbContext.Zaznamy
                .Where(z => zaznamIds.Contains(z.Id))
                .ToDictionaryAsync(z => z.Id, z => z.Title ?? string.Empty, context.CancellationToken);

        foreach (var invitation in invitations)
        {
            invitation.Status = InvitationStatus.Expired;

            var targetName = invitation.TargetType switch
            {
                InvitationTargetType.Project => projectNames.TryGetValue(invitation.TargetId, out var projectName) ? projectName : string.Empty,
                InvitationTargetType.Property => propertyNames.TryGetValue(invitation.TargetId, out var propertyName) ? propertyName : string.Empty,
                InvitationTargetType.Zaznam => zaznamNames.TryGetValue(invitation.TargetId, out var zaznamName) ? zaznamName : string.Empty,
                _ => string.Empty
            };

            ActivityNotificationHelper.AddNotification(
                _dbContext,
                invitation.CreatedBy,
                NotificationType.InvitationExpired,
                new
                {
                    invitationId = invitation.Id,
                    email = invitation.Email,
                    targetType = ToTypeString(invitation.TargetType),
                    targetId = invitation.TargetId,
                    targetName
                });
        }

        await _dbContext.SaveChangesAsync(context.CancellationToken);

        _logger.LogInformation(
            "InvitationExpiration: Expired {Count} invitations",
            invitations.Count);
    }

    private static string ToTypeString(InvitationTargetType type)
    {
        return type switch
        {
            InvitationTargetType.Project => "project",
            InvitationTargetType.Property => "property",
            InvitationTargetType.Zaznam => "zaznam",
            _ => "project"
        };
    }
}
