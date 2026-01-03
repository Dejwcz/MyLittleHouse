using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using MujDomecek.API.Extensions;
using MujDomecek.Application.Abstractions;
using MujDomecek.Application.DTOs;
using MujDomecek.Domain.Aggregates.Property;
using MujDomecek.Domain.Aggregates.Zaznam;
using MujDomecek.Domain.ValueObjects;
using MujDomecek.Infrastructure.Persistence;

namespace MujDomecek.API.Endpoints;

public static class UnitEndpoints
{
    public static IEndpointRouteBuilder MapUnitEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/units").RequireAuthorization();

        group.MapGet("/", GetUnitsAsync);
        group.MapPost("/", CreateUnitAsync);
        group.MapGet("/{id:guid}", GetUnitAsync);
        group.MapPut("/{id:guid}", UpdateUnitAsync);
        group.MapPatch("/{id:guid}/cover", UpdateUnitCoverAsync);
        group.MapDelete("/{id:guid}", DeleteUnitAsync);

        return endpoints;
    }

    private static async Task<IResult> GetUnitsAsync(
        ClaimsPrincipal user,
        Guid propertyId,
        Guid? parentUnitId,
        IStorageService storageService,
        ApplicationDbContext dbContext)
    {
        var userId = user.GetUserId();
        if (userId == Guid.Empty)
            return Results.Unauthorized();

        if (propertyId == Guid.Empty)
            return Results.BadRequest();

        var propertyExists = await dbContext.Properties.AnyAsync(p => p.Id == propertyId);
        if (!propertyExists)
            return Results.NotFound();

        if (!await HasPropertyAccessAsync(dbContext, propertyId, userId))
            return Results.Forbid();

        var query = dbContext.Units.Where(u => u.PropertyId == propertyId);
        if (parentUnitId.HasValue)
            query = query.Where(u => u.ParentUnitId == parentUnitId.Value);

        var coverMediaIds = await query
            .Select(u => u.CoverMediaId)
            .Where(id => id.HasValue)
            .Select(id => id.GetValueOrDefault())
            .Distinct()
            .ToListAsync();

        Dictionary<Guid, string?> coverLookup = coverMediaIds.Count == 0
            ? new Dictionary<Guid, string?>()
            : await dbContext.Media
                .Where(m => coverMediaIds.Contains(m.Id) && m.OwnerType == OwnerType.Unit)
                .Select(m => new { m.Id, m.StorageKey })
                .ToDictionaryAsync(m => m.Id, m => (string?)m.StorageKey);

        var units = await query
            .Select(u => new UnitDto(
                u.Id,
                u.PropertyId,
                u.ParentUnitId,
                u.Name,
                u.Description,
                u.UnitType.ToString().ToLowerInvariant(),
                dbContext.Units.Count(c => c.ParentUnitId == u.Id),
                dbContext.Zaznamy.Count(z => z.UnitId == u.Id),
                u.CoverMediaId,
                GetCoverUrl(storageService, coverLookup, u.CoverMediaId),
                u.CreatedAt,
                u.UpdatedAt))
            .ToListAsync();

        return Results.Ok(units);
    }

    private static async Task<IResult> CreateUnitAsync(
        ClaimsPrincipal user,
        [FromBody] CreateUnitRequest request,
        ApplicationDbContext dbContext)
    {
        var userId = user.GetUserId();
        if (userId == Guid.Empty)
            return Results.Unauthorized();

        var propertyExists = await dbContext.Properties.AnyAsync(p => p.Id == request.PropertyId);
        if (!propertyExists)
            return Results.NotFound();

        if (!await HasPropertyAccessAsync(dbContext, request.PropertyId, userId))
            return Results.Forbid();

        if (request.ParentUnitId.HasValue)
        {
            var parentValid = await dbContext.Units
                .AnyAsync(u => u.Id == request.ParentUnitId.Value && u.PropertyId == request.PropertyId);
            if (!parentValid)
                return Results.BadRequest();
        }

        var unit = new Unit
        {
            Id = Guid.NewGuid(),
            PropertyId = request.PropertyId,
            ParentUnitId = request.ParentUnitId,
            Name = request.Name,
            Description = request.Description,
            UnitType = Enum.TryParse<UnitType>(request.UnitType, true, out var parsed) ? parsed : UnitType.Other,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        dbContext.Units.Add(unit);
        await dbContext.SaveChangesAsync();

        var dto = new UnitDto(
            unit.Id,
            unit.PropertyId,
            unit.ParentUnitId,
            unit.Name,
            unit.Description,
            unit.UnitType.ToString().ToLowerInvariant(),
            0,
            0,
            unit.CoverMediaId,
            null,
            unit.CreatedAt,
            unit.UpdatedAt);

        return Results.Created($"/units/{unit.Id}", dto);
    }

    private static async Task<IResult> GetUnitAsync(
        ClaimsPrincipal user,
        Guid id,
        IStorageService storageService,
        ApplicationDbContext dbContext)
    {
        var userId = user.GetUserId();
        if (userId == Guid.Empty)
            return Results.Unauthorized();

        var unit = await dbContext.Units.FirstOrDefaultAsync(u => u.Id == id);
        if (unit is null)
            return Results.NotFound();

        if (!await HasPropertyAccessAsync(dbContext, unit.PropertyId, userId))
            return Results.Forbid();

        var coverUrl = await GetCoverUrlAsync(dbContext, storageService, OwnerType.Unit, unit.Id, unit.CoverMediaId);

        var dto = new UnitDto(
            unit.Id,
            unit.PropertyId,
            unit.ParentUnitId,
            unit.Name,
            unit.Description,
            unit.UnitType.ToString().ToLowerInvariant(),
            dbContext.Units.Count(c => c.ParentUnitId == unit.Id),
            dbContext.Zaznamy.Count(z => z.UnitId == unit.Id),
            unit.CoverMediaId,
            coverUrl,
            unit.CreatedAt,
            unit.UpdatedAt);

        return Results.Ok(dto);
    }

    private static async Task<IResult> UpdateUnitAsync(
        ClaimsPrincipal user,
        Guid id,
        [FromBody] UpdateUnitRequest request,
        IStorageService storageService,
        ApplicationDbContext dbContext)
    {
        var userId = user.GetUserId();
        if (userId == Guid.Empty)
            return Results.Unauthorized();

        var unit = await dbContext.Units.FirstOrDefaultAsync(u => u.Id == id);
        if (unit is null)
            return Results.NotFound();

        if (!await HasPropertyAccessAsync(dbContext, unit.PropertyId, userId))
            return Results.Forbid();

        if (!string.IsNullOrWhiteSpace(request.Name))
            unit.Name = request.Name;
        if (request.Description is not null)
            unit.Description = request.Description;
        if (!string.IsNullOrWhiteSpace(request.UnitType))
            unit.UnitType = Enum.TryParse<UnitType>(request.UnitType, true, out var parsed) ? parsed : unit.UnitType;
        if (request.ParentUnitId.HasValue)
        {
            var parentValid = await dbContext.Units
                .AnyAsync(u => u.Id == request.ParentUnitId.Value && u.PropertyId == unit.PropertyId);
            if (!parentValid)
                return Results.BadRequest();

            unit.ParentUnitId = request.ParentUnitId;
        }

        unit.UpdatedAt = DateTime.UtcNow;
        await dbContext.SaveChangesAsync();

        var coverUrl = await GetCoverUrlAsync(dbContext, storageService, OwnerType.Unit, unit.Id, unit.CoverMediaId);

        var dto = new UnitDto(
            unit.Id,
            unit.PropertyId,
            unit.ParentUnitId,
            unit.Name,
            unit.Description,
            unit.UnitType.ToString().ToLowerInvariant(),
            dbContext.Units.Count(c => c.ParentUnitId == unit.Id),
            dbContext.Zaznamy.Count(z => z.UnitId == unit.Id),
            unit.CoverMediaId,
            coverUrl,
            unit.CreatedAt,
            unit.UpdatedAt);

        return Results.Ok(dto);
    }

    private static async Task<IResult> UpdateUnitCoverAsync(
        ClaimsPrincipal user,
        Guid id,
        [FromBody] CoverMediaRequest request,
        IStorageService storageService,
        ApplicationDbContext dbContext)
    {
        var userId = user.GetUserId();
        if (userId == Guid.Empty)
            return Results.Unauthorized();

        var unit = await dbContext.Units.FirstOrDefaultAsync(u => u.Id == id);
        if (unit is null)
            return Results.NotFound();

        if (!await HasPropertyAccessAsync(dbContext, unit.PropertyId, userId))
            return Results.Forbid();

        Media? coverMedia = null;
        if (request.CoverMediaId.HasValue)
        {
            coverMedia = await dbContext.Media.FirstOrDefaultAsync(m => m.Id == request.CoverMediaId.Value);
            if (coverMedia is null
                || coverMedia.OwnerType != OwnerType.Unit
                || coverMedia.OwnerId != unit.Id
                || coverMedia.Type != MediaType.Photo)
                return Results.BadRequest();

            unit.CoverMediaId = coverMedia.Id;
        }
        else
        {
            unit.CoverMediaId = null;
        }

        unit.UpdatedAt = DateTime.UtcNow;
        await dbContext.SaveChangesAsync();

        var coverUrl = coverMedia is null ? null : storageService.GetThumbnailUrl(coverMedia.StorageKey);

        var dto = new UnitDto(
            unit.Id,
            unit.PropertyId,
            unit.ParentUnitId,
            unit.Name,
            unit.Description,
            unit.UnitType.ToString().ToLowerInvariant(),
            dbContext.Units.Count(c => c.ParentUnitId == unit.Id),
            dbContext.Zaznamy.Count(z => z.UnitId == unit.Id),
            unit.CoverMediaId,
            coverUrl,
            unit.CreatedAt,
            unit.UpdatedAt);

        return Results.Ok(dto);
    }

    private static async Task<IResult> DeleteUnitAsync(
        ClaimsPrincipal user,
        Guid id,
        ApplicationDbContext dbContext)
    {
        var userId = user.GetUserId();
        if (userId == Guid.Empty)
            return Results.Unauthorized();

        var unit = await dbContext.Units.FirstOrDefaultAsync(u => u.Id == id);
        if (unit is null)
            return Results.NotFound();

        if (!await HasPropertyAccessAsync(dbContext, unit.PropertyId, userId))
            return Results.Forbid();

        unit.IsDeleted = true;
        unit.DeletedAt = DateTime.UtcNow;
        await dbContext.SaveChangesAsync();

        return Results.NoContent();
    }

    private static string? BuildCoverUrl(IStorageService storageService, string? storageKey)
    {
        if (string.IsNullOrWhiteSpace(storageKey))
            return null;

        return storageService.GetThumbnailUrl(storageKey);
    }

    private static string? GetCoverUrl(
        IStorageService storageService,
        IReadOnlyDictionary<Guid, string?> coverLookup,
        Guid? coverMediaId)
    {
        if (!coverMediaId.HasValue)
            return null;

        return coverLookup.TryGetValue(coverMediaId.Value, out var storageKey)
            ? BuildCoverUrl(storageService, storageKey)
            : null;
    }

    private static async Task<string?> GetCoverUrlAsync(
        ApplicationDbContext dbContext,
        IStorageService storageService,
        OwnerType ownerType,
        Guid ownerId,
        Guid? coverMediaId)
    {
        if (!coverMediaId.HasValue)
            return null;

        var storageKey = await dbContext.Media
            .Where(m => m.Id == coverMediaId.Value && m.OwnerType == ownerType && m.OwnerId == ownerId)
            .Select(m => m.StorageKey)
            .FirstOrDefaultAsync();

        return BuildCoverUrl(storageService, storageKey);
    }

    private static Task<bool> HasPropertyAccessAsync(ApplicationDbContext dbContext, Guid propertyId, Guid userId)
    {
        return dbContext.Properties.AnyAsync(p => p.Id == propertyId &&
            (dbContext.Projects.Any(pr => pr.Id == p.ProjectId && pr.OwnerId == userId)
             || dbContext.ProjectMembers.Any(pm => pm.ProjectId == p.ProjectId && pm.UserId == userId)
             || dbContext.PropertyMembers.Any(pm => pm.PropertyId == p.Id && pm.UserId == userId)));
    }
}

