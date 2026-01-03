using System.Security.Claims;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using MujDomecek.API.Extensions;
using MujDomecek.Application.DTOs;
using MujDomecek.Domain.Aggregates.Project;
using MujDomecek.Domain.Aggregates.Property;
using MujDomecek.Domain.Aggregates.Sync;
using MujDomecek.Domain.Aggregates.Zaznam;
using MujDomecek.Domain.ValueObjects;
using MujDomecek.Infrastructure.Persistence;

namespace MujDomecek.API.Endpoints;

public static class SyncEndpoints
{
    private const int MaxPullChanges = 200;

    public static IEndpointRouteBuilder MapSyncEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/sync").RequireAuthorization();

        group.MapGet("/status", GetStatusAsync);
        group.MapPost("/push", PushAsync);
        group.MapGet("/pull", PullAsync);

        return endpoints;
    }

    private static async Task<IResult> GetStatusAsync(
        ClaimsPrincipal user,
        string? scopeType,
        Guid? scopeId,
        ApplicationDbContext dbContext)
    {
        var userId = user.GetUserId();
        if (userId == Guid.Empty)
            return Results.Unauthorized();

        if (!TryNormalizeScopeType(scopeType, out var normalizedScope) || scopeId is null || scopeId == Guid.Empty)
            return Results.BadRequest();

        if (!await HasScopeAccessAsync(dbContext, normalizedScope, scopeId.Value, userId))
            return Results.Forbid();

        var response = new SyncStatusResponse(null, 0, DateTime.UtcNow);
        return Results.Ok(response);
    }

    private static async Task<IResult> PushAsync(
        ClaimsPrincipal user,
        [FromBody] SyncPushRequest request,
        ApplicationDbContext dbContext)
    {
        var userId = user.GetUserId();
        if (userId == Guid.Empty)
            return Results.Unauthorized();

        if (!TryNormalizeScopeType(request.ScopeType, out var scopeType) || request.ScopeId == Guid.Empty)
            return Results.BadRequest();

        if (!await HasScopeAccessAsync(dbContext, scopeType, request.ScopeId, userId))
            return Results.Forbid();

        if (request.Changes is null || request.Changes.Count == 0)
            return Results.BadRequest();

        var accepted = new List<string>();
        var rejected = new List<SyncRejectedChange>();
        var conflicts = new List<SyncConflict>();

        foreach (var change in request.Changes)
        {
            if (!TryNormalizeEntityType(change.EntityType, out var entityType))
            {
                rejected.Add(new SyncRejectedChange(change.Id, "invalid entityType"));
                continue;
            }

            if (!TryNormalizeOperation(change.Operation, out var operation))
            {
                rejected.Add(new SyncRejectedChange(change.Id, "invalid operation"));
                continue;
            }

            if (!await IsChangeAllowedForScopeAsync(dbContext, scopeType, request.ScopeId, change, entityType, operation))
            {
                rejected.Add(new SyncRejectedChange(change.Id, "scope_mismatch"));
                continue;
            }

            var timestamp = NormalizeTimestamp(change.ClientTimestamp);
            try
            {
                var error = await ApplyChangeAsync(dbContext, userId, change, entityType, operation, timestamp);
                if (error is null)
                {
                    accepted.Add(change.Id);
                    continue;
                }

                rejected.Add(new SyncRejectedChange(change.Id, error));
            }
            catch (Exception ex)
            {
                dbContext.SyncQueueItems.Add(new SyncQueueItem
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    EntityType = entityType,
                    EntityId = change.EntityId,
                    Operation = operation,
                    PayloadJson = change.Data is { } element ? element.GetRawText() : null,
                    Status = SyncQueueStatus.Failed,
                    RetryCount = 0,
                    NextRetryAt = DateTime.UtcNow,
                    LastAttemptAt = DateTime.UtcNow,
                    LastError = ex.Message,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                });

                rejected.Add(new SyncRejectedChange(change.Id, "queued_for_retry"));
            }
        }
        await dbContext.SaveChangesAsync();

        var response = new SyncPushResponse(
            request.CorrelationId,
            accepted,
            rejected,
            conflicts,
            DateTime.UtcNow);

        return Results.Ok(response);
    }

    private static async Task<IResult> PullAsync(
        ClaimsPrincipal user,
        string? scopeType,
        Guid? scopeId,
        long? since,
        ApplicationDbContext dbContext)
    {
        var userId = user.GetUserId();
        if (userId == Guid.Empty)
            return Results.Unauthorized();

        if (!TryNormalizeScopeType(scopeType, out var normalizedScope) || scopeId is null || scopeId == Guid.Empty)
            return Results.BadRequest();

        if (!await HasScopeAccessAsync(dbContext, normalizedScope, scopeId.Value, userId))
            return Results.Forbid();

        var sinceRevision = since.GetValueOrDefault(0);
        var changes = new List<(long Revision, SyncPullChange Change)>();

        var projectIds = new List<Guid>();
        var propertyIds = new List<Guid>();
        var zaznamIds = new List<Guid>();

        if (normalizedScope == SyncScopeType.Project)
        {
            projectIds.Add(scopeId.Value);
            propertyIds = await dbContext.Properties
                .IgnoreQueryFilters()
                .Where(p => p.ProjectId == scopeId.Value)
                .Select(p => p.Id)
                .ToListAsync();
        }
        else if (normalizedScope == SyncScopeType.Property)
        {
            var property = await dbContext.Properties
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(p => p.Id == scopeId.Value);
            if (property is null)
                return Results.NotFound();

            propertyIds.Add(property.Id);
            projectIds.Add(property.ProjectId);
        }
        else if (normalizedScope == SyncScopeType.Zaznam)
        {
            var zaznam = await dbContext.Zaznamy
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(z => z.Id == scopeId.Value);
            if (zaznam is null)
                return Results.NotFound();

            zaznamIds.Add(zaznam.Id);
            propertyIds.Add(zaznam.PropertyId);
            var projectId = await dbContext.Properties
                .IgnoreQueryFilters()
                .Where(p => p.Id == zaznam.PropertyId)
                .Select(p => p.ProjectId)
                .FirstOrDefaultAsync();
            if (projectId != Guid.Empty)
                projectIds.Add(projectId);
        }

        if (projectIds.Count > 0)
        {
            var projectsQuery = dbContext.Projects.IgnoreQueryFilters().Where(p => projectIds.Contains(p.Id));
            if (sinceRevision > 0)
                projectsQuery = projectsQuery.Where(p => p.ServerRevision > sinceRevision);

            var projects = await projectsQuery.ToListAsync();
            foreach (var project in projects)
                changes.Add((project.ServerRevision, BuildProjectChange(project)));
        }

        if (propertyIds.Count > 0)
        {
            var propertiesQuery = dbContext.Properties.IgnoreQueryFilters().Where(p => propertyIds.Contains(p.Id));
            if (sinceRevision > 0)
                propertiesQuery = propertiesQuery.Where(p => p.ServerRevision > sinceRevision);

            var properties = await propertiesQuery.ToListAsync();
            foreach (var property in properties)
                changes.Add((property.ServerRevision, BuildPropertyChange(property)));
        }

        var unitsQuery = dbContext.Units
            .IgnoreQueryFilters()
            .Where(u => propertyIds.Contains(u.PropertyId));
        if (sinceRevision > 0)
            unitsQuery = unitsQuery.Where(u => u.ServerRevision > sinceRevision);

        var units = await unitsQuery.ToListAsync();
        foreach (var unit in units)
            changes.Add((unit.ServerRevision, BuildUnitChange(unit)));

        var zaznamyQuery = dbContext.Zaznamy
            .IgnoreQueryFilters()
            .Where(z => propertyIds.Contains(z.PropertyId));
        if (zaznamIds.Count > 0)
            zaznamyQuery = zaznamyQuery.Where(z => zaznamIds.Contains(z.Id));
        if (sinceRevision > 0)
            zaznamyQuery = zaznamyQuery.Where(z => z.ServerRevision > sinceRevision);

        var zaznamy = await zaznamyQuery.ToListAsync();
        if (zaznamy.Count > 0)
        {
            var zaznamIdsForTags = zaznamy.Select(z => z.Id).ToList();
            var tagLookup = await dbContext.ZaznamTags
                .Where(t => zaznamIdsForTags.Contains(t.ZaznamId))
                .Join(dbContext.Tags, t => t.TagId, tag => tag.Id, (t, tag) => new { t.ZaznamId, tag.Name })
                .GroupBy(x => x.ZaznamId)
                .ToDictionaryAsync(g => g.Key, g => g.Select(x => x.Name).ToList());

            foreach (var zaznam in zaznamy)
            {
                tagLookup.TryGetValue(zaznam.Id, out var tags);
                changes.Add((zaznam.ServerRevision, BuildZaznamChange(zaznam, tags ?? [])));
            }
        }

        var accessibleZaznamIds = zaznamIds.Count > 0
            ? zaznamIds
            : propertyIds.Count == 0
                ? []
                : await dbContext.Zaznamy
                    .IgnoreQueryFilters()
                    .Where(z => propertyIds.Contains(z.PropertyId))
                    .Select(z => z.Id)
                    .ToListAsync();

        var documentsQuery = dbContext.ZaznamDokumenty
            .IgnoreQueryFilters()
            .Where(d => accessibleZaznamIds.Contains(d.ZaznamId));
        if (sinceRevision > 0)
            documentsQuery = documentsQuery.Where(d => d.ServerRevision > sinceRevision);

        var documents = await documentsQuery.ToListAsync();
        foreach (var document in documents)
            changes.Add((document.ServerRevision, BuildDocumentChange(document)));

        var commentsQuery = dbContext.Comments
            .IgnoreQueryFilters()
            .Where(c => accessibleZaznamIds.Contains(c.ZaznamId));
        if (sinceRevision > 0)
            commentsQuery = commentsQuery.Where(c => c.ServerRevision > sinceRevision);

        var comments = await commentsQuery.ToListAsync();
        foreach (var comment in comments)
            changes.Add((comment.ServerRevision, BuildCommentChange(comment)));

        var ordered = changes.OrderBy(c => c.Revision).ToList();
        var hasMore = ordered.Count > MaxPullChanges;
        var resultItems = ordered.Take(MaxPullChanges).Select(c => c.Change).ToList();

        var response = new SyncPullResponse(resultItems, DateTime.UtcNow, hasMore);
        return Results.Ok(response);
    }
    internal static async Task<string?> ApplyChangeAsync(
        ApplicationDbContext dbContext,
        Guid userId,
        SyncChange change,
        string entityType,
        string operation,
        DateTime timestamp)
    {
        return entityType switch
        {
            "project" => await ApplyProjectChangeAsync(dbContext, userId, change, operation, timestamp),
            "property" => await ApplyPropertyChangeAsync(dbContext, userId, change, operation, timestamp),
            "unit" => await ApplyUnitChangeAsync(dbContext, userId, change, operation, timestamp),
            "zaznam" => await ApplyZaznamChangeAsync(dbContext, userId, change, operation, timestamp),
            "document" => await ApplyDocumentChangeAsync(dbContext, userId, change, operation, timestamp),
            "comment" => await ApplyCommentChangeAsync(dbContext, userId, change, operation, timestamp),
            _ => "unsupported entityType"
        };
    }

    private static async Task<string?> ApplyProjectChangeAsync(
        ApplicationDbContext dbContext,
        Guid userId,
        SyncChange change,
        string operation,
        DateTime timestamp)
    {
        var existing = await dbContext.Projects
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(p => p.Id == change.EntityId);

        if (existing is not null && !await HasProjectAccessAsync(dbContext, existing.Id, userId))
            return "forbidden";

        if (operation == "delete")
        {
            if (existing is null)
                return null;

            existing.IsDeleted = true;
            existing.DeletedAt = timestamp;
            existing.UpdatedAt = timestamp;
            return null;
        }

        if (!TryGetDataObject(change.Data, out var data))
            return "missing data";

        if (existing is null)
        {
            if (!TryGetRequiredString(data, "name", out var name))
                return "missing name";

            TryGetString(data, "description", out var description);

            var project = new Project
            {
                Id = change.EntityId,
                OwnerId = userId,
                Name = name,
                Description = description,
                CreatedAt = timestamp,
                UpdatedAt = timestamp
            };

            dbContext.Projects.Add(project);
            return null;
        }
        if (TryGetString(data, "name", out var updatedName) && !string.IsNullOrWhiteSpace(updatedName))
            existing.Name = updatedName;

        if (TryGetString(data, "description", out var updatedDescription))
            existing.Description = updatedDescription;

        existing.UpdatedAt = timestamp;
        return null;
    }
    private static async Task<string?> ApplyPropertyChangeAsync(
        ApplicationDbContext dbContext,
        Guid userId,
        SyncChange change,
        string operation,
        DateTime timestamp)
    {
        var existing = await dbContext.Properties
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(p => p.Id == change.EntityId);

        if (existing is not null && !await HasPropertyAccessAsync(dbContext, existing.Id, userId))
            return "forbidden";

        if (operation == "delete")
        {
            if (existing is null)
                return null;

            existing.IsDeleted = true;
            existing.DeletedAt = timestamp;
            existing.UpdatedAt = timestamp;
            return null;
        }

        if (!TryGetDataObject(change.Data, out var data))
            return "missing data";
        if (existing is null)
        {
            if (!TryGetRequiredGuid(data, "projectId", out var projectId))
                return "missing projectId";
            if (!await HasProjectAccessAsync(dbContext, projectId, userId))
                return "forbidden";
            if (!TryGetRequiredString(data, "name", out var name))
                return "missing name";

            TryGetString(data, "description", out var description);
            TryGetDecimal(data, "latitude", out var latitude);
            TryGetDecimal(data, "longitude", out var longitude);

            var geoRadius = 100;
            if (TryGetInt32(data, "geoRadius", out var radius))
                geoRadius = radius;

            var property = new Property
            {
                Id = change.EntityId,
                ProjectId = projectId,
                Name = name,
                Description = description,
                Latitude = latitude,
                Longitude = longitude,
                GeoRadius = geoRadius,
                CreatedAt = timestamp,
                UpdatedAt = timestamp
            };

            dbContext.Properties.Add(property);
            return null;
        }
        if (TryGetString(data, "name", out var updatedName) && !string.IsNullOrWhiteSpace(updatedName))
            existing.Name = updatedName;

        if (TryGetString(data, "description", out var updatedDescription))
            existing.Description = updatedDescription;

        if (TryGetDecimal(data, "latitude", out var updatedLatitude))
            existing.Latitude = updatedLatitude;

        if (TryGetDecimal(data, "longitude", out var updatedLongitude))
            existing.Longitude = updatedLongitude;

        if (TryGetInt32(data, "geoRadius", out var updatedGeoRadius))
            existing.GeoRadius = updatedGeoRadius;

        existing.UpdatedAt = timestamp;
        return null;
    }
    private static async Task<string?> ApplyUnitChangeAsync(
        ApplicationDbContext dbContext,
        Guid userId,
        SyncChange change,
        string operation,
        DateTime timestamp)
    {
        var existing = await dbContext.Units
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.Id == change.EntityId);

        if (existing is not null && !await HasPropertyAccessAsync(dbContext, existing.PropertyId, userId))
            return "forbidden";

        if (operation == "delete")
        {
            if (existing is null)
                return null;

            existing.IsDeleted = true;
            existing.DeletedAt = timestamp;
            existing.UpdatedAt = timestamp;
            return null;
        }

        if (!TryGetDataObject(change.Data, out var data))
            return "missing data";
        if (existing is null)
        {
            if (!TryGetRequiredGuid(data, "propertyId", out var propertyId))
                return "missing propertyId";
            if (!await HasPropertyAccessAsync(dbContext, propertyId, userId))
                return "forbidden";
            if (!TryGetRequiredString(data, "name", out var name))
                return "missing name";
            if (!TryGetRequiredString(data, "unitType", out var unitTypeRaw))
                return "missing unitType";
            if (!TryParseUnitType(unitTypeRaw, out var unitType))
                return "invalid unitType";

            TryGetGuid(data, "parentUnitId", out var parentUnitId);
            TryGetString(data, "description", out var description);

            var unit = new Unit
            {
                Id = change.EntityId,
                PropertyId = propertyId,
                ParentUnitId = parentUnitId,
                Name = name,
                Description = description,
                UnitType = unitType,
                CreatedAt = timestamp,
                UpdatedAt = timestamp
            };

            dbContext.Units.Add(unit);
            return null;
        }
        if (TryGetGuid(data, "parentUnitId", out var updatedParentUnitId))
            existing.ParentUnitId = updatedParentUnitId;

        if (TryGetString(data, "name", out var updatedName) && !string.IsNullOrWhiteSpace(updatedName))
            existing.Name = updatedName;

        if (TryGetString(data, "description", out var updatedDescription))
            existing.Description = updatedDescription;

        if (TryGetString(data, "unitType", out var updatedUnitTypeRaw)
            && TryParseUnitType(updatedUnitTypeRaw, out var updatedUnitType))
            existing.UnitType = updatedUnitType;

        existing.UpdatedAt = timestamp;
        return null;
    }
    private static async Task<string?> ApplyZaznamChangeAsync(
        ApplicationDbContext dbContext,
        Guid userId,
        SyncChange change,
        string operation,
        DateTime timestamp)
    {
        var existing = await dbContext.Zaznamy
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(z => z.Id == change.EntityId);

        if (existing is not null && !await HasPropertyAccessAsync(dbContext, existing.PropertyId, userId))
            return "forbidden";

        if (operation == "delete")
        {
            if (existing is null)
                return null;

            existing.IsDeleted = true;
            existing.DeletedAt = timestamp;
            existing.UpdatedAt = timestamp;
            return null;
        }

        if (!TryGetDataObject(change.Data, out var data))
            return "missing data";
        if (existing is null)
        {
            if (!TryGetRequiredGuid(data, "propertyId", out var propertyId))
                return "missing propertyId";
            if (!await HasPropertyAccessAsync(dbContext, propertyId, userId))
                return "forbidden";

            TryGetGuid(data, "unitId", out var unitId);
            TryGetString(data, "title", out var title);
            TryGetString(data, "description", out var description);

            var date = DateOnly.FromDateTime(timestamp);
            if (TryGetDateOnly(data, "date", out var parsedDate) && parsedDate.HasValue)
                date = parsedDate.Value;

            int? cost = null;
            if (TryGetInt32Nullable(data, "cost", out var parsedCost))
                cost = parsedCost;

            var status = ParseStatus(ReadStringValue(data, "status"));
            var flags = ParseFlags(ReadStringListValue(data, "flags"));

            var zaznam = new Zaznam
            {
                Id = change.EntityId,
                PropertyId = propertyId,
                UnitId = unitId,
                Title = title,
                Description = description,
                Date = date,
                Cost = cost,
                Status = status,
                Flags = flags,
                CreatedAt = timestamp,
                UpdatedAt = timestamp
            };

            dbContext.Zaznamy.Add(zaznam);

            if (TryGetStringList(data, "tags", out var tagNames) && tagNames.Count > 0)
                await ApplyTagsAsync(dbContext, zaznam.Id, tagNames);

            return null;
        }
        if (TryGetGuid(data, "unitId", out var updatedUnitId))
            existing.UnitId = updatedUnitId;

        if (TryGetString(data, "title", out var updatedTitle))
            existing.Title = updatedTitle;

        if (TryGetString(data, "description", out var updatedDescription))
            existing.Description = updatedDescription;

        if (TryGetDateOnly(data, "date", out var updatedDate) && updatedDate.HasValue)
            existing.Date = updatedDate.Value;

        if (TryGetInt32Nullable(data, "cost", out var updatedCost))
            existing.Cost = updatedCost;

        if (TryGetString(data, "status", out var updatedStatus) && !string.IsNullOrWhiteSpace(updatedStatus))
            existing.Status = ParseStatus(updatedStatus);
        if (TryGetStringList(data, "flags", out var updatedFlags))
            existing.Flags = ParseFlags(updatedFlags);

        if (TryGetStringList(data, "tags", out var updatedTags))
            await ApplyTagsAsync(dbContext, existing.Id, updatedTags);

        existing.UpdatedAt = timestamp;
        return null;
    }
    private static async Task<string?> ApplyDocumentChangeAsync(
        ApplicationDbContext dbContext,
        Guid userId,
        SyncChange change,
        string operation,
        DateTime timestamp)
    {
        var existing = await dbContext.ZaznamDokumenty
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(d => d.Id == change.EntityId);

        if (existing is not null && !await HasZaznamAccessAsync(dbContext, existing.ZaznamId, userId))
            return "forbidden";

        if (operation == "delete")
        {
            if (existing is null)
                return null;

            existing.IsDeleted = true;
            existing.DeletedAt = timestamp;
            existing.UpdatedAt = timestamp;
            return null;
        }

        if (!TryGetDataObject(change.Data, out var data))
            return "missing data";
        if (existing is null)
        {
            if (!TryGetRequiredGuid(data, "zaznamId", out var zaznamId))
                return "missing zaznamId";
            if (!await HasZaznamAccessAsync(dbContext, zaznamId, userId))
                return "forbidden";
            if (!TryGetRequiredString(data, "type", out var typeRaw))
                return "missing type";
            if (!TryParseDocumentType(typeRaw, out var docType))
                return "invalid type";
            if (!TryGetRequiredString(data, "storageKey", out var storageKey))
                return "missing storageKey";
            if (!TryGetRequiredString(data, "mimeType", out var mimeType))
                return "missing mimeType";
            if (!TryGetInt64(data, "sizeBytes", out var sizeBytes))
                return "missing sizeBytes";

            TryGetString(data, "originalFileName", out var originalFileName);
            TryGetString(data, "description", out var description);

            var document = new ZaznamDokument
            {
                Id = change.EntityId,
                ZaznamId = zaznamId,
                Type = docType,
                StorageKey = storageKey,
                OriginalFileName = originalFileName,
                MimeType = mimeType,
                SizeBytes = sizeBytes,
                Description = description,
                CreatedAt = timestamp,
                UpdatedAt = timestamp
            };

            dbContext.ZaznamDokumenty.Add(document);
            return null;
        }
        if (TryGetString(data, "type", out var updatedTypeRaw)
            && TryParseDocumentType(updatedTypeRaw, out var updatedType))
            existing.Type = updatedType;

        if (TryGetString(data, "storageKey", out var updatedStorageKey)
            && !string.IsNullOrWhiteSpace(updatedStorageKey))
            existing.StorageKey = updatedStorageKey;

        if (TryGetString(data, "originalFileName", out var updatedOriginalFileName))
            existing.OriginalFileName = updatedOriginalFileName;

        if (TryGetString(data, "mimeType", out var updatedMimeType)
            && !string.IsNullOrWhiteSpace(updatedMimeType))
            existing.MimeType = updatedMimeType;

        if (TryGetInt64(data, "sizeBytes", out var updatedSizeBytes))
            existing.SizeBytes = updatedSizeBytes;

        if (TryGetString(data, "description", out var updatedDescription))
            existing.Description = updatedDescription;

        existing.UpdatedAt = timestamp;
        return null;
    }
    private static async Task<string?> ApplyCommentChangeAsync(
        ApplicationDbContext dbContext,
        Guid userId,
        SyncChange change,
        string operation,
        DateTime timestamp)
    {
        var existing = await dbContext.Comments
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(c => c.Id == change.EntityId);

        if (existing is not null && !await HasZaznamAccessAsync(dbContext, existing.ZaznamId, userId))
            return "forbidden";

        if (operation == "delete")
        {
            if (existing is null)
                return null;

            existing.IsDeleted = true;
            existing.DeletedAt = timestamp;
            existing.UpdatedAt = timestamp;
            return null;
        }

        if (!TryGetDataObject(change.Data, out var data))
            return "missing data";
        if (existing is null)
        {
            if (!TryGetRequiredGuid(data, "zaznamId", out var zaznamId))
                return "missing zaznamId";
            if (!await HasZaznamAccessAsync(dbContext, zaznamId, userId))
                return "forbidden";
            if (!TryGetRequiredString(data, "content", out var content))
                return "missing content";

            var comment = new Comment
            {
                Id = change.EntityId,
                ZaznamId = zaznamId,
                AuthorUserId = userId,
                Content = content,
                CreatedAt = timestamp,
                UpdatedAt = timestamp
            };

            dbContext.Comments.Add(comment);
            return null;
        }

        if (TryGetString(data, "content", out var updatedContent) && !string.IsNullOrWhiteSpace(updatedContent))
            existing.Content = updatedContent;

        existing.UpdatedAt = timestamp;
        return null;
    }
    private static async Task ApplyTagsAsync(
        ApplicationDbContext dbContext,
        Guid zaznamId,
        IReadOnlyList<string> tagNames)
    {
        var trimmed = tagNames
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Select(name => name.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var existing = await dbContext.ZaznamTags
            .Where(t => t.ZaznamId == zaznamId)
            .ToListAsync();
        dbContext.ZaznamTags.RemoveRange(existing);

        if (trimmed.Count == 0)
            return;

        var tags = await dbContext.Tags
            .Where(tag => trimmed.Contains(tag.Name))
            .ToListAsync();

        foreach (var tag in tags)
            dbContext.ZaznamTags.Add(new ZaznamTag { ZaznamId = zaznamId, TagId = tag.Id });
    }

    private static Task<bool> HasProjectAccessAsync(ApplicationDbContext dbContext, Guid projectId, Guid userId)
    {
        return dbContext.Projects.AnyAsync(p => p.Id == projectId
            && (p.OwnerId == userId
                || dbContext.ProjectMembers.Any(pm => pm.ProjectId == p.Id && pm.UserId == userId)
                || dbContext.Properties.Any(prop => prop.ProjectId == p.Id
                    && dbContext.PropertyMembers.Any(pm => pm.PropertyId == prop.Id && pm.UserId == userId))));
    }

    private static Task<bool> HasPropertyAccessAsync(ApplicationDbContext dbContext, Guid propertyId, Guid userId)
    {
        return dbContext.Properties.AnyAsync(p => p.Id == propertyId
            && (dbContext.Projects.Any(pr => pr.Id == p.ProjectId && pr.OwnerId == userId)
                || dbContext.ProjectMembers.Any(pm => pm.ProjectId == p.ProjectId && pm.UserId == userId)
                || dbContext.PropertyMembers.Any(pm => pm.PropertyId == p.Id && pm.UserId == userId)));
    }

    private static async Task<bool> HasZaznamAccessAsync(ApplicationDbContext dbContext, Guid zaznamId, Guid userId)
    {
        if (await dbContext.ZaznamMembers.AnyAsync(m => m.ZaznamId == zaznamId && m.UserId == userId))
            return true;

        var propertyId = await dbContext.Zaznamy
            .IgnoreQueryFilters()
            .Where(z => z.Id == zaznamId)
            .Select(z => z.PropertyId)
            .FirstOrDefaultAsync();

        if (propertyId == Guid.Empty)
            return false;

        return await HasPropertyAccessAsync(dbContext, propertyId, userId);
    }

    private static Task<bool> HasScopeAccessAsync(
        ApplicationDbContext dbContext,
        SyncScopeType scopeType,
        Guid scopeId,
        Guid userId)
    {
        return scopeType switch
        {
            SyncScopeType.Project => HasProjectAccessAsync(dbContext, scopeId, userId),
            SyncScopeType.Property => HasPropertyAccessAsync(dbContext, scopeId, userId),
            SyncScopeType.Zaznam => HasZaznamAccessAsync(dbContext, scopeId, userId),
            _ => Task.FromResult(false)
        };
    }

    private static async Task<bool> IsChangeAllowedForScopeAsync(
        ApplicationDbContext dbContext,
        SyncScopeType scopeType,
        Guid scopeId,
        SyncChange change,
        string entityType,
        string operation)
    {
        switch (scopeType)
        {
            case SyncScopeType.Project:
                return await IsChangeAllowedForProjectScopeAsync(dbContext, scopeId, change, entityType, operation);
            case SyncScopeType.Property:
                return await IsChangeAllowedForPropertyScopeAsync(dbContext, scopeId, change, entityType, operation);
            case SyncScopeType.Zaznam:
                return await IsChangeAllowedForZaznamScopeAsync(dbContext, scopeId, change, entityType, operation);
            default:
                return false;
        }
    }

    private static async Task<bool> IsChangeAllowedForProjectScopeAsync(
        ApplicationDbContext dbContext,
        Guid projectId,
        SyncChange change,
        string entityType,
        string operation)
    {
        if (operation == "create")
        {
            return await IsCreateAllowedForProjectScopeAsync(dbContext, projectId, change, entityType);
        }

        var entityId = change.EntityId;
        return entityType switch
        {
            "project" => entityId == projectId,
            "property" => await dbContext.Properties.AnyAsync(p => p.Id == entityId && p.ProjectId == projectId),
            "unit" => await dbContext.Units.AnyAsync(u => u.Id == entityId && dbContext.Properties.Any(p => p.Id == u.PropertyId && p.ProjectId == projectId)),
            "zaznam" => await dbContext.Zaznamy.AnyAsync(z => z.Id == entityId && dbContext.Properties.Any(p => p.Id == z.PropertyId && p.ProjectId == projectId)),
            "document" => await dbContext.ZaznamDokumenty.AnyAsync(d => d.Id == entityId && dbContext.Zaznamy.Any(z => z.Id == d.ZaznamId && dbContext.Properties.Any(p => p.Id == z.PropertyId && p.ProjectId == projectId))),
            "comment" => await dbContext.Comments.AnyAsync(c => c.Id == entityId && dbContext.Zaznamy.Any(z => z.Id == c.ZaznamId && dbContext.Properties.Any(p => p.Id == z.PropertyId && p.ProjectId == projectId))),
            _ => false
        };
    }

    private static async Task<bool> IsChangeAllowedForPropertyScopeAsync(
        ApplicationDbContext dbContext,
        Guid propertyId,
        SyncChange change,
        string entityType,
        string operation)
    {
        if (operation == "create")
        {
            return await IsCreateAllowedForPropertyScopeAsync(dbContext, propertyId, change, entityType);
        }

        var entityId = change.EntityId;
        return entityType switch
        {
            "project" => false,
            "property" => entityId == propertyId,
            "unit" => await dbContext.Units.AnyAsync(u => u.Id == entityId && u.PropertyId == propertyId),
            "zaznam" => await dbContext.Zaznamy.AnyAsync(z => z.Id == entityId && z.PropertyId == propertyId),
            "document" => await dbContext.ZaznamDokumenty.AnyAsync(d => d.Id == entityId && dbContext.Zaznamy.Any(z => z.Id == d.ZaznamId && z.PropertyId == propertyId)),
            "comment" => await dbContext.Comments.AnyAsync(c => c.Id == entityId && dbContext.Zaznamy.Any(z => z.Id == c.ZaznamId && z.PropertyId == propertyId)),
            _ => false
        };
    }

    private static async Task<bool> IsChangeAllowedForZaznamScopeAsync(
        ApplicationDbContext dbContext,
        Guid zaznamId,
        SyncChange change,
        string entityType,
        string operation)
    {
        if (operation == "create")
        {
            return await IsCreateAllowedForZaznamScopeAsync(dbContext, zaznamId, change, entityType);
        }

        var entityId = change.EntityId;
        return entityType switch
        {
            "project" => false,
            "property" => false,
            "unit" => false,
            "zaznam" => entityId == zaznamId,
            "document" => await dbContext.ZaznamDokumenty.AnyAsync(d => d.Id == entityId && d.ZaznamId == zaznamId),
            "comment" => await dbContext.Comments.AnyAsync(c => c.Id == entityId && c.ZaznamId == zaznamId),
            _ => false
        };
    }

    private static async Task<bool> IsCreateAllowedForProjectScopeAsync(
        ApplicationDbContext dbContext,
        Guid projectId,
        SyncChange change,
        string entityType)
    {
        if (!TryGetDataObject(change.Data, out var data))
            return false;

        return entityType switch
        {
            "project" => change.EntityId == projectId,
            "property" => TryGetRequiredGuid(data, "projectId", out var propProjectId) && propProjectId == projectId,
            "unit" => TryGetRequiredGuid(data, "propertyId", out var unitPropertyId)
                && await dbContext.Properties.AnyAsync(p => p.Id == unitPropertyId && p.ProjectId == projectId),
            "zaznam" => TryGetRequiredGuid(data, "propertyId", out var zaznamPropertyId)
                && await dbContext.Properties.AnyAsync(p => p.Id == zaznamPropertyId && p.ProjectId == projectId),
            "document" => TryGetRequiredGuid(data, "zaznamId", out var docZaznamId)
                && await dbContext.Zaznamy.AnyAsync(z => z.Id == docZaznamId && dbContext.Properties.Any(p => p.Id == z.PropertyId && p.ProjectId == projectId)),
            "comment" => TryGetRequiredGuid(data, "zaznamId", out var commentZaznamId)
                && await dbContext.Zaznamy.AnyAsync(z => z.Id == commentZaznamId && dbContext.Properties.Any(p => p.Id == z.PropertyId && p.ProjectId == projectId)),
            _ => false
        };
    }

    private static async Task<bool> IsCreateAllowedForPropertyScopeAsync(
        ApplicationDbContext dbContext,
        Guid propertyId,
        SyncChange change,
        string entityType)
    {
        if (!TryGetDataObject(change.Data, out var data))
            return false;

        return entityType switch
        {
            "property" => change.EntityId == propertyId,
            "unit" => TryGetRequiredGuid(data, "propertyId", out var unitPropertyId) && unitPropertyId == propertyId,
            "zaznam" => TryGetRequiredGuid(data, "propertyId", out var zaznamPropertyId) && zaznamPropertyId == propertyId,
            "document" => TryGetRequiredGuid(data, "zaznamId", out var docZaznamId)
                && await dbContext.Zaznamy.AnyAsync(z => z.Id == docZaznamId && z.PropertyId == propertyId),
            "comment" => TryGetRequiredGuid(data, "zaznamId", out var commentZaznamId)
                && await dbContext.Zaznamy.AnyAsync(z => z.Id == commentZaznamId && z.PropertyId == propertyId),
            _ => false
        };
    }

    private static async Task<bool> IsCreateAllowedForZaznamScopeAsync(
        ApplicationDbContext dbContext,
        Guid zaznamId,
        SyncChange change,
        string entityType)
    {
        if (!TryGetDataObject(change.Data, out var data))
            return false;

        return entityType switch
        {
            "zaznam" => change.EntityId == zaznamId,
            "document" => TryGetRequiredGuid(data, "zaznamId", out var docZaznamId) && docZaznamId == zaznamId,
            "comment" => TryGetRequiredGuid(data, "zaznamId", out var commentZaznamId) && commentZaznamId == zaznamId,
            _ => false
        };
    }

    private static SyncPullChange BuildProjectChange(Project project)
    {
        if (project.IsDeleted)
            return new SyncPullChange("project", project.Id, "delete", null, RevisionToTimestamp(project.ServerRevision));

        var data = JsonSerializer.SerializeToElement(new
        {
            ownerId = project.OwnerId,
            name = project.Name,
            description = project.Description
        });

        return new SyncPullChange(
            "project",
            project.Id,
            ResolveOperation(project.CreatedAt, project.UpdatedAt),
            data,
            RevisionToTimestamp(project.ServerRevision));
    }
    private static SyncPullChange BuildPropertyChange(Property property)
    {
        if (property.IsDeleted)
            return new SyncPullChange("property", property.Id, "delete", null, RevisionToTimestamp(property.ServerRevision));

        var data = JsonSerializer.SerializeToElement(new
        {
            projectId = property.ProjectId,
            name = property.Name,
            description = property.Description,
            latitude = property.Latitude,
            longitude = property.Longitude,
            geoRadius = property.GeoRadius
        });

        return new SyncPullChange(
            "property",
            property.Id,
            ResolveOperation(property.CreatedAt, property.UpdatedAt),
            data,
            RevisionToTimestamp(property.ServerRevision));
    }
    private static SyncPullChange BuildUnitChange(Unit unit)
    {
        if (unit.IsDeleted)
            return new SyncPullChange("unit", unit.Id, "delete", null, RevisionToTimestamp(unit.ServerRevision));

        var data = JsonSerializer.SerializeToElement(new
        {
            propertyId = unit.PropertyId,
            parentUnitId = unit.ParentUnitId,
            name = unit.Name,
            description = unit.Description,
            unitType = unit.UnitType.ToString().ToLowerInvariant()
        });

        return new SyncPullChange(
            "unit",
            unit.Id,
            ResolveOperation(unit.CreatedAt, unit.UpdatedAt),
            data,
            RevisionToTimestamp(unit.ServerRevision));
    }
    private static SyncPullChange BuildZaznamChange(Zaznam zaznam, IReadOnlyList<string> tags)
    {
        if (zaznam.IsDeleted)
            return new SyncPullChange("zaznam", zaznam.Id, "delete", null, RevisionToTimestamp(zaznam.ServerRevision));

        var data = JsonSerializer.SerializeToElement(new
        {
            propertyId = zaznam.PropertyId,
            unitId = zaznam.UnitId,
            title = zaznam.Title,
            description = zaznam.Description,
            date = zaznam.Date,
            cost = zaznam.Cost,
            status = zaznam.Status.ToString().ToLowerInvariant(),
            flags = FlagsToStrings(zaznam.Flags),
            tags
        });

        return new SyncPullChange(
            "zaznam",
            zaznam.Id,
            ResolveOperation(zaznam.CreatedAt, zaznam.UpdatedAt),
            data,
            RevisionToTimestamp(zaznam.ServerRevision));
    }
    private static SyncPullChange BuildDocumentChange(ZaznamDokument document)
    {
        if (document.IsDeleted)
            return new SyncPullChange("document", document.Id, "delete", null, RevisionToTimestamp(document.ServerRevision));

        var data = JsonSerializer.SerializeToElement(new
        {
            zaznamId = document.ZaznamId,
            type = ToDocumentType(document.Type),
            storageKey = document.StorageKey,
            originalFileName = document.OriginalFileName,
            mimeType = document.MimeType,
            sizeBytes = document.SizeBytes,
            description = document.Description
        });

        return new SyncPullChange(
            "document",
            document.Id,
            ResolveOperation(document.CreatedAt, document.UpdatedAt),
            data,
            RevisionToTimestamp(document.ServerRevision));
    }

    private static SyncPullChange BuildCommentChange(Comment comment)
    {
        if (comment.IsDeleted)
            return new SyncPullChange("comment", comment.Id, "delete", null, RevisionToTimestamp(comment.ServerRevision));

        var data = JsonSerializer.SerializeToElement(new
        {
            zaznamId = comment.ZaznamId,
            authorUserId = comment.AuthorUserId,
            content = comment.Content
        });

        return new SyncPullChange(
            "comment",
            comment.Id,
            ResolveOperation(comment.CreatedAt, comment.UpdatedAt),
            data,
            RevisionToTimestamp(comment.ServerRevision));
    }
    private static string ResolveOperation(DateTime createdAt, DateTime updatedAt)
    {
        return createdAt == updatedAt ? "create" : "update";
    }

    private static DateTime RevisionToTimestamp(long revision)
    {
        if (revision <= 0)
            return DateTime.UnixEpoch;

        return DateTimeOffset.FromUnixTimeMilliseconds(revision).UtcDateTime;
    }

    internal static DateTime NormalizeTimestamp(DateTime timestamp)
    {
        if (timestamp == default)
            return DateTime.UtcNow;

        return timestamp.Kind switch
        {
            DateTimeKind.Utc => timestamp,
            DateTimeKind.Local => timestamp.ToUniversalTime(),
            _ => DateTime.SpecifyKind(timestamp, DateTimeKind.Utc)
        };
    }

    internal static bool TryNormalizeScopeType(string? raw, out SyncScopeType normalized)
    {
        normalized = SyncScopeType.Project;
        var value = raw?.Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(value))
            return false;

        return value switch
        {
            "project" => SetScopeType(SyncScopeType.Project, out normalized),
            "property" => SetScopeType(SyncScopeType.Property, out normalized),
            "zaznam" => SetScopeType(SyncScopeType.Zaznam, out normalized),
            _ => false
        };
    }

    private static bool SetScopeType(SyncScopeType value, out SyncScopeType normalized)
    {
        normalized = value;
        return true;
    }
    internal static bool TryNormalizeEntityType(string? raw, out string normalized)
    {
        normalized = raw?.Trim().ToLowerInvariant() ?? string.Empty;
        return normalized is "project" or "property" or "unit" or "zaznam" or "document" or "comment";
    }

    internal static bool TryNormalizeOperation(string? raw, out string normalized)
    {
        normalized = raw?.Trim().ToLowerInvariant() ?? string.Empty;
        return normalized is "create" or "update" or "delete";
    }

    private static bool TryGetDataObject(JsonElement? data, out JsonElement obj)
    {
        obj = default;
        if (data is null || data.Value.ValueKind != JsonValueKind.Object)
            return false;

        obj = data.Value;
        return true;
    }
    private static bool TryGetProperty(JsonElement data, string name, out JsonElement value)
    {
        if (data.ValueKind == JsonValueKind.Object && data.TryGetProperty(name, out value))
            return true;

        value = default;
        return false;
    }

    private static bool TryGetString(JsonElement data, string name, out string? value)
    {
        value = null;
        if (!TryGetProperty(data, name, out var prop))
            return false;

        if (prop.ValueKind == JsonValueKind.Null)
            return true;

        if (prop.ValueKind == JsonValueKind.String)
        {
            value = prop.GetString();
            return true;
        }

        return false;
    }

    private static bool TryGetRequiredString(JsonElement data, string name, out string value)
    {
        value = string.Empty;
        if (!TryGetString(data, name, out var raw) || string.IsNullOrWhiteSpace(raw))
            return false;

        value = raw;
        return true;
    }
    private static bool TryGetGuid(JsonElement data, string name, out Guid? value)
    {
        value = null;
        if (!TryGetProperty(data, name, out var prop))
            return false;

        if (prop.ValueKind == JsonValueKind.Null)
            return true;

        if (prop.ValueKind == JsonValueKind.String && Guid.TryParse(prop.GetString(), out var parsed))
        {
            value = parsed;
            return true;
        }

        return false;
    }

    private static bool TryGetRequiredGuid(JsonElement data, string name, out Guid value)
    {
        value = Guid.Empty;
        if (!TryGetGuid(data, name, out var raw) || raw is null || raw == Guid.Empty)
            return false;

        value = raw.Value;
        return true;
    }
    private static bool TryGetInt32(JsonElement data, string name, out int value)
    {
        value = default;
        if (!TryGetProperty(data, name, out var prop))
            return false;

        return prop.ValueKind == JsonValueKind.Number && prop.TryGetInt32(out value);
    }

    private static bool TryGetInt32Nullable(JsonElement data, string name, out int? value)
    {
        value = null;
        if (!TryGetProperty(data, name, out var prop))
            return false;

        if (prop.ValueKind == JsonValueKind.Null)
            return true;

        if (prop.ValueKind == JsonValueKind.Number && prop.TryGetInt32(out var parsed))
        {
            value = parsed;
            return true;
        }

        return false;
    }
    private static bool TryGetInt64(JsonElement data, string name, out long value)
    {
        value = default;
        if (!TryGetProperty(data, name, out var prop))
            return false;

        return prop.ValueKind == JsonValueKind.Number && prop.TryGetInt64(out value);
    }

    private static bool TryGetDecimal(JsonElement data, string name, out decimal? value)
    {
        value = null;
        if (!TryGetProperty(data, name, out var prop))
            return false;

        if (prop.ValueKind == JsonValueKind.Null)
            return true;

        if (prop.ValueKind == JsonValueKind.Number && prop.TryGetDecimal(out var parsed))
        {
            value = parsed;
            return true;
        }

        return false;
    }

    private static bool TryGetDateOnly(JsonElement data, string name, out DateOnly? value)
    {
        value = null;
        if (!TryGetProperty(data, name, out var prop))
            return false;

        if (prop.ValueKind == JsonValueKind.Null)
            return true;

        if (prop.ValueKind == JsonValueKind.String && DateOnly.TryParse(prop.GetString(), out var parsed))
        {
            value = parsed;
            return true;
        }

        return false;
    }
    private static bool TryGetStringList(JsonElement data, string name, out List<string> values)
    {
        values = [];
        if (!TryGetProperty(data, name, out var prop))
            return false;

        if (prop.ValueKind == JsonValueKind.Null)
            return true;

        if (prop.ValueKind != JsonValueKind.Array)
            return false;

        foreach (var item in prop.EnumerateArray())
        {
            if (item.ValueKind == JsonValueKind.String && item.GetString() is { } text)
                values.Add(text);
        }

        return true;
    }

    private static string? ReadStringValue(JsonElement data, string name)
    {
        return TryGetString(data, name, out var value) ? value : null;
    }

    private static IReadOnlyList<string>? ReadStringListValue(JsonElement data, string name)
    {
        return TryGetStringList(data, name, out var values) ? values : null;
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

        return Enum.GetValues<ZaznamFlags>()
            .Where(flag => flag != ZaznamFlags.None && flags.HasFlag(flag))
            .Select(flag => flag.ToString().ToLowerInvariant())
            .ToList();
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
    private static bool TryParseUnitType(string? raw, out UnitType unitType)
    {
        unitType = UnitType.Other;
        if (string.IsNullOrWhiteSpace(raw))
            return false;

        return Enum.TryParse<UnitType>(raw, true, out unitType);
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

    private static string ToDocumentType(DocumentType type)
    {
        return type switch
        {
            DocumentType.Photo => "photo",
            DocumentType.Document => "document",
            DocumentType.Receipt => "receipt",
            _ => "document"
        };
    }
}

