using System.Text.Json;
using MujDomecek.API.Endpoints;
using MujDomecek.Application.Abstractions;
using MujDomecek.Application.DTOs;
using MujDomecek.Domain.Aggregates.Sync;
using MujDomecek.Infrastructure.Persistence;

namespace MujDomecek.API.Services;

public sealed class SyncQueueProcessor : ISyncQueueProcessor
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<SyncQueueProcessor> _logger;

    public SyncQueueProcessor(ApplicationDbContext dbContext, ILogger<SyncQueueProcessor> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<SyncQueueProcessResult> ProcessAsync(SyncQueueItem item, CancellationToken ct = default)
    {
        if (item.UserId == Guid.Empty)
            return SyncQueueProcessResult.Fail("missing_user");

        if (item.EntityId == Guid.Empty)
            return SyncQueueProcessResult.Fail("missing_entity_id");

        if (!SyncEndpoints.TryNormalizeEntityType(item.EntityType, out var entityType))
            return SyncQueueProcessResult.Fail("invalid entityType");

        if (!SyncEndpoints.TryNormalizeOperation(item.Operation, out var operation))
            return SyncQueueProcessResult.Fail("invalid operation");

        JsonElement? data = null;
        if (!string.IsNullOrWhiteSpace(item.PayloadJson))
        {
            try
            {
                using var doc = JsonDocument.Parse(item.PayloadJson);
                data = doc.RootElement.Clone();
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "SyncQueue: invalid payload for item {ItemId}", item.Id);
                return SyncQueueProcessResult.Fail("invalid_payload");
            }
        }

        var timestamp = item.CreatedAt == default ? DateTime.UtcNow : item.CreatedAt;
        var change = new SyncChange(
            item.Id.ToString("D"),
            entityType,
            item.EntityId,
            operation,
            data,
            timestamp);

        var normalizedTimestamp = SyncEndpoints.NormalizeTimestamp(change.ClientTimestamp);
        var error = await SyncEndpoints.ApplyChangeAsync(
            _dbContext,
            item.UserId,
            change,
            entityType,
            operation,
            normalizedTimestamp);

        return error is null
            ? SyncQueueProcessResult.Ok()
            : SyncQueueProcessResult.Fail(error);
    }
}
