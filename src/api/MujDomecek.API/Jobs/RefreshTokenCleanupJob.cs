using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using MujDomecek.API.Options.Jobs;
using MujDomecek.Infrastructure.Persistence;
using Quartz;

namespace MujDomecek.API.Jobs;

public sealed class RefreshTokenCleanupJob : IJob
{
    private readonly ApplicationDbContext _dbContext;
    private readonly RefreshTokenCleanupOptions _options;
    private readonly ILogger<RefreshTokenCleanupJob> _logger;

    public RefreshTokenCleanupJob(
        ApplicationDbContext dbContext,
        IOptions<RefreshTokenCleanupOptions> options,
        ILogger<RefreshTokenCleanupJob> logger)
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
        var revokedCutoff = now.AddDays(-7);
        var totalDeleted = 0;

        while (!context.CancellationToken.IsCancellationRequested)
        {
            var tokens = await _dbContext.RefreshTokens
                .Where(t => t.ExpiresAt < now
                    || (t.RevokedAt != null && t.RevokedAt < revokedCutoff))
                .OrderBy(t => t.ExpiresAt)
                .Take(_options.BatchSize)
                .ToListAsync(context.CancellationToken);

            if (tokens.Count == 0)
                break;

            _dbContext.RefreshTokens.RemoveRange(tokens);
            await _dbContext.SaveChangesAsync(context.CancellationToken);
            totalDeleted += tokens.Count;

            if (tokens.Count < _options.BatchSize)
                break;
        }

        if (totalDeleted > 0)
        {
            _logger.LogInformation(
                "RefreshTokenCleanup: Deleted {Count} tokens",
                totalDeleted);
        }
    }
}
