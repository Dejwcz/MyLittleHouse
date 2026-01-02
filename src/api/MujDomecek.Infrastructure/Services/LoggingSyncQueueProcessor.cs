using Microsoft.Extensions.Logging;
using MujDomecek.Application.Abstractions;
using MujDomecek.Domain.Aggregates.Sync;

namespace MujDomecek.Infrastructure.Services;

public sealed class LoggingSyncQueueProcessor : ISyncQueueProcessor
{
    private readonly ILogger<LoggingSyncQueueProcessor> _logger;

    public LoggingSyncQueueProcessor(ILogger<LoggingSyncQueueProcessor> logger)
    {
        _logger = logger;
    }

    public Task<SyncQueueProcessResult> ProcessAsync(SyncQueueItem item, CancellationToken ct = default)
    {
        _logger.LogWarning(
            "SyncQueueProcessor not configured. Item {ItemId} stays failed.",
            item.Id);
        return Task.FromResult(SyncQueueProcessResult.Fail("sync_processor_not_configured"));
    }
}
