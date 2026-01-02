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

public static class DocumentEndpoints
{
    public static IEndpointRouteBuilder MapDocumentEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("").RequireAuthorization();

        group.MapGet("/zaznamy/{zaznamId:guid}/documents", GetDocumentsAsync);
        group.MapPost("/zaznamy/{zaznamId:guid}/documents", AddDocumentAsync);
        group.MapGet("/documents/{id:guid}", GetDocumentAsync);
        group.MapPut("/documents/{id:guid}", UpdateDocumentAsync);
        group.MapDelete("/documents/{id:guid}", DeleteDocumentAsync);
        group.MapGet("/documents/{id:guid}/url", GetDocumentUrlAsync);

        return endpoints;
    }

    private static async Task<IResult> GetDocumentsAsync(
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

        var documents = await dbContext.ZaznamDokumenty
            .Where(d => d.ZaznamId == zaznamId)
            .OrderByDescending(d => d.CreatedAt)
            .ToListAsync();

        var dtos = documents.Select(d => ToDocumentDto(d, storageService)).ToList();
        return Results.Ok(dtos);
    }

    private static async Task<IResult> AddDocumentAsync(
        ClaimsPrincipal user,
        Guid zaznamId,
        [FromBody] AddDocumentRequest request,
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

        if (!TryParseDocumentType(request.Type, out var docType))
            return Results.BadRequest();

        var document = new ZaznamDokument
        {
            Id = Guid.NewGuid(),
            ZaznamId = zaznamId,
            Type = docType,
            StorageKey = request.StorageKey,
            OriginalFileName = request.OriginalFileName,
            MimeType = request.MimeType,
            SizeBytes = request.SizeBytes,
            Description = request.Description,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        dbContext.ZaznamDokumenty.Add(document);
        await dbContext.SaveChangesAsync();

        return Results.Created($"/documents/{document.Id}", ToDocumentDto(document, storageService));
    }

    private static async Task<IResult> GetDocumentAsync(
        ClaimsPrincipal user,
        Guid id,
        IStorageService storageService,
        ApplicationDbContext dbContext)
    {
        var userId = user.GetUserId();
        if (userId == Guid.Empty)
            return Results.Unauthorized();

        var document = await dbContext.ZaznamDokumenty.FirstOrDefaultAsync(d => d.Id == id);
        if (document is null)
            return Results.NotFound();

        if (!await HasZaznamAccessAsync(dbContext, document.ZaznamId, userId))
            return Results.Forbid();

        return Results.Ok(ToDocumentDto(document, storageService));
    }

    private static async Task<IResult> UpdateDocumentAsync(
        ClaimsPrincipal user,
        Guid id,
        [FromBody] UpdateDocumentRequest request,
        IStorageService storageService,
        ApplicationDbContext dbContext)
    {
        var userId = user.GetUserId();
        if (userId == Guid.Empty)
            return Results.Unauthorized();

        var document = await dbContext.ZaznamDokumenty.FirstOrDefaultAsync(d => d.Id == id);
        if (document is null)
            return Results.NotFound();

        if (!await HasZaznamAccessAsync(dbContext, document.ZaznamId, userId))
            return Results.Forbid();

        if (request.Description is not null)
            document.Description = request.Description;

        document.UpdatedAt = DateTime.UtcNow;
        await dbContext.SaveChangesAsync();

        return Results.Ok(ToDocumentDto(document, storageService));
    }

    private static async Task<IResult> DeleteDocumentAsync(
        ClaimsPrincipal user,
        Guid id,
        ApplicationDbContext dbContext)
    {
        var userId = user.GetUserId();
        if (userId == Guid.Empty)
            return Results.Unauthorized();

        var document = await dbContext.ZaznamDokumenty.FirstOrDefaultAsync(d => d.Id == id);
        if (document is null)
            return Results.NotFound();

        if (!await HasZaznamAccessAsync(dbContext, document.ZaznamId, userId))
            return Results.Forbid();

        document.IsDeleted = true;
        document.DeletedAt = DateTime.UtcNow;
        await dbContext.SaveChangesAsync();

        return Results.NoContent();
    }

    private static async Task<IResult> GetDocumentUrlAsync(
        ClaimsPrincipal user,
        Guid id,
        IStorageService storageService,
        IOptions<StorageOptions> storageOptions,
        ApplicationDbContext dbContext)
    {
        var userId = user.GetUserId();
        if (userId == Guid.Empty)
            return Results.Unauthorized();

        var document = await dbContext.ZaznamDokumenty.FirstOrDefaultAsync(d => d.Id == id);
        if (document is null)
            return Results.NotFound();

        if (!await HasZaznamAccessAsync(dbContext, document.ZaznamId, userId))
            return Results.Forbid();

        var expiresIn = GetPresignedExpiry(storageOptions.Value);
        var download = await storageService.GetDownloadUrlAsync(document.StorageKey, expiresIn);

        return Results.Ok(new DocumentUrlResponse(download.Url, download.ExpiresAt));
    }

    private static async Task<bool> HasZaznamAccessAsync(
        ApplicationDbContext dbContext,
        Guid zaznamId,
        Guid userId)
    {
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

    private static DocumentDto ToDocumentDto(
        ZaznamDokument document,
        IStorageService storageService)
    {
        var thumbnailUrl = document.Type == DocumentType.Photo
            ? storageService.GetThumbnailUrl(document.StorageKey)
            : null;

        return new DocumentDto(
            document.Id,
            document.ZaznamId,
            ToTypeString(document.Type),
            document.StorageKey,
            document.OriginalFileName,
            document.MimeType,
            document.SizeBytes,
            document.Description,
            thumbnailUrl,
            document.CreatedAt);
    }

    private static bool TryParseDocumentType(string? type, out DocumentType documentType)
    {
        documentType = DocumentType.Document;
        if (string.IsNullOrWhiteSpace(type))
            return false;

        return type.Trim().ToLowerInvariant() switch
        {
            "photo" => SetDocumentType(DocumentType.Photo, out documentType),
            "document" => SetDocumentType(DocumentType.Document, out documentType),
            "receipt" => SetDocumentType(DocumentType.Receipt, out documentType),
            _ => false
        };
    }

    private static bool SetDocumentType(DocumentType type, out DocumentType documentType)
    {
        documentType = type;
        return true;
    }

    private static string ToTypeString(DocumentType type)
    {
        return type switch
        {
            DocumentType.Photo => "photo",
            DocumentType.Document => "document",
            DocumentType.Receipt => "receipt",
            _ => "document"
        };
    }

    private static TimeSpan GetPresignedExpiry(StorageOptions options)
    {
        var minutes = options.PresignedExpiryMinutes;
        return minutes > 0 ? TimeSpan.FromMinutes(minutes) : TimeSpan.FromMinutes(60);
    }
}

