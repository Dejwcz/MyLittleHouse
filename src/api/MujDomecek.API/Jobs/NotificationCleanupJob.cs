using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using MujDomecek.API.Options.Jobs;
using MujDomecek.Infrastructure.Persistence;
using Quartz;

namespace MujDomecek.API.Jobs;

public sealed class NotificationCleanupJob : IJob
{
    private readonly ApplicationDbContext _dbContext;
    private readonly NotificationCleanupOptions _options;
    private readonly ILogger<NotificationCleanupJob> _logger;

    public NotificationCleanupJob(
        ApplicationDbContext dbContext,
        IOptions<NotificationCleanupOptions> options,
        ILogger<NotificationCleanupJob> logger)
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
        var readCutoff = now.AddDays(-_options.RetentionDaysRead);
        var unreadCutoff = now.AddDays(-_options.RetentionDaysUnread);

        var deletedRead = 0;
        while (!context.CancellationToken.IsCancellationRequested)
        {
            var deleted = await _dbContext.Notifications
                .Where(n => n.ReadAt != null && n.ReadAt < readCutoff)
                .Take(_options.BatchSize)
                .ExecuteDeleteAsync(context.CancellationToken);

            deletedRead += deleted;
            if (deleted < _options.BatchSize)
                break;
        }

        var deletedUnread = 0;
        while (!context.CancellationToken.IsCancellationRequested)
        {
            var deleted = await _dbContext.Notifications
                .Where(n => n.ReadAt == null && n.CreatedAt < unreadCutoff)
                .Take(_options.BatchSize)
                .ExecuteDeleteAsync(context.CancellationToken);

            deletedUnread += deleted;
            if (deleted < _options.BatchSize)
                break;
        }

        if (deletedRead > 0 || deletedUnread > 0)
        {
            _logger.LogInformation(
                "NotificationCleanup: Deleted {ReadCount} read and {UnreadCount} unread notifications",
                deletedRead,
                deletedUnread);
        }
    }
}
