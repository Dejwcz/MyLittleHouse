using MujDomecek.Domain.Aggregates.Project;
using MujDomecek.Infrastructure.Persistence;

namespace MujDomecek.Infrastructure.Tests;

[Collection("Postgres")]
public class EfRepositoryTests
{
    private readonly PostgresFixture _fixture;

    public EfRepositoryTests(PostgresFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task AddAsync_PersistsAndLoadsEntity()
    {
        await using var context = _fixture.CreateDbContext();
        var repository = new EfRepository<Project>(context);
        var project = new Project
        {
            Id = Guid.NewGuid(),
            Name = "Repository test",
            OwnerId = Guid.NewGuid()
        };

        await repository.AddAsync(project);
        await context.SaveChangesAsync();

        var loaded = await repository.GetByIdAsync(project.Id);

        Assert.NotNull(loaded);
        Assert.Equal(project.Name, loaded!.Name);
        Assert.Equal(project.OwnerId, loaded.OwnerId);
    }

    [Fact]
    public async Task Remove_RemovesEntity()
    {
        await using var context = _fixture.CreateDbContext();
        var repository = new EfRepository<Project>(context);
        var project = new Project
        {
            Id = Guid.NewGuid(),
            Name = "Remove test",
            OwnerId = Guid.NewGuid()
        };

        await repository.AddAsync(project);
        await context.SaveChangesAsync();

        repository.Remove(project);
        await context.SaveChangesAsync();

        await using var verifyContext = _fixture.CreateDbContext();
        var verifyRepository = new EfRepository<Project>(verifyContext);
        var loaded = await verifyRepository.GetByIdAsync(project.Id);

        Assert.Null(loaded);
    }
}
