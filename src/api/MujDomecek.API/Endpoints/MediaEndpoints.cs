using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using MujDomecek.API.Extensions;
using MujDomecek.Application.Abstractions;
using MujDomecek.Application.DTOs;
using MujDomecek.Domain.Aggregates.Zaznam;
using MujDomecek.Domain.ValueObjects;
using MujDomecek.Infrastructure.Persistence;
using MujDomecek.Infrastructure.Options;

namespace MujDomecek.API.Endpoints;

public static class MediaEndpoints
{
    public static IEndpointRouteBuilder MapMediaEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("").RequireAuthorization();

        group.MapGet("/zaznamy/{zaznamId:guid}/media", GetMediaListAsync);
        group.MapPost("/zaznamy/{zaznamId:guid}/media", AddMediaAsync);
        group.MapGet("/media/{id:guid}", GetMediaAsync);
        group.MapPut("/media/{id:guid}", UpdateMediaAsync);
        group.MapDelete("/media/{id:guid}", DeleteMediaAsync);
        group.MapGet("/media/{id:guid}/url", GetMediaUrlAsync);

        return endpoints;
    }

    private static async Task<IResult> GetMediaListAsync(
        ClaimsPrincipal user,
        Guid zaznamId,
        IStorageService storageService,
        ApplicationDbContext dbContext)
    {
        var userId = user.GetUserId();
        if (userId == Guid.Empty)
            return Results.Unauthorized();

        if (!await HasZaznamAccessAsync(dbContext, zaznamId, userId))
            return Results.Forbid();

        var documents = await dbContext.Media
            .Where(d => d.OwnerType == OwnerType.Zaznam && d.OwnerId == zaznamId)
            .OrderByDescending(d => d.CreatedAt)
            .ToListAsync();

        var dtos = documents.Select(d => ToMediaDto(d, storageService)).ToList();
        return Results.Ok(dtos);
    }

    private static async Task<IResult> AddMediaAsync(
        ClaimsPrincipal user,
        Guid zaznamId,
        [FromBody] AddMediaRequest request,
        IStorageService storageService,
        ApplicationDbContext dbContext)
    {
        var userId = user.GetUserId();
        if (userId == Guid.Empty)
            return Results.Unauthorized();

        if (!await HasZaznamAccessAsync(dbContext, zaznamId, userId))
            return Results.Forbid();

        if (string.IsNullOrWhiteSpace(request.StorageKey))
            return Results.BadRequest();

        if (!TryParseMediaType(request.Type, out var mediaType))
            return Results.BadRequest();

        var document = new Media
        {
            Id = Guid.NewGuid(),
            OwnerType = OwnerType.Zaznam,
            OwnerId = zaznamId,
            Type = mediaType,
            StorageKey = request.StorageKey,
            OriginalFileName = request.OriginalFileName,
            MimeType = request.MimeType,
            SizeBytes = request.SizeBytes,
            Description = request.Description,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        dbContext.Media.Add(document);
        await dbContext.SaveChangesAsync();

        return Results.Created($"/media/{document.Id}", ToMediaDto(document, storageService));
    }

    private static async Task<IResult> GetMediaAsync(
        ClaimsPrincipal user,
        Guid id,
        IStorageService storageService,
        ApplicationDbContext dbContext)
    {
        var userId = user.GetUserId();
        if (userId == Guid.Empty)
            return Results.Unauthorized();

        var document = await dbContext.Media.FirstOrDefaultAsync(d => d.Id == id);
        if (document is null)
            return Results.NotFound();

        if (document.OwnerType != OwnerType.Zaznam
            || !await HasZaznamAccessAsync(dbContext, document.OwnerId, userId))
            return Results.Forbid();

        return Results.Ok(ToMediaDto(document, storageService));
    }

    private static async Task<IResult> UpdateMediaAsync(
        ClaimsPrincipal user,
        Guid id,
        [FromBody] UpdateMediaRequest request,
        IStorageService storageService,
        ApplicationDbContext dbContext)
    {
        var userId = user.GetUserId();
        if (userId == Guid.Empty)
            return Results.Unauthorized();

        var document = await dbContext.Media.FirstOrDefaultAsync(d => d.Id == id);
        if (document is null)
            return Results.NotFound();

        if (document.OwnerType != OwnerType.Zaznam
            || !await HasZaznamAccessAsync(dbContext, document.OwnerId, userId))
            return Results.Forbid();

        if (request.Description is not null)
            document.Description = request.Description;

        document.UpdatedAt = DateTime.UtcNow;
        await dbContext.SaveChangesAsync();

        return Results.Ok(ToMediaDto(document, storageService));
    }

    private static async Task<IResult> DeleteMediaAsync(
        ClaimsPrincipal user,
        Guid id,
        ApplicationDbContext dbContext)
    {
        var userId = user.GetUserId();
        if (userId == Guid.Empty)
            return Results.Unauthorized();

        var document = await dbContext.Media.FirstOrDefaultAsync(d => d.Id == id);
        if (document is null)
            return Results.NotFound();

        if (document.OwnerType != OwnerType.Zaznam
            || !await HasZaznamAccessAsync(dbContext, document.OwnerId, userId))
            return Results.Forbid();

        document.IsDeleted = true;
        document.DeletedAt = DateTime.UtcNow;
        await dbContext.SaveChangesAsync();

        return Results.NoContent();
    }

    private static async Task<IResult> GetMediaUrlAsync(
        ClaimsPrincipal user,
        Guid id,
        IStorageService storageService,
        IOptions<StorageOptions> storageOptions,
        ApplicationDbContext dbContext)
    {
        var userId = user.GetUserId();
        if (userId == Guid.Empty)
            return Results.Unauthorized();

        var document = await dbContext.Media.FirstOrDefaultAsync(d => d.Id == id);
        if (document is null)
            return Results.NotFound();

        if (document.OwnerType != OwnerType.Zaznam
            || !await HasZaznamAccessAsync(dbContext, document.OwnerId, userId))
            return Results.Forbid();

        var expiresIn = GetPresignedExpiry(storageOptions.Value);
        var download = await storageService.GetDownloadUrlAsync(document.StorageKey, expiresIn);

        return Results.Ok(new MediaUrlResponse(download.Url, download.ExpiresAt));
    }

    private static async Task<bool> HasZaznamAccessAsync(
        ApplicationDbContext dbContext,
        Guid zaznamId,
        Guid userId)
    {
        if (await dbContext.ZaznamMembers.AnyAsync(m => m.ZaznamId == zaznamId && m.UserId == userId))
            return true;

        var propertyId = await dbContext.Zaznamy
            .Where(z => z.Id == zaznamId)
            .Select(z => z.PropertyId)
            .FirstOrDefaultAsync();

        if (propertyId == Guid.Empty)
            return false;

        return await dbContext.Properties.AnyAsync(p => p.Id == propertyId &&
            (dbContext.Projects.Any(pr => pr.Id == p.ProjectId && pr.OwnerId == userId)
             || dbContext.ProjectMembers.Any(pm => pm.ProjectId == p.ProjectId && pm.UserId == userId)
             || dbContext.PropertyMembers.Any(pm => pm.PropertyId == p.Id && pm.UserId == userId)));
    }

    private static MediaDto ToMediaDto(
        Media document,
        IStorageService storageService)
    {
        var thumbnailUrl = document.Type == MediaType.Photo
            ? storageService.GetThumbnailUrl(document.StorageKey)
            : null;

        return new MediaDto(
            document.Id,
            document.OwnerId,
            ToMediaTypeString(document.Type),
            document.StorageKey,
            document.OriginalFileName,
            document.MimeType,
            document.SizeBytes,
            document.Description,
            thumbnailUrl,
            document.CreatedAt);
    }

    private static bool TryParseMediaType(string? type, out MediaType mediaType)
    {
        mediaType = MediaType.Document;
        if (string.IsNullOrWhiteSpace(type))
            return false;

        return type.Trim().ToLowerInvariant() switch
        {
            "photo" => SetMediaType(MediaType.Photo, out mediaType),
            "document" => SetMediaType(MediaType.Document, out mediaType),
            "receipt" => SetMediaType(MediaType.Receipt, out mediaType),
            _ => false
        };
    }

    private static bool SetMediaType(MediaType type, out MediaType mediaType)
    {
        mediaType = type;
        return true;
    }

    private static string ToMediaTypeString(MediaType type)
    {
        return type switch
        {
            MediaType.Photo => "photo",
            MediaType.Document => "document",
            MediaType.Receipt => "receipt",
            _ => "document"
        };
    }

    private static TimeSpan GetPresignedExpiry(StorageOptions options)
    {
        var minutes = options.PresignedExpiryMinutes;
        return minutes > 0 ? TimeSpan.FromMinutes(minutes) : TimeSpan.FromMinutes(60);
    }
}

