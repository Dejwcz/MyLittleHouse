using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using MujDomecek.API.Extensions;
using MujDomecek.Application.Abstractions;
using MujDomecek.API.Services;
using MujDomecek.Application.DTOs;
using MujDomecek.Domain.Aggregates.Zaznam;
using MujDomecek.Domain.ValueObjects;
using MujDomecek.Infrastructure.Persistence;

namespace MujDomecek.API.Endpoints;

public static class ZaznamEndpoints
{
    private const int DefaultPageSize = 20;
    private const int MaxPageSize = 100;

    public static IEndpointRouteBuilder MapZaznamEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/zaznamy").RequireAuthorization();

        group.MapGet("/", GetZaznamyAsync);
        group.MapPost("/", CreateZaznamAsync);
        group.MapGet("/{id:guid}", GetZaznamAsync);
        group.MapPut("/{id:guid}", UpdateZaznamAsync);
        group.MapDelete("/{id:guid}", DeleteZaznamAsync);
        group.MapPost("/{id:guid}/complete", CompleteZaznamAsync);
        group.MapGet("/drafts", GetDraftsAsync);

        return endpoints;
    }

    private static async Task<IResult> GetZaznamyAsync(
        ClaimsPrincipal user,
        Guid? propertyId,
        Guid? unitId,
        string? status,
        DateOnly? from,
        DateOnly? to,
        string[]? tags,
        string? search,
        int? page,
        int? pageSize,
        string? sort,
        string? order,
        IStorageService storageService,
        ApplicationDbContext dbContext)
    {
        var userId = user.GetUserId();
        if (userId == Guid.Empty)
            return Results.Unauthorized();

        var currentPage = Math.Max(page ?? 1, 1);
        var size = Math.Clamp(pageSize ?? DefaultPageSize, 1, MaxPageSize);

        var query = dbContext.Zaznamy.AsQueryable();

        query = query.Where(z => dbContext.Properties.Any(p => p.Id == z.PropertyId &&
            (dbContext.Projects.Any(pr => pr.Id == p.ProjectId && pr.OwnerId == userId)
             || dbContext.ProjectMembers.Any(pm => pm.ProjectId == p.ProjectId && pm.UserId == userId)
             || dbContext.PropertyMembers.Any(pm => pm.PropertyId == p.Id && pm.UserId == userId))));

        if (propertyId.HasValue)
            query = query.Where(z => z.PropertyId == propertyId.Value);
        if (unitId.HasValue)
            query = query.Where(z => z.UnitId == unitId.Value);
        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(z => z.Status == ParseStatus(status));
        if (from.HasValue)
            query = query.Where(z => z.Date >= from.Value);
        if (to.HasValue)
            query = query.Where(z => z.Date <= to.Value);

        var normalizedTags = NormalizeTagNames(tags);
        if (normalizedTags.Count > 0)
        {
            var tagResolution = await ResolveTagIdsAsync(dbContext, normalizedTags);
            if (tagResolution.TagIds.Count == 0)
                return Results.Ok(new ZaznamListResponse(Array.Empty<ZaznamDto>(), 0, currentPage, size));

            query = query.Where(z => dbContext.ZaznamTags
                .Any(t => t.ZaznamId == z.Id && tagResolution.TagIds.Contains(t.TagId)));
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLowerInvariant();
            query = query.Where(z =>
                (z.Title ?? string.Empty).ToLower().Contains(term)
                || (z.Description ?? string.Empty).ToLower().Contains(term));
        }

        query = ApplySort(query, sort, order);

        var total = await query.CountAsync();
        if (total == 0)
            return Results.Ok(new ZaznamListResponse(Array.Empty<ZaznamDto>(), 0, currentPage, size));

        var pageItems = await query
            .Skip((currentPage - 1) * size)
            .Take(size)
            .Select(z => new
            {
                z.Id,
                z.PropertyId,
                z.UnitId,
                z.Title,
                z.Description,
                z.Date,
                z.Cost,
                z.Status,
                z.Flags,
                z.CreatedAt,
                z.UpdatedAt
            })
            .ToListAsync();

        var zaznamIds = pageItems.Select(i => i.Id).ToList();
        var propertyIds = pageItems.Select(i => i.PropertyId).Distinct().ToList();
        var unitIds = pageItems.Where(i => i.UnitId.HasValue).Select(i => i.UnitId!.Value).Distinct().ToList();

        var propertyNames = await dbContext.Properties
            .Where(p => propertyIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, p => p.Name);

        var unitNames = unitIds.Count == 0
            ? []
            : await dbContext.Units
                .Where(u => unitIds.Contains(u.Id))
                .ToDictionaryAsync(u => u.Id, u => u.Name ?? string.Empty);

        var tagRows = await dbContext.ZaznamTags
            .Where(t => zaznamIds.Contains(t.ZaznamId))
            .Join(dbContext.Tags, t => t.TagId, tag => tag.Id, (t, tag) => new { t.ZaznamId, tag.Name })
            .ToListAsync();

        var tagsLookup = tagRows
            .GroupBy(r => r.ZaznamId)
            .ToDictionary(g => g.Key, g => g.Select(r => r.Name).ToList());

        var documentCounts = await dbContext.ZaznamDokumenty
            .Where(d => zaznamIds.Contains(d.ZaznamId))
            .GroupBy(d => d.ZaznamId)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Key, x => x.Count);

        var commentCounts = await dbContext.Comments
            .Where(c => zaznamIds.Contains(c.ZaznamId))
            .GroupBy(c => c.ZaznamId)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Key, x => x.Count);

        var thumbnailLookup = await dbContext.ZaznamDokumenty
            .Where(d => zaznamIds.Contains(d.ZaznamId) && d.Type == DocumentType.Photo)
            .GroupBy(d => d.ZaznamId)
            .Select(g => new
            {
                g.Key,
                StorageKey = g.OrderBy(d => d.CreatedAt).Select(d => d.StorageKey).FirstOrDefault()
            })
            .ToDictionaryAsync(x => x.Key, x => x.StorageKey);

        var createdByLookup = await dbContext.Activities
            .Where(a => a.Type == ActivityType.ZaznamCreated && a.TargetId.HasValue && zaznamIds.Contains(a.TargetId.Value))
            .GroupBy(a => a.TargetId!.Value)
            .Select(g => new { ZaznamId = g.Key, UserId = g.OrderBy(a => a.CreatedAt).Select(a => a.ActorUserId).FirstOrDefault() })
            .ToDictionaryAsync(x => x.ZaznamId, x => x.UserId);

        var createdByIds = createdByLookup.Values
            .Where(id => id != Guid.Empty)
            .Distinct()
            .ToList();
        var createdByUsers = createdByIds.Count == 0
            ? []
            : await dbContext.Users
                .Where(u => createdByIds.Contains(u.Id))
                .ToDictionaryAsync(
                    u => u.Id,
                    u => (First: u.FirstName ?? string.Empty, Last: u.LastName ?? string.Empty, Email: u.Email));

        var items = pageItems.Select(item =>
        {
            var createdById = createdByLookup.TryGetValue(item.Id, out var ownerId) && ownerId != Guid.Empty
                ? ownerId
                : userId;
            createdByUsers.TryGetValue(createdById, out var createdByUser);

            tagsLookup.TryGetValue(item.Id, out var tagNames);
            documentCounts.TryGetValue(item.Id, out var documentCount);
            commentCounts.TryGetValue(item.Id, out var commentCount);
            thumbnailLookup.TryGetValue(item.Id, out var thumbnailKey);
            unitNames.TryGetValue(item.UnitId ?? Guid.Empty, out var unitName);

            return new ZaznamDto(
                item.Id,
                item.PropertyId,
                propertyNames.TryGetValue(item.PropertyId, out var propertyName) ? propertyName : string.Empty,
                item.UnitId,
                unitName,
                item.Title,
                item.Description,
                item.Date,
                item.Cost,
                item.Status.ToString().ToLowerInvariant(),
                FlagsToStrings(item.Flags),
                tagNames ?? [],
                documentCount,
                commentCount,
                BuildThumbnailUrl(storageService, thumbnailKey),
                "synced",
                item.CreatedAt,
                item.UpdatedAt,
                new SimpleUserDto(createdById, BuildDisplayName(createdByUser.First, createdByUser.Last, createdByUser.Email)));
        }).ToList();

        return Results.Ok(new ZaznamListResponse(items, total, currentPage, size));
    }

    private static async Task<IResult> GetDraftsAsync(
        ClaimsPrincipal user,
        Guid? propertyId,
        Guid? unitId,
        string[]? tags,
        string? search,
        int? page,
        int? pageSize,
        string? sort,
        string? order,
        IStorageService storageService,
        ApplicationDbContext dbContext)
    {
        return await GetZaznamyAsync(
            user,
            propertyId,
            unitId,
            "draft",
            null,
            null,
            tags,
            search,
            page,
            pageSize,
            sort,
            order,
            storageService,
            dbContext);
    }

    private static async Task<IResult> CreateZaznamAsync(
        ClaimsPrincipal user,
        [FromBody] CreateZaznamRequest request,
        IStorageService storageService,
        ApplicationDbContext dbContext)
    {
        var userId = user.GetUserId();
        if (userId == Guid.Empty)
            return Results.Unauthorized();

        if (!await HasPropertyAccessAsync(dbContext, request.PropertyId, userId))
            return Results.Forbid();

        var parsedStatus = ParseStatus(request.Status);
        if (parsedStatus == ZaznamStatus.Complete && string.IsNullOrWhiteSpace(request.Title))
            return Results.BadRequest();

        if (request.UnitId.HasValue)
        {
            var unitValid = await dbContext.Units
                .AnyAsync(u => u.Id == request.UnitId.Value && u.PropertyId == request.PropertyId);
            if (!unitValid)
                return Results.BadRequest();
        }

        var normalizedTags = NormalizeTagNames(request.Tags);
        var tagResolution = normalizedTags.Count == 0
            ? new TagResolution(new List<short>(), new List<string>())
            : await ResolveTagIdsAsync(dbContext, normalizedTags);

        if (tagResolution.MissingTags.Count > 0)
            return Results.BadRequest(new { missingTags = tagResolution.MissingTags });

        var zaznam = new Zaznam
        {
            Id = Guid.NewGuid(),
            PropertyId = request.PropertyId,
            UnitId = request.UnitId,
            Title = request.Title,
            Description = request.Description,
            Date = request.Date ?? DateOnly.FromDateTime(DateTime.UtcNow),
            Cost = request.Cost,
            Status = parsedStatus,
            Flags = ParseFlags(request.Flags),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        if (tagResolution.TagIds.Count > 0)
        {
            zaznam.Tags = tagResolution.TagIds
                .Select(tagId => new ZaznamTag { ZaznamId = zaznam.Id, TagId = tagId })
                .ToList();
        }

        dbContext.Zaznamy.Add(zaznam);
        ActivityNotificationHelper.AddActivity(
            dbContext,
            zaznam.PropertyId,
            userId,
            ActivityType.ZaznamCreated,
            "zaznam",
            zaznam.Id,
            new { title = zaznam.Title });
        await dbContext.SaveChangesAsync();

        return Results.Created($"/zaznamy/{zaznam.Id}", await ToDtoAsync(dbContext, zaznam, userId, storageService));
    }

    private static async Task<IResult> GetZaznamAsync(
        ClaimsPrincipal user,
        Guid id,
        IStorageService storageService,
        ApplicationDbContext dbContext)
    {
        var userId = user.GetUserId();
        if (userId == Guid.Empty)
            return Results.Unauthorized();

        var zaznam = await dbContext.Zaznamy.FirstOrDefaultAsync(z => z.Id == id);
        if (zaznam is null)
            return Results.NotFound();

        if (!await HasZaznamAccessAsync(dbContext, zaznam.Id, userId))
            return Results.Forbid();

        var dto = await ToDtoAsync(dbContext, zaznam, userId, storageService);
        var documents = await dbContext.ZaznamDokumenty
            .Where(d => d.ZaznamId == zaznam.Id)
            .OrderByDescending(d => d.CreatedAt)
            .ToListAsync();
        var documentDtos = documents.Select(ToDocumentDto).ToList();

        var comments = await LoadCommentDtosAsync(dbContext, zaznam.Id);
        var detail = new ZaznamDetailDto(
            dto.Id,
            dto.PropertyId,
            dto.PropertyName,
            dto.UnitId,
            dto.UnitName,
            dto.Title,
            dto.Description,
            dto.Date,
            dto.Cost,
            dto.Status,
            dto.Flags,
            dto.Tags,
            dto.DocumentCount,
            dto.CommentCount,
            dto.ThumbnailUrl,
            dto.SyncStatus,
            dto.CreatedAt,
            dto.UpdatedAt,
            dto.CreatedBy,
            documentDtos,
            comments,
            Array.Empty<AuditLogEntryDto>());

        return Results.Ok(detail);
    }

    private static async Task<IResult> UpdateZaznamAsync(
        ClaimsPrincipal user,
        Guid id,
        [FromBody] UpdateZaznamRequest request,
        IStorageService storageService,
        ApplicationDbContext dbContext)
    {
        var userId = user.GetUserId();
        if (userId == Guid.Empty)
            return Results.Unauthorized();

        var zaznam = await dbContext.Zaznamy.FirstOrDefaultAsync(z => z.Id == id);
        if (zaznam is null)
            return Results.NotFound();

        if (!await HasZaznamAccessAsync(dbContext, zaznam.Id, userId))
            return Results.Forbid();

        if (request.UnitId.HasValue)
        {
            var unitValid = await dbContext.Units
                .AnyAsync(u => u.Id == request.UnitId.Value && u.PropertyId == zaznam.PropertyId);
            if (!unitValid)
                return Results.BadRequest();

            zaznam.UnitId = request.UnitId;
        }
        if (request.Title is not null)
            zaznam.Title = request.Title;
        if (request.Description is not null)
            zaznam.Description = request.Description;
        if (request.Date.HasValue)
            zaznam.Date = request.Date.Value;
        if (request.Cost.HasValue)
            zaznam.Cost = request.Cost.Value;
        if (request.Flags is not null)
            zaznam.Flags = ParseFlags(request.Flags);
        if (request.Tags is not null)
        {
            var normalizedTags = NormalizeTagNames(request.Tags);
            var tagResolution = normalizedTags.Count == 0
                ? new TagResolution(new List<short>(), new List<string>())
                : await ResolveTagIdsAsync(dbContext, normalizedTags);

            if (tagResolution.MissingTags.Count > 0)
                return Results.BadRequest(new { missingTags = tagResolution.MissingTags });

            var existingTags = await dbContext.ZaznamTags
                .Where(t => t.ZaznamId == zaznam.Id)
                .ToListAsync();

            var desiredIds = tagResolution.TagIds.ToHashSet();
            var existingIds = existingTags.Select(t => t.TagId).ToHashSet();

            foreach (var tag in existingTags.Where(t => !desiredIds.Contains(t.TagId)))
                dbContext.ZaznamTags.Remove(tag);

            foreach (var tagId in desiredIds.Except(existingIds))
            {
                dbContext.ZaznamTags.Add(new ZaznamTag
                {
                    ZaznamId = zaznam.Id,
                    TagId = tagId
                });
            }
        }

        zaznam.UpdatedAt = DateTime.UtcNow;
        ActivityNotificationHelper.AddActivity(
            dbContext,
            zaznam.PropertyId,
            userId,
            ActivityType.ZaznamUpdated,
            "zaznam",
            zaznam.Id,
            new { title = zaznam.Title });
        await dbContext.SaveChangesAsync();

        return Results.Ok(await ToDtoAsync(dbContext, zaznam, userId, storageService));
    }

    private static async Task<IResult> DeleteZaznamAsync(
        ClaimsPrincipal user,
        Guid id,
        ApplicationDbContext dbContext)
    {
        var userId = user.GetUserId();
        if (userId == Guid.Empty)
            return Results.Unauthorized();

        var zaznam = await dbContext.Zaznamy.FirstOrDefaultAsync(z => z.Id == id);
        if (zaznam is null)
            return Results.NotFound();

        if (!await HasZaznamAccessAsync(dbContext, zaznam.Id, userId))
            return Results.Forbid();

        zaznam.IsDeleted = true;
        zaznam.DeletedAt = DateTime.UtcNow;
        ActivityNotificationHelper.AddActivity(
            dbContext,
            zaznam.PropertyId,
            userId,
            ActivityType.ZaznamDeleted,
            "zaznam",
            zaznam.Id,
            new { title = zaznam.Title });
        await dbContext.SaveChangesAsync();

        return Results.NoContent();
    }

    private static async Task<IResult> CompleteZaznamAsync(
        ClaimsPrincipal user,
        Guid id,
        IStorageService storageService,
        ApplicationDbContext dbContext)
    {
        var userId = user.GetUserId();
        if (userId == Guid.Empty)
            return Results.Unauthorized();

        var zaznam = await dbContext.Zaznamy.FirstOrDefaultAsync(z => z.Id == id);
        if (zaznam is null)
            return Results.NotFound();

        if (!await HasZaznamAccessAsync(dbContext, zaznam.Id, userId))
            return Results.Forbid();

        if (string.IsNullOrWhiteSpace(zaznam.Title))
            return Results.BadRequest();

        zaznam.Status = ZaznamStatus.Complete;
        zaznam.UpdatedAt = DateTime.UtcNow;
        ActivityNotificationHelper.AddActivity(
            dbContext,
            zaznam.PropertyId,
            userId,
            ActivityType.ZaznamUpdated,
            "zaznam",
            zaznam.Id,
            new { title = zaznam.Title });
        await dbContext.SaveChangesAsync();

        return Results.Ok(await ToDtoAsync(dbContext, zaznam, userId, storageService));
    }

    private static async Task<bool> HasZaznamAccessAsync(ApplicationDbContext dbContext, Guid zaznamId, Guid userId)
    {
        var propertyId = await dbContext.Zaznamy
            .Where(z => z.Id == zaznamId)
            .Select(z => z.PropertyId)
            .FirstOrDefaultAsync();

        if (propertyId == Guid.Empty)
            return false;

        return await HasPropertyAccessAsync(dbContext, propertyId, userId);
    }

    private static Task<bool> HasPropertyAccessAsync(ApplicationDbContext dbContext, Guid propertyId, Guid userId)
    {
        return dbContext.Properties.AnyAsync(p => p.Id == propertyId &&
            (dbContext.Projects.Any(pr => pr.Id == p.ProjectId && pr.OwnerId == userId)
             || dbContext.ProjectMembers.Any(pm => pm.ProjectId == p.ProjectId && pm.UserId == userId)
             || dbContext.PropertyMembers.Any(pm => pm.PropertyId == p.Id && pm.UserId == userId)));
    }

    private static async Task<IReadOnlyList<CommentDto>> LoadCommentDtosAsync(
        ApplicationDbContext dbContext,
        Guid zaznamId)
    {
        var comments = await dbContext.Comments
            .Where(c => c.ZaznamId == zaznamId)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();

        if (comments.Count == 0)
            return Array.Empty<CommentDto>();

        var commentIds = comments.Select(c => c.Id).ToList();
        var mentionLookup = await CommentMentionHelper.LoadMentionLookupAsync(dbContext, commentIds);

        var authorIds = comments.Select(c => c.AuthorUserId).Distinct().ToList();
        var authors = await dbContext.Users
            .Where(u => authorIds.Contains(u.Id))
            .Select(u => new { u.Id, u.FirstName, u.LastName, u.Email })
            .ToListAsync();

        var authorLookup = authors.ToDictionary(a => a.Id, a => a);

        return comments.Select(comment =>
        {
            authorLookup.TryGetValue(comment.AuthorUserId, out var author);
            mentionLookup.TryGetValue(comment.Id, out var mentions);

            return new CommentDto(
                comment.Id,
                comment.ZaznamId,
                comment.Content,
                mentions ?? [],
                new CommentAuthorDto(
                    comment.AuthorUserId,
                    BuildDisplayName(author?.FirstName, author?.LastName, author?.Email),
                    null),
                comment.CreatedAt,
                comment.UpdatedAt,
                comment.CreatedAt != comment.UpdatedAt);
        }).ToList();
    }

    private static async Task<ZaznamDto> ToDtoAsync(
        ApplicationDbContext dbContext,
        Zaznam zaznam,
        Guid userId,
        IStorageService storageService)
    {
        var propertyName = await dbContext.Properties.Where(p => p.Id == zaznam.PropertyId).Select(p => p.Name).FirstOrDefaultAsync() ?? string.Empty;
        var unitName = await dbContext.Units.Where(u => u.Id == zaznam.UnitId).Select(u => u.Name).FirstOrDefaultAsync();
        var tags = await dbContext.ZaznamTags.Where(t => t.ZaznamId == zaznam.Id)
            .Join(dbContext.Tags, t => t.TagId, tag => tag.Id, (t, tag) => tag.Name)
            .ToListAsync();
        var documentCount = await dbContext.ZaznamDokumenty.CountAsync(d => d.ZaznamId == zaznam.Id);
        var commentCount = await dbContext.Comments.CountAsync(c => c.ZaznamId == zaznam.Id);
        var thumbnailKey = await dbContext.ZaznamDokumenty
            .Where(d => d.ZaznamId == zaznam.Id && d.Type == DocumentType.Photo)
            .OrderBy(d => d.CreatedAt)
            .Select(d => d.StorageKey)
            .FirstOrDefaultAsync();

        var createdById = await dbContext.Activities
            .Where(a => a.Type == ActivityType.ZaznamCreated && a.TargetId == zaznam.Id)
            .OrderBy(a => a.CreatedAt)
            .Select(a => (Guid?)a.ActorUserId)
            .FirstOrDefaultAsync();

        var createdByUserId = createdById.HasValue && createdById.Value != Guid.Empty
            ? createdById.Value
            : userId;

        string? createdByName = null;
        if (createdById.HasValue && createdById.Value != Guid.Empty)
        {
            var createdByUser = await dbContext.Users
                .Where(u => u.Id == createdByUserId)
                .Select(u => new { u.FirstName, u.LastName, u.Email })
                .FirstOrDefaultAsync();
            createdByName = BuildDisplayName(createdByUser?.FirstName, createdByUser?.LastName, createdByUser?.Email);
        }

        return new ZaznamDto(
            zaznam.Id,
            zaznam.PropertyId,
            propertyName,
            zaznam.UnitId,
            unitName,
            zaznam.Title,
            zaznam.Description,
            zaznam.Date,
            zaznam.Cost,
            zaznam.Status.ToString().ToLowerInvariant(),
            FlagsToStrings(zaznam.Flags),
            tags,
            documentCount,
            commentCount,
            BuildThumbnailUrl(storageService, thumbnailKey),
            "synced",
            zaznam.CreatedAt,
            zaznam.UpdatedAt,
            new SimpleUserDto(createdByUserId, createdByName ?? string.Empty));
    }

    private static DocumentDto ToDocumentDto(ZaznamDokument document)
    {
        return new DocumentDto(
            document.Id,
            document.ZaznamId,
            ToDocumentTypeString(document.Type),
            document.StorageKey,
            document.OriginalFileName,
            document.MimeType,
            document.SizeBytes,
            document.Description,
            null,
            document.CreatedAt);
    }

    private static string ToDocumentTypeString(DocumentType type)
    {
        return type switch
        {
            DocumentType.Photo => "photo",
            DocumentType.Document => "document",
            DocumentType.Receipt => "receipt",
            _ => "document"
        };
    }

    private static List<string> NormalizeTagNames(IReadOnlyList<string>? tags)
    {
        if (tags is null || tags.Count == 0)
            return [];

        return tags
            .Select(tag => tag?.Trim())
            .Where(tag => !string.IsNullOrWhiteSpace(tag))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Select(tag => tag!)
            .ToList();
    }

    private static async Task<TagResolution> ResolveTagIdsAsync(
        ApplicationDbContext dbContext,
        IReadOnlyList<string> tagNames)
    {
        if (tagNames.Count == 0)
            return new TagResolution(new List<short>(), new List<string>());

        var normalized = tagNames
            .Select(name => name.Trim())
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (normalized.Count == 0)
            return new TagResolution(new List<short>(), new List<string>());

        var normalizedLower = normalized.Select(name => name.ToLowerInvariant()).ToList();
        var tags = await dbContext.Tags
            .Where(t => t.IsActive && normalizedLower.Contains(t.Name.ToLower()))
            .ToListAsync();

        var tagIds = tags.Select(t => t.Id).Distinct().ToList();
        var missing = normalized
            .Where(name => tags.All(t => !string.Equals(t.Name, name, StringComparison.OrdinalIgnoreCase)))
            .ToList();

        return new TagResolution(tagIds, missing);
    }

    private static IQueryable<Zaznam> ApplySort(
        IQueryable<Zaznam> query,
        string? sort,
        string? order)
    {
        var descending = !string.Equals(order, "asc", StringComparison.OrdinalIgnoreCase);
        var key = sort?.Trim().ToLowerInvariant();

        return key switch
        {
            "createdat" => descending
                ? query.OrderByDescending(z => z.CreatedAt)
                : query.OrderBy(z => z.CreatedAt),
            "cost" => descending
                ? query.OrderByDescending(z => z.Cost)
                : query.OrderBy(z => z.Cost),
            _ => descending
                ? query.OrderByDescending(z => z.Date)
                : query.OrderBy(z => z.Date)
        };
    }

    private static string? BuildThumbnailUrl(IStorageService storageService, string? storageKey)
    {
        if (string.IsNullOrWhiteSpace(storageKey))
            return null;

        return storageService.GetThumbnailUrl(storageKey);
    }

    private static string BuildDisplayName(string? firstName, string? lastName, string? email)
    {
        var name = $"{firstName} {lastName}".Trim();
        return string.IsNullOrWhiteSpace(name) ? email ?? string.Empty : name;
    }

    private static ZaznamStatus ParseStatus(string? status)
    {
        return Enum.TryParse<ZaznamStatus>(status ?? string.Empty, true, out var parsed)
            ? parsed
            : ZaznamStatus.Complete;
    }

    private static IReadOnlyList<string> FlagsToStrings(ZaznamFlags flags)
    {
        if (flags == ZaznamFlags.None)
            return Array.Empty<string>();

        var values = Enum.GetValues<ZaznamFlags>()
            .Where(f => f != ZaznamFlags.None && flags.HasFlag(f))
            .Select(f => f.ToString().ToLowerInvariant())
            .ToList();

        return values;
    }

    private static ZaznamFlags ParseFlags(IReadOnlyList<string>? flags)
    {
        if (flags is null || flags.Count == 0)
            return ZaznamFlags.None;

        var result = ZaznamFlags.None;
        foreach (var flag in flags)
        {
            if (Enum.TryParse<ZaznamFlags>(flag, true, out var parsed))
                result |= parsed;
        }

        return result;
    }

    private sealed record TagResolution(IReadOnlyList<short> TagIds, IReadOnlyList<string> MissingTags);
}

