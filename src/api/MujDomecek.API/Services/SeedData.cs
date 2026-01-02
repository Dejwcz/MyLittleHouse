using Microsoft.EntityFrameworkCore;
using MujDomecek.Domain.Aggregates.Zaznam;
using MujDomecek.Infrastructure.Persistence;

namespace MujDomecek.API.Services;

public static class SeedData
{
    public static async Task SeedTagsAsync(ApplicationDbContext dbContext, IConfiguration configuration)
    {
        var seed = configuration.GetSection("Tags:Seed").Get<List<TagSeedItem>>() ?? [];
        if (seed.Count == 0)
            return;

        var existingIds = await dbContext.Tags.Select(t => t.Id).ToListAsync();
        var missing = seed.Where(s => !existingIds.Contains(s.Id)).ToList();

        if (missing.Count == 0)
            return;

        foreach (var item in missing)
        {
            dbContext.Tags.Add(new Tag
            {
                Id = item.Id,
                Name = item.Name,
                Icon = item.Icon,
                SortOrder = item.Id,
                IsActive = true
            });
        }

        await dbContext.SaveChangesAsync();
    }

    public sealed record TagSeedItem(short Id, string Name, string Icon);
}
