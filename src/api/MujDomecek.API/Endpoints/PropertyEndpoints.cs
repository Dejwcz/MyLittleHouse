using System.Security.Claims;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using MujDomecek.API.Extensions;
using MujDomecek.API.Services;
using MujDomecek.Application.Abstractions;
using MujDomecek.Application.DTOs;
using MujDomecek.Domain.Aggregates.Project;
using MujDomecek.Domain.Aggregates.Property;
using MujDomecek.Domain.Aggregates.Zaznam;
using MujDomecek.Domain.ValueObjects;
using MujDomecek.Infrastructure.Persistence;

namespace MujDomecek.API.Endpoints;

public static class PropertyEndpoints
{
    public static IEndpointRouteBuilder MapPropertyEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/properties").RequireAuthorization();

        group.MapGet("/", GetPropertiesAsync);
        group.MapPost("/", CreatePropertyAsync);
        group.MapGet("/{id:guid}", GetPropertyAsync);
        group.MapPut("/{id:guid}", UpdatePropertyAsync);
        group.MapPatch("/{id:guid}/cover", UpdatePropertyCoverAsync);
        group.MapDelete("/{id:guid}", DeletePropertyAsync);
        group.MapGet("/{id:guid}/stats", GetStatsAsync);
        group.MapGet("/{id:guid}/members", GetMembersAsync);
        group.MapPost("/{id:guid}/members", AddMemberAsync);
        group.MapPut("/{id:guid}/members/{userId:guid}", UpdateMemberAsync);
        group.MapDelete("/{id:guid}/members/{userId:guid}", RemoveMemberAsync);
        group.MapPost("/{id:guid}/leave", LeavePropertyAsync);
        group.MapGet("/{id:guid}/activity", GetActivityAsync);

