using Microsoft.EntityFrameworkCore;
using MujDomecek.Domain.Aggregates.Project;
using MujDomecek.Domain.ValueObjects;
using MujDomecek.Infrastructure.Persistence;

namespace MujDomecek.Infrastructure.Tests;

[Collection("Postgres")]
public class ApplicationDbContextTests
{
    private readonly PostgresFixture _fixture;

    public ApplicationDbContextTests(PostgresFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task SaveChangesAsync_NewEntity_SetsAuditFields()
    {
        await using var context = _fixture.CreateDbContext();
        var project = new Project
        {
            Id = Guid.NewGuid(),
            Name = "Audit test",
            OwnerId = Guid.NewGuid()
        };

        context.Projects.Add(project);
        await context.SaveChangesAsync();

        Assert.NotEqual(default, project.CreatedAt);
        Assert.NotEqual(default, project.UpdatedAt);
        Assert.True(project.ServerRevision > 0);
    }

    [Fact]
    public async Task Projects_QueryFilter_HidesSoftDeletedEntities()
    {
        await using var context = _fixture.CreateDbContext();
        var active = new Project
        {
            Id = Guid.NewGuid(),
            Name = "Active",
            OwnerId = Guid.NewGuid()
        };
        var deleted = new Project
        {
            Id = Guid.NewGuid(),
            Name = "Deleted",
            OwnerId = Guid.NewGuid(),
            IsDeleted = true,
            DeletedAt = DateTime.UtcNow
        };

        context.Projects.AddRange(active, deleted);
        await context.SaveChangesAsync();

        var visible = await context.Projects
            .Where(p => p.Id == active.Id || p.Id == deleted.Id)
            .ToListAsync();

        Assert.Single(visible);
        Assert.Equal(active.Id, visible[0].Id);

        var all = await context.Projects
            .IgnoreQueryFilters()
            .Where(p => p.Id == active.Id || p.Id == deleted.Id)
            .ToListAsync();

        Assert.Equal(2, all.Count);
    }

    [Fact]
    public void MediaEntity_ConfiguresOwnerIndex()
    {
        using var context = _fixture.CreateDbContext();
        var entityType = context.Model.FindEntityType("MujDomecek.Domain.Aggregates.Zaznam.Media");

        Assert.NotNull(entityType);

        var hasOwnerIndex = entityType!.GetIndexes()
            .Any(index =>
                index.Properties.Select(p => p.Name).SequenceEqual(["OwnerType", "OwnerId"]));

        Assert.True(hasOwnerIndex);
    }

    [Fact]
    public void PropertyEntity_IncludesPropertyTypeColumn()
    {
        using var context = _fixture.CreateDbContext();
        var entityType = context.Model.FindEntityType("MujDomecek.Domain.Aggregates.Property.Property");

        Assert.NotNull(entityType);

        var propertyType = entityType!.FindProperty("PropertyType");
        Assert.NotNull(propertyType);
        Assert.Equal(typeof(PropertyType), propertyType!.ClrType);
    }

    [Fact]
    public void UnitType_Enum_UsesSimplifiedValues()
    {
        var names = Enum.GetNames(typeof(UnitType));

        Assert.Equal(new[] { "Room", "Floor", "Cellar", "Parking", "Other" }, names);
    }
}
