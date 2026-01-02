using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using MujDomecek.API.Options.Jobs;
using MujDomecek.Infrastructure.Persistence;
using Quartz;

namespace MujDomecek.API.Jobs;

public sealed class ActivityCleanupJob : IJob
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ActivityCleanupOptions _options;
    private readonly ILogger<ActivityCleanupJob> _logger;

    public ActivityCleanupJob(
        ApplicationDbContext dbContext,
        IOptions<ActivityCleanupOptions> options,
        ILogger<ActivityCleanupJob> logger)
    {
        _dbContext = dbContext;
        _options = options.Value;
        _logger = logger;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        if (!_options.Enabled)
            return;

        var cutoffDate = DateTime.UtcNow.AddDays(-_options.RetentionDays);
        var totalDeleted = 0;

        while (!context.CancellationToken.IsCancellationRequested)
        {
            var deleted = await _dbContext.Activities
                .Where(a => a.CreatedAt < cutoffDate)
                .Take(_options.BatchSize)
                .ExecuteDeleteAsync(context.CancellationToken);

            totalDeleted += deleted;

            if (deleted < _options.BatchSize)
                break;
        }

        if (totalDeleted > 0)
        {
            _logger.LogInformation(
                "ActivityCleanup: Deleted {Count} activities",
                totalDeleted);
        }
    }
}
