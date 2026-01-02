using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using MujDomecek.API.Options.Jobs;
using MujDomecek.Application.Abstractions;
using MujDomecek.Domain.ValueObjects;
using MujDomecek.Infrastructure.Persistence;
using Quartz;

namespace MujDomecek.API.Jobs;

public sealed class SyncRetryJob : IJob
{
    private readonly ApplicationDbContext _dbContext;
    private readonly SyncRetryOptions _options;
    private readonly ISyncQueueProcessor _processor;
    private readonly ILogger<SyncRetryJob> _logger;

    public SyncRetryJob(
        ApplicationDbContext dbContext,
        IOptions<SyncRetryOptions> options,
        ISyncQueueProcessor processor,
        ILogger<SyncRetryJob> logger)
    {
        _dbContext = dbContext;
        _options = options.Value;
        _processor = processor;
        _logger = logger;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        if (!_options.Enabled)
            return;

        var now = DateTime.UtcNow;

        var items = await _dbContext.SyncQueueItems
            .Where(item => item.Status == SyncQueueStatus.Failed)
            .Where(item => item.RetryCount < _options.MaxRetries)
            .Where(item => item.NextRetryAt == null || item.NextRetryAt <= now)
            .OrderBy(item => item.NextRetryAt ?? DateTime.MinValue)
            .Take(_options.BatchSize)
            .ToListAsync(context.CancellationToken);

        if (items.Count == 0)
            return;

        var processed = 0;

        foreach (var item in items)
        {
            var result = await _processor.ProcessAsync(item, context.CancellationToken);
            item.LastAttemptAt = now;

            if (result.Success)
            {
                _dbContext.SyncQueueItems.Remove(item);
                processed++;
                continue;
            }

            item.LastError = result.Error;
            item.RetryCount++;

            if (item.RetryCount >= _options.MaxRetries)
            {
                item.Status = SyncQueueStatus.PermanentlyFailed;
                item.NextRetryAt = null;
                continue;
            }

            item.Status = SyncQueueStatus.Failed;
            item.NextRetryAt = now.AddMinutes(GetDelayMinutes(item.RetryCount));
        }

        await _dbContext.SaveChangesAsync(context.CancellationToken);

        _logger.LogInformation("SyncRetry: processed {Processed} items", processed);
    }

    private int GetDelayMinutes(int retryCount)
    {
        var delays = _options.RetryDelayMinutes;
        if (delays.Length == 0)
            return 5;

        var index = Math.Clamp(retryCount - 1, 0, delays.Length - 1);
        return delays[index];
    }
}