        return endpoints;
    }

    private static async Task<IResult> GetPropertiesAsync(
        ClaimsPrincipal user,
        Guid? projectId,
        bool? shared,
        IStorageService storageService,
        ApplicationDbContext dbContext)
    {
        var userId = user.GetUserId();
        if (userId == Guid.Empty)
            return Results.Unauthorized();

        var query = dbContext.Properties.AsQueryable();

        if (projectId.HasValue)
            query = query.Where(p => p.ProjectId == projectId.Value);

        var projectMemberships = dbContext.ProjectMembers.Where(m => m.UserId == userId);
        var propertyMemberships = dbContext.PropertyMembers.Where(m => m.UserId == userId);

        query = query.Where(p =>
            dbContext.Projects.Any(pr => pr.Id == p.ProjectId && pr.OwnerId == userId)
            || projectMemberships.Any(pm => pm.ProjectId == p.ProjectId)
            || propertyMemberships.Any(pm => pm.PropertyId == p.Id));

        var properties = await query
            .Select(p => new
            {
                Property = p,
                Project = dbContext.Projects.First(pr => pr.Id == p.ProjectId),
                PropertyMember = dbContext.PropertyMembers.FirstOrDefault(pm => pm.PropertyId == p.Id && pm.UserId == userId),
                ProjectMember = dbContext.ProjectMembers.FirstOrDefault(pm => pm.ProjectId == p.ProjectId && pm.UserId == userId)
            })
            .ToListAsync();

        var coverMediaIds = properties
            .Select(x => x.Property.CoverMediaId)
            .Where(id => id.HasValue)
            .Select(id => id.GetValueOrDefault())
            .Distinct()
            .ToList();

        Dictionary<Guid, string?> coverLookup = coverMediaIds.Count == 0
            ? new Dictionary<Guid, string?>()
            : await dbContext.Media
                .Where(m => coverMediaIds.Contains(m.Id) && m.OwnerType == OwnerType.Property)
                .Select(m => new { m.Id, m.StorageKey })
                .ToDictionaryAsync(m => m.Id, m => (string?)m.StorageKey);

        var items = properties.Select(x => new PropertyDto(
            x.Property.Id,
            x.Property.ProjectId,
            x.Project.Name,
            x.Property.Name,
            x.Property.Description,
            x.Property.PropertyType.ToString().ToLowerInvariant(),
            x.Property.Latitude,
            x.Property.Longitude,
            x.Property.GeoRadius,
            dbContext.Units.Count(u => u.PropertyId == x.Property.Id),
            dbContext.Zaznamy.Count(z => z.PropertyId == x.Property.Id),
            dbContext.Zaznamy.Where(z => z.PropertyId == x.Property.Id).Sum(z => (decimal?)z.Cost) ?? 0,
            ToRoleString(x.PropertyMember?.Role ?? x.ProjectMember?.Role ?? MemberRole.Viewer, x.Project.OwnerId == userId),
            x.Project.OwnerId != userId,
            ToSyncModeString(x.Property.SyncMode),
            ToSyncStatusString(x.Property.SyncStatus),
            x.Property.CoverMediaId,
            GetCoverUrl(storageService, coverLookup, x.Property.CoverMediaId),
            x.Property.CreatedAt,
            x.Property.UpdatedAt)).ToList();

        if (shared.HasValue)
            items = items.Where(p => p.IsShared == shared.Value).ToList();

        return Results.Ok(new PropertyListResponse(items, items.Count));
    }

    private static async Task<IResult> CreatePropertyAsync(
        ClaimsPrincipal user,
        [FromBody] CreatePropertyRequest request,
        ApplicationDbContext dbContext)
    {
        var userId = user.GetUserId();
        if (userId == Guid.Empty)
            return Results.Unauthorized();

        var project = await dbContext.Projects.FirstOrDefaultAsync(p => p.Id == request.ProjectId);
        if (project is null)
            return Results.NotFound();

        if (!await HasProjectAccessAsync(dbContext, project.Id, userId))
            return Results.Forbid();

        var property = new Property
        {
            Id = Guid.NewGuid(),
            ProjectId = project.Id,
            Name = request.Name,
            Description = request.Description,
            PropertyType = Enum.TryParse<PropertyType>(request.PropertyType, true, out var parsed) ? parsed : PropertyType.Other,
            Latitude = request.Latitude,
            Longitude = request.Longitude,
            GeoRadius = request.GeoRadius ?? 100,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        dbContext.Properties.Add(property);
        await dbContext.SaveChangesAsync();

        var dto = new PropertyDto(
            property.Id,
            property.ProjectId,
            project.Name,
            property.Name,
            property.Description,
            property.PropertyType.ToString().ToLowerInvariant(),
            property.Latitude,
            property.Longitude,
            property.GeoRadius,
            0,
            0,
            0,
            project.OwnerId == userId ? "owner" : "editor",
            project.OwnerId != userId,
            ToSyncModeString(property.SyncMode),
            ToSyncStatusString(property.SyncStatus),
            property.CoverMediaId,
            null,
            property.CreatedAt,
            property.UpdatedAt);

        return Results.Created($"/properties/{property.Id}", dto);
    }

    private static async Task<IResult> GetPropertyAsync(
        ClaimsPrincipal user,
        Guid id,
        IStorageService storageService,
        ApplicationDbContext dbContext)
    {
        var userId = user.GetUserId();
        if (userId == Guid.Empty)
            return Results.Unauthorized();

        var property = await dbContext.Properties.FirstOrDefaultAsync(p => p.Id == id);
        if (property is null)
            return Results.NotFound();

        if (!await HasPropertyAccessAsync(dbContext, property.Id, userId))
            return Results.Forbid();

        var project = await dbContext.Projects.FirstAsync(p => p.Id == property.ProjectId);
        var propertyMember = await dbContext.PropertyMembers.FirstOrDefaultAsync(pm => pm.PropertyId == property.Id && pm.UserId == userId);
        var projectMember = await dbContext.ProjectMembers.FirstOrDefaultAsync(pm => pm.ProjectId == property.ProjectId && pm.UserId == userId);
        var coverUrl = await GetCoverUrlAsync(dbContext, storageService, OwnerType.Property, property.Id, property.CoverMediaId);

        var dto = new PropertyDto(
            property.Id,
            property.ProjectId,
            project.Name,
            property.Name,
            property.Description,
            property.PropertyType.ToString().ToLowerInvariant(),
            property.Latitude,
            property.Longitude,
            property.GeoRadius,
            dbContext.Units.Count(u => u.PropertyId == property.Id),
            dbContext.Zaznamy.Count(z => z.PropertyId == property.Id),
            dbContext.Zaznamy.Where(z => z.PropertyId == property.Id).Sum(z => (decimal?)z.Cost) ?? 0,
            ToRoleString(propertyMember?.Role ?? projectMember?.Role ?? MemberRole.Viewer, project.OwnerId == userId),
            project.OwnerId != userId,
            ToSyncModeString(property.SyncMode),
            ToSyncStatusString(property.SyncStatus),
            property.CoverMediaId,
            coverUrl,
            property.CreatedAt,
            property.UpdatedAt);

        return Results.Ok(dto);
    }

    private static async Task<IResult> UpdatePropertyAsync(
        ClaimsPrincipal user,
        Guid id,
        [FromBody] UpdatePropertyRequest request,
        IStorageService storageService,
        ApplicationDbContext dbContext)
    {
        var userId = user.GetUserId();
        if (userId == Guid.Empty)
            return Results.Unauthorized();

        var property = await dbContext.Properties.FirstOrDefaultAsync(p => p.Id == id);
        if (property is null)
            return Results.NotFound();

        if (!await HasPropertyAccessAsync(dbContext, property.Id, userId))
            return Results.Forbid();

        if (!string.IsNullOrWhiteSpace(request.Name))
            property.Name = request.Name;
        if (request.Description is not null)
            property.Description = request.Description;
        if (!string.IsNullOrWhiteSpace(request.PropertyType))
            property.PropertyType = Enum.TryParse<PropertyType>(request.PropertyType, true, out var parsed) ? parsed : property.PropertyType;
        if (request.Latitude.HasValue)
            property.Latitude = request.Latitude;
        if (request.Longitude.HasValue)
            property.Longitude = request.Longitude;
        if (request.GeoRadius.HasValue)
            property.GeoRadius = request.GeoRadius.Value;

        property.UpdatedAt = DateTime.UtcNow;
        await dbContext.SaveChangesAsync();

        var project = await dbContext.Projects.FirstAsync(p => p.Id == property.ProjectId);
        var coverUrl = await GetCoverUrlAsync(dbContext, storageService, OwnerType.Property, property.Id, property.CoverMediaId);

        var dto = new PropertyDto(
            property.Id,
            property.ProjectId,
            project.Name,
            property.Name,
            property.Description,
            property.PropertyType.ToString().ToLowerInvariant(),
            property.Latitude,
            property.Longitude,
            property.GeoRadius,
            dbContext.Units.Count(u => u.PropertyId == property.Id),
            dbContext.Zaznamy.Count(z => z.PropertyId == property.Id),
            dbContext.Zaznamy.Where(z => z.PropertyId == property.Id).Sum(z => (decimal?)z.Cost) ?? 0,
            project.OwnerId == userId ? "owner" : "editor",
            project.OwnerId != userId,
            ToSyncModeString(property.SyncMode),
            ToSyncStatusString(property.SyncStatus),
            property.CoverMediaId,
            coverUrl,
            property.CreatedAt,
            property.UpdatedAt);

        return Results.Ok(dto);
    }

    private static async Task<IResult> UpdatePropertyCoverAsync(
        ClaimsPrincipal user,
        Guid id,
        [FromBody] CoverMediaRequest request,
        IStorageService storageService,
        ApplicationDbContext dbContext)
    {
        var userId = user.GetUserId();
        if (userId == Guid.Empty)
            return Results.Unauthorized();

        var property = await dbContext.Properties.FirstOrDefaultAsync(p => p.Id == id);
        if (property is null)
            return Results.NotFound();

        if (!await HasPropertyAccessAsync(dbContext, property.Id, userId))
            return Results.Forbid();

        Media? coverMedia = null;
        if (request.CoverMediaId.HasValue)
        {
            coverMedia = await dbContext.Media.FirstOrDefaultAsync(m => m.Id == request.CoverMediaId.Value);
            if (coverMedia is null
                || coverMedia.OwnerType != OwnerType.Property
                || coverMedia.OwnerId != property.Id
                || coverMedia.Type != MediaType.Photo)
                return Results.BadRequest();

            property.CoverMediaId = coverMedia.Id;
        }
        else
        {
            property.CoverMediaId = null;
        }

        property.UpdatedAt = DateTime.UtcNow;
        await dbContext.SaveChangesAsync();

        var project = await dbContext.Projects.FirstAsync(p => p.Id == property.ProjectId);
        var propertyMember = await dbContext.PropertyMembers.FirstOrDefaultAsync(pm => pm.PropertyId == property.Id && pm.UserId == userId);
        var projectMember = await dbContext.ProjectMembers.FirstOrDefaultAsync(pm => pm.ProjectId == property.ProjectId && pm.UserId == userId);
        var coverUrl = coverMedia is null ? null : storageService.GetThumbnailUrl(coverMedia.StorageKey);

        var dto = new PropertyDto(
            property.Id,
            property.ProjectId,
            project.Name,
            property.Name,
            property.Description,
            property.PropertyType.ToString().ToLowerInvariant(),
            property.Latitude,
            property.Longitude,
            property.GeoRadius,
            dbContext.Units.Count(u => u.PropertyId == property.Id),
            dbContext.Zaznamy.Count(z => z.PropertyId == property.Id),
            dbContext.Zaznamy.Where(z => z.PropertyId == property.Id).Sum(z => (decimal?)z.Cost) ?? 0,
            ToRoleString(propertyMember?.Role ?? projectMember?.Role ?? MemberRole.Viewer, project.OwnerId == userId),
            project.OwnerId != userId,
            ToSyncModeString(property.SyncMode),
            ToSyncStatusString(property.SyncStatus),
            property.CoverMediaId,
            coverUrl,
            property.CreatedAt,
            property.UpdatedAt);

        return Results.Ok(dto);
    }

    private static async Task<IResult> DeletePropertyAsync(
        ClaimsPrincipal user,
        Guid id,
        ApplicationDbContext dbContext)
    {
        var userId = user.GetUserId();
        if (userId == Guid.Empty)
            return Results.Unauthorized();

        var property = await dbContext.Properties.FirstOrDefaultAsync(p => p.Id == id);
        if (property is null)
            return Results.NotFound();

        var project = await dbContext.Projects.FirstAsync(p => p.Id == property.ProjectId);
        if (project.OwnerId != userId)
            return Results.Forbid();

        property.IsDeleted = true;
        property.DeletedAt = DateTime.UtcNow;
        await dbContext.SaveChangesAsync();

        return Results.NoContent();
    }

    private static async Task<IResult> GetStatsAsync(
        ClaimsPrincipal user,
        Guid id,
        ApplicationDbContext dbContext)
    {
        var userId = user.GetUserId();
        if (userId == Guid.Empty)
            return Results.Unauthorized();

        if (!await HasPropertyAccessAsync(dbContext, id, userId))
            return Results.Forbid();

        var totalCost = await dbContext.Zaznamy.Where(z => z.PropertyId == id).SumAsync(z => (decimal?)z.Cost) ?? 0;
        var zaznamCount = await dbContext.Zaznamy.CountAsync(z => z.PropertyId == id);
        var draftCount = await dbContext.Zaznamy.CountAsync(z => z.PropertyId == id && z.Status == ZaznamStatus.Draft);
        var documentCount = await dbContext.Media
            .Where(d => d.OwnerType == OwnerType.Zaznam)
            .Join(dbContext.Zaznamy, d => d.OwnerId, z => z.Id, (d, z) => new { d, z })
            .Where(x => x.z.PropertyId == id)
            .CountAsync();

        var costByMonth = await dbContext.Zaznamy
            .Where(z => z.PropertyId == id && z.Cost.HasValue)
            .GroupBy(z => new { z.Date.Year, z.Date.Month })
            .Select(g => new CostByMonth(
                $"{g.Key.Year:D4}-{g.Key.Month:D2}",
                g.Sum(z => (decimal?)z.Cost) ?? 0))
            .OrderBy(x => x.Month)
            .ToListAsync();

        var costByYear = await dbContext.Zaznamy
            .Where(z => z.PropertyId == id && z.Cost.HasValue)
            .GroupBy(z => z.Date.Year)
            .Select(g => new CostByYear(
                g.Key,
                g.Sum(z => (decimal?)z.Cost) ?? 0))
            .OrderBy(x => x.Year)
            .ToListAsync();

        var stats = new PropertyStatsResponse(
            totalCost,
            zaznamCount,
            draftCount,
            documentCount,
            costByMonth,
            costByYear);

        return Results.Ok(stats);
    }

    private static async Task<IResult> GetMembersAsync(
        ClaimsPrincipal user,
        Guid id,
        ApplicationDbContext dbContext)
    {
        var userId = user.GetUserId();
        if (userId == Guid.Empty)
            return Results.Unauthorized();

        if (!await HasPropertyAccessAsync(dbContext, id, userId))
            return Results.Forbid();

        var members = await dbContext.PropertyMembers
            .Where(m => m.PropertyId == id)
            .Join(dbContext.Users, m => m.UserId, u => u.Id, (m, u) => new MemberDto(
                u.Id,
                u.Email ?? string.Empty,
                $"{u.FirstName} {u.LastName}".Trim(),
                null,
                m.Role.ToString().ToLowerInvariant(),
                m.PermissionsJson == null ? null : System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, bool>>(m.PermissionsJson),
                "active",
                m.CreatedAt))
            .ToListAsync();

        return Results.Ok(members);
    }

    private static async Task<IResult> AddMemberAsync(
        ClaimsPrincipal user,
        HttpRequest httpRequest,
        Guid id,
        [FromBody] AddMemberRequest request,
        ApplicationDbContext dbContext,
        IEmailDispatcher emailDispatcher,
        EmailTemplateService emailTemplates,
        IConfiguration configuration,
        CancellationToken ct)
    {
        var userId = user.GetUserId();
        if (userId == Guid.Empty)
            return Results.Unauthorized();

        var property = await dbContext.Properties.FirstOrDefaultAsync(p => p.Id == id);
        if (property is null)
            return Results.NotFound();

        var project = await dbContext.Projects.FirstAsync(p => p.Id == property.ProjectId);
        if (project.OwnerId != userId)
            return Results.Forbid();

        var token = InvitationTokenHelper.CreateToken();
        var inviteUrl = InvitationTokenHelper.BuildInviteUrl(httpRequest, configuration, token.Token);

        var invitation = new Invitation
        {
            Id = Guid.NewGuid(),
            TokenHash = token.TokenHash,
            TargetType = InvitationTargetType.Property,
            TargetId = property.Id,
            Email = request.Email,
            Role = ToRole(request.Role),
            PermissionsJson = request.Permissions is null ? null : System.Text.Json.JsonSerializer.Serialize(request.Permissions),
            Status = InvitationStatus.Pending,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            CreatedAt = DateTime.UtcNow,
            CreatedBy = userId
        };

        dbContext.Invitations.Add(invitation);

        var normalizedEmail = NormalizeEmail(request.Email);
        if (normalizedEmail is not null)
        {
            var recipient = await dbContext.Users.FirstOrDefaultAsync(u => u.NormalizedEmail == normalizedEmail);
            if (recipient is not null && recipient.Id != userId)
            {
                ActivityNotificationHelper.AddNotification(dbContext, recipient.Id, NotificationType.InvitationReceived, new
                {
                    invitationId = invitation.Id,
                    targetType = "property",
                    targetId = property.Id,
                    targetName = property.Name,
                    role = ToRoleString(invitation.Role, isOwner: false),
                    invitedByUserId = userId
                });
            }
        }

        await dbContext.SaveChangesAsync();

        await InvitationEmailHelper.SendInvitationAsync(
            dbContext,
            emailDispatcher,
            emailTemplates,
            invitation,
            inviteUrl,
            property.Name,
            ct);

        return Results.Accepted(value: new InvitationLinkResponse(invitation.Id, inviteUrl, invitation.ExpiresAt));
    }

    private static async Task<IResult> UpdateMemberAsync(
        ClaimsPrincipal user,
        Guid id,
        Guid userId,
        [FromBody] UpdateMemberRequest request,
        ApplicationDbContext dbContext)
    {
        var actorId = user.GetUserId();
        if (actorId == Guid.Empty)
            return Results.Unauthorized();

        var property = await dbContext.Properties.FirstOrDefaultAsync(p => p.Id == id);
        if (property is null)
            return Results.NotFound();

        var project = await dbContext.Projects.FirstAsync(p => p.Id == property.ProjectId);
        if (project.OwnerId != actorId)
            return Results.Forbid();

        var member = await dbContext.PropertyMembers.FirstOrDefaultAsync(m => m.PropertyId == id && m.UserId == userId);
        if (member is null)
            return Results.NotFound();

        var roleChanged = false;
        if (!string.IsNullOrWhiteSpace(request.Role))
        {
            var parsed = ToRole(request.Role);
            if (member.Role != parsed)
            {
                member.Role = parsed;
                roleChanged = true;
            }
        }
        if (request.Permissions is not null)
            member.PermissionsJson = System.Text.Json.JsonSerializer.Serialize(request.Permissions);

        if (roleChanged)
        {
            ActivityNotificationHelper.AddActivity(
                dbContext,
                property.Id,
                actorId,
                ActivityType.MemberRoleChanged,
                "member",
                userId,
                new { userId, role = ToRoleString(member.Role, isOwner: false) });
        }

        await dbContext.SaveChangesAsync();
        return Results.NoContent();
    }

    private static async Task<IResult> RemoveMemberAsync(
        ClaimsPrincipal user,
        Guid id,
        Guid userId,
        ApplicationDbContext dbContext)
    {
        var actorId = user.GetUserId();
        if (actorId == Guid.Empty)
            return Results.Unauthorized();

        var property = await dbContext.Properties.FirstOrDefaultAsync(p => p.Id == id);
        if (property is null)
            return Results.NotFound();

        var project = await dbContext.Projects.FirstAsync(p => p.Id == property.ProjectId);
        if (project.OwnerId != actorId)
            return Results.Forbid();

        var member = await dbContext.PropertyMembers.FirstOrDefaultAsync(m => m.PropertyId == id && m.UserId == userId);
        if (member is null)
            return Results.NotFound();

        dbContext.PropertyMembers.Remove(member);
        ActivityNotificationHelper.AddActivity(
            dbContext,
            property.Id,
            actorId,
            ActivityType.MemberLeft,
            "member",
            userId,
            new { userId });
        await dbContext.SaveChangesAsync();
        return Results.NoContent();
    }

    private static async Task<IResult> LeavePropertyAsync(
        ClaimsPrincipal user,
        Guid id,
        ApplicationDbContext dbContext)
    {
        var userId = user.GetUserId();
        if (userId == Guid.Empty)
            return Results.Unauthorized();

        var member = await dbContext.PropertyMembers.FirstOrDefaultAsync(m => m.PropertyId == id && m.UserId == userId);
        if (member is null)
            return Results.NotFound();

        dbContext.PropertyMembers.Remove(member);
        ActivityNotificationHelper.AddActivity(
            dbContext,
            member.PropertyId,
            userId,
            ActivityType.MemberLeft,
            "member",
            userId,
            new { userId });
        await dbContext.SaveChangesAsync();
        return Results.NoContent();
    }

    private static async Task<IResult> GetActivityAsync(
        ClaimsPrincipal user,
        Guid id,
        int? page,
        int? pageSize,
        IStorageService storageService,
        ApplicationDbContext dbContext)
    {
        var userId = user.GetUserId();
        if (userId == Guid.Empty)
            return Results.Unauthorized();

        if (!await HasPropertyAccessAsync(dbContext, id, userId))
            return Results.Forbid();

        var currentPage = Math.Max(page ?? 1, 1);
        var size = Math.Clamp(pageSize ?? 20, 1, 50);

        var query = dbContext.Activities.Where(a => a.PropertyId == id);
        var total = await query.CountAsync();

        var items = await query
            .OrderByDescending(a => a.CreatedAt)
            .Skip((currentPage - 1) * size)
            .Take(size)
            .ToListAsync();

        var actorIds = items.Select(a => a.ActorUserId).Distinct().ToList();
        var actors = await dbContext.Users
            .Where(u => actorIds.Contains(u.Id))
            .Select(u => new { u.Id, Name = ($"{u.FirstName} {u.LastName}").Trim(), u.AvatarStorageKey })
            .ToListAsync();

        var actorLookup = actors.ToDictionary(a => a.Id, a => a);

        var dtos = items.Select(a =>
        {
            actorLookup.TryGetValue(a.ActorUserId, out var actor);
            var actorDto = new ActivityActorDto(
                a.ActorUserId,
                string.IsNullOrWhiteSpace(actor?.Name) ? "Unknown" : actor.Name,
                string.IsNullOrWhiteSpace(actor?.AvatarStorageKey)
                    ? null
                    : storageService.GetPublicUrl(actor.AvatarStorageKey));

            return new ActivityDto(
                a.Id,
                ToActivityType(a.Type),
                actorDto,
                a.TargetType,
                a.TargetId,
                DeserializeMetadata(a.MetadataJson),
                a.CreatedAt);
        }).ToList();

        return Results.Ok(new ActivityListResponse(dtos, total));
    }

    private static IReadOnlyDictionary<string, object>? DeserializeMetadata(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return null;

        return JsonSerializer.Deserialize<Dictionary<string, object>>(json);
    }

    private static string ToActivityType(ActivityType type)
    {
        return type switch
        {
            ActivityType.ZaznamCreated => "zaznam_created",
            ActivityType.ZaznamUpdated => "zaznam_updated",
            ActivityType.ZaznamDeleted => "zaznam_deleted",
            ActivityType.CommentAdded => "comment_added",
            ActivityType.MemberJoined => "member_joined",
            ActivityType.MemberLeft => "member_left",
            ActivityType.MemberRoleChanged => "member_role_changed",
            _ => "unknown"
        };
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

    private static async Task<bool> HasProjectAccessAsync(ApplicationDbContext dbContext, Guid projectId, Guid userId)
    {
        return await dbContext.Projects.AnyAsync(p => p.Id == projectId && p.OwnerId == userId)
            || await dbContext.ProjectMembers.AnyAsync(m => m.ProjectId == projectId && m.UserId == userId);
    }

    private static async Task<bool> HasPropertyAccessAsync(ApplicationDbContext dbContext, Guid propertyId, Guid userId)
    {
        return await dbContext.Properties.AnyAsync(p => p.Id == propertyId &&
            (dbContext.Projects.Any(pr => pr.Id == p.ProjectId && pr.OwnerId == userId)
             || dbContext.ProjectMembers.Any(pm => pm.ProjectId == p.ProjectId && pm.UserId == userId)
             || dbContext.PropertyMembers.Any(pm => pm.PropertyId == p.Id && pm.UserId == userId)));
    }

    private static string ToRoleString(MemberRole role, bool isOwner)
    {
        return isOwner ? "owner" : role.ToString().ToLowerInvariant();
    }

    private static string ToSyncModeString(SyncMode mode)
    {
        return mode == SyncMode.Synced ? "synced" : "local-only";
    }

    private static string ToSyncStatusString(SyncStatus status)
    {
        return status.ToString().ToLowerInvariant();
    }

    private static MemberRole ToRole(string role)
    {
        return Enum.TryParse<MemberRole>(role, true, out var parsed) ? parsed : MemberRole.Viewer;
    }

    private static string? NormalizeEmail(string? email)
    {
        return string.IsNullOrWhiteSpace(email)
            ? null
            : email.Trim().ToUpperInvariant();
    }
}

