using MujDomecek.Domain.Aggregates.Sync;

namespace MujDomecek.Application.Abstractions;

public interface ISyncQueueProcessor
{
    Task<SyncQueueProcessResult> ProcessAsync(SyncQueueItem item, CancellationToken ct = default);
}

public sealed record SyncQueueProcessResult(bool Success, string? Error)
{
    public static SyncQueueProcessResult Ok() => new(true, null);

    public static SyncQueueProcessResult Fail(string error) => new(false, error);
}
