using Microsoft.EntityFrameworkCore;
using MujDomecek.Infrastructure.Persistence;
using Testcontainers.PostgreSql;

namespace MujDomecek.Infrastructure.Tests;

public sealed class PostgresFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .Build();

    private DbContextOptions<ApplicationDbContext>? _dbOptions;

    public async Task InitializeAsync()
    {
        await _container.StartAsync();

        _dbOptions = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(_container.GetConnectionString())
            .Options;

        await using var context = new ApplicationDbContext(_dbOptions);
        await context.Database.EnsureCreatedAsync();
    }

    public ApplicationDbContext CreateDbContext()
    {
        if (_dbOptions is null)
            throw new InvalidOperationException("Postgres fixture not initialized.");

        return new ApplicationDbContext(_dbOptions);
    }

    public async Task DisposeAsync()
    {
        await _container.DisposeAsync();
    }
}
