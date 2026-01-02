using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using MujDomecek.API.Options.Jobs;
using MujDomecek.Domain.Aggregates.Zaznam;
using MujDomecek.Domain.ValueObjects;
using MujDomecek.Infrastructure.Persistence;
using Quartz;

namespace MujDomecek.API.Jobs;

public sealed class DraftCleanupJob : IJob
{
    private readonly ApplicationDbContext _dbContext;
    private readonly DraftCleanupOptions _options;
    private readonly ILogger<DraftCleanupJob> _logger;

    public DraftCleanupJob(
        ApplicationDbContext dbContext,
        IOptions<DraftCleanupOptions> options,
        ILogger<DraftCleanupJob> logger)
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
        var cutoffDate = now.AddDays(-_options.DeleteAfterDays);

        var drafts = await _dbContext.Zaznamy
            .Where(z => z.Status == ZaznamStatus.Draft)
            .Where(z => !z.IsDeleted)
            .Where(z => z.CreatedAt < cutoffDate)
            .OrderBy(z => z.CreatedAt)
            .Take(_options.BatchSize)
            .ToListAsync(context.CancellationToken);

        if (drafts.Count == 0)
            return;

        var draftIds = drafts.Select(d => d.Id).ToList();
        var documents = await _dbContext.ZaznamDokumenty
            .Where(d => draftIds.Contains(d.ZaznamId))
            .Where(d => !d.IsDeleted)
            .ToListAsync(context.CancellationToken);

        foreach (var document in documents)
        {
            document.IsDeleted = true;
            document.DeletedAt = now;
            document.UpdatedAt = now;
        }

        foreach (var draft in drafts)
        {
            draft.IsDeleted = true;
            draft.DeletedAt = now;
            draft.UpdatedAt = now;
        }

        await _dbContext.SaveChangesAsync(context.CancellationToken);

        _logger.LogInformation(
            "DraftCleanup: Deleted {DraftCount} drafts and {DocumentCount} documents",
            drafts.Count,
            documents.Count);
    }
}
