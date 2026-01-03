using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using MujDomecek.API.Extensions;
using MujDomecek.API.Services;
using MujDomecek.Application.Abstractions;
using MujDomecek.Application.DTOs;
using MujDomecek.Domain.Aggregates.Project;
using MujDomecek.Domain.Aggregates.Zaznam;
using MujDomecek.Domain.ValueObjects;
using MujDomecek.Infrastructure.Persistence;

namespace MujDomecek.API.Endpoints;

public static class ProjectEndpoints
{
    public static IEndpointRouteBuilder MapProjectEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/projects").RequireAuthorization();

        group.MapGet("/", GetProjectsAsync);
        group.MapPost("/", CreateProjectAsync);
        group.MapGet("/{id:guid}", GetProjectAsync);
        group.MapPut("/{id:guid}", UpdateProjectAsync);
        group.MapDelete("/{id:guid}", DeleteProjectAsync);

        group.MapGet("/{id:guid}/members", GetMembersAsync);
        group.MapPost("/{id:guid}/members", AddMemberAsync);
        group.MapPut("/{id:guid}/members/{userId:guid}", UpdateMemberAsync);
        group.MapDelete("/{id:guid}/members/{userId:guid}", RemoveMemberAsync);
        group.MapPost("/{id:guid}/leave", LeaveProjectAsync);

        return endpoints;
    }

    private static async Task<IResult> GetProjectsAsync(
        ClaimsPrincipal user,
        ApplicationDbContext dbContext)
    {
        var userId = user.GetUserId();
        if (userId == Guid.Empty)
            return Results.Unauthorized();

        var projectIds = await dbContext.ProjectMembers
            .Where(m => m.UserId == userId)
            .Select(m => m.ProjectId)
            .ToListAsync();

        var projects = await dbContext.Projects
            .Where(p => p.OwnerId == userId || projectIds.Contains(p.Id))
            .Select(p => new ProjectDto(
                p.Id,
                p.Name,
                p.Description,
                dbContext.Properties.Count(pr => pr.ProjectId == p.Id),
                dbContext.ProjectMembers.Count(pm => pm.ProjectId == p.Id) + 1,
                p.OwnerId == userId
                    ? "owner"
                    : dbContext.ProjectMembers.Where(pm => pm.ProjectId == p.Id && pm.UserId == userId)
                        .Select(pm => ToRoleString(pm.Role))
                        .FirstOrDefault() ?? "viewer",
                ToSyncModeString(p.SyncMode),
                ToSyncStatusString(p.SyncStatus),
                p.CreatedAt,
                p.UpdatedAt))
            .ToListAsync();

        return Results.Ok(new ProjectListResponse(projects, projects.Count));
    }

    private static async Task<IResult> CreateProjectAsync(
        ClaimsPrincipal user,
        [FromBody] CreateProjectRequest request,
        ApplicationDbContext dbContext)
    {
        var userId = user.GetUserId();
        if (userId == Guid.Empty)
            return Results.Unauthorized();

        var project = new Project
        {
            Id = Guid.NewGuid(),
            OwnerId = userId,
            Name = request.Name,
            Description = request.Description,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        dbContext.Projects.Add(project);
        dbContext.ProjectMembers.Add(new ProjectMember
        {
            Id = Guid.NewGuid(),
            ProjectId = project.Id,
            UserId = userId,
            Role = MemberRole.Owner,
            CreatedAt = DateTime.UtcNow
        });

        await dbContext.SaveChangesAsync();

        var dto = new ProjectDto(
            project.Id,
            project.Name,
            project.Description,
            0,
            1,
            "owner",
            ToSyncModeString(project.SyncMode),
            ToSyncStatusString(project.SyncStatus),
            project.CreatedAt,
            project.UpdatedAt);

        return Results.Created($"/projects/{project.Id}", dto);
    }

    private static async Task<IResult> GetProjectAsync(
        ClaimsPrincipal user,
        Guid id,
        IStorageService storageService,
        ApplicationDbContext dbContext)
    {
        var userId = user.GetUserId();
        if (userId == Guid.Empty)
            return Results.Unauthorized();

        var project = await dbContext.Projects.FirstOrDefaultAsync(p => p.Id == id);
        if (project is null)
            return Results.NotFound();

        if (!await HasAccessAsync(dbContext, project.Id, userId))
            return Results.Forbid();

        var coverMediaIds = await dbContext.Properties
            .Where(p => p.ProjectId == project.Id && p.CoverMediaId.HasValue)
            .Select(p => p.CoverMediaId.GetValueOrDefault())
            .Distinct()
            .ToListAsync();

        Dictionary<Guid, string?> coverLookup = coverMediaIds.Count == 0
            ? new Dictionary<Guid, string?>()
            : await dbContext.Media
                .Where(m => coverMediaIds.Contains(m.Id) && m.OwnerType == OwnerType.Property)
                .Select(m => new { m.Id, m.StorageKey })
                .ToDictionaryAsync(m => m.Id, m => (string?)m.StorageKey);

        var properties = await dbContext.Properties
            .Where(p => p.ProjectId == project.Id)
            .Select(p => new PropertyDto(
                p.Id,
                p.ProjectId,
                project.Name,
                p.Name,
                p.Description,
                p.PropertyType.ToString().ToLowerInvariant(),
                p.Latitude,
                p.Longitude,
                p.GeoRadius,
                dbContext.Units.Count(u => u.PropertyId == p.Id),
                dbContext.Zaznamy.Count(z => z.PropertyId == p.Id),
            dbContext.Zaznamy.Where(z => z.PropertyId == p.Id).Sum(z => (decimal?)z.Cost) ?? 0,
            "owner",
            false,
            ToSyncModeString(p.SyncMode),
            ToSyncStatusString(p.SyncStatus),
            p.CoverMediaId,
            GetCoverUrl(storageService, coverLookup, p.CoverMediaId),
            p.CreatedAt,
            p.UpdatedAt))
            .ToListAsync();

        var members = await GetMemberDtosAsync(dbContext, project.Id);

        var detail = new ProjectDetailDto(
            project.Id,
            project.Name,
            project.Description,
            properties.Count,
            members.Count,
            project.OwnerId == userId ? "owner" : "editor",
            ToSyncModeString(project.SyncMode),
            ToSyncStatusString(project.SyncStatus),
            project.CreatedAt,
            project.UpdatedAt,
            properties,
            members);

        return Results.Ok(detail);
    }

    private static async Task<IResult> UpdateProjectAsync(
        ClaimsPrincipal user,
        Guid id,
        [FromBody] UpdateProjectRequest request,
        ApplicationDbContext dbContext)
    {
        var userId = user.GetUserId();
        if (userId == Guid.Empty)
            return Results.Unauthorized();

        var project = await dbContext.Projects.FirstOrDefaultAsync(p => p.Id == id);
        if (project is null)
            return Results.NotFound();

        if (project.OwnerId != userId)
            return Results.Forbid();

        if (!string.IsNullOrWhiteSpace(request.Name))
            project.Name = request.Name;
        if (request.Description is not null)
            project.Description = request.Description;

        project.UpdatedAt = DateTime.UtcNow;
        await dbContext.SaveChangesAsync();

        var dto = new ProjectDto(
            project.Id,
            project.Name,
            project.Description,
            dbContext.Properties.Count(p => p.ProjectId == project.Id),
            dbContext.ProjectMembers.Count(pm => pm.ProjectId == project.Id) + 1,
            "owner",
            ToSyncModeString(project.SyncMode),
            ToSyncStatusString(project.SyncStatus),
            project.CreatedAt,
            project.UpdatedAt);

        return Results.Ok(dto);
    }

    private static async Task<IResult> DeleteProjectAsync(
        ClaimsPrincipal user,
        Guid id,
        ApplicationDbContext dbContext)
    {
        var userId = user.GetUserId();
        if (userId == Guid.Empty)
            return Results.Unauthorized();

        var project = await dbContext.Projects.FirstOrDefaultAsync(p => p.Id == id);
        if (project is null)
            return Results.NotFound();

        if (project.OwnerId != userId)
            return Results.Forbid();

        project.IsDeleted = true;
        project.DeletedAt = DateTime.UtcNow;
        await dbContext.SaveChangesAsync();

        return Results.NoContent();
    }

    private static async Task<IResult> GetMembersAsync(
        ClaimsPrincipal user,
        Guid id,
        ApplicationDbContext dbContext)
    {
        var userId = user.GetUserId();
        if (userId == Guid.Empty)
            return Results.Unauthorized();

        if (!await HasAccessAsync(dbContext, id, userId))
            return Results.Forbid();

        var members = await GetMemberDtosAsync(dbContext, id);
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

        var project = await dbContext.Projects.FirstOrDefaultAsync(p => p.Id == id);
        if (project is null)
            return Results.NotFound();

        if (project.OwnerId != userId)
            return Results.Forbid();

        var token = InvitationTokenHelper.CreateToken();
        var inviteUrl = InvitationTokenHelper.BuildInviteUrl(httpRequest, configuration, token.Token);

        var invitation = new Invitation
        {
            Id = Guid.NewGuid(),
            TokenHash = token.TokenHash,
            TargetType = InvitationTargetType.Project,
            TargetId = project.Id,
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
                    targetType = "project",
                    targetId = project.Id,
                    targetName = project.Name,
                    role = ToRoleString(invitation.Role),
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
            project.Name,
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

        var project = await dbContext.Projects.FirstOrDefaultAsync(p => p.Id == id);
        if (project is null)
            return Results.NotFound();

        if (project.OwnerId != actorId)
            return Results.Forbid();

        var member = await dbContext.ProjectMembers.FirstOrDefaultAsync(m => m.ProjectId == id && m.UserId == userId);
        if (member is null)
            return Results.NotFound();

        if (!string.IsNullOrWhiteSpace(request.Role))
            member.Role = ToRole(request.Role);
        if (request.Permissions is not null)
            member.PermissionsJson = System.Text.Json.JsonSerializer.Serialize(request.Permissions);

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

        var project = await dbContext.Projects.FirstOrDefaultAsync(p => p.Id == id);
        if (project is null)
            return Results.NotFound();

        if (project.OwnerId != actorId)
            return Results.Forbid();

        var member = await dbContext.ProjectMembers.FirstOrDefaultAsync(m => m.ProjectId == id && m.UserId == userId);
        if (member is null)
            return Results.NotFound();

        dbContext.ProjectMembers.Remove(member);
        await dbContext.SaveChangesAsync();
        return Results.NoContent();
    }

    private static async Task<IResult> LeaveProjectAsync(
        ClaimsPrincipal user,
        Guid id,
        ApplicationDbContext dbContext)
    {
        var userId = user.GetUserId();
        if (userId == Guid.Empty)
            return Results.Unauthorized();

        var member = await dbContext.ProjectMembers.FirstOrDefaultAsync(m => m.ProjectId == id && m.UserId == userId);
        if (member is null)
            return Results.NotFound();

        dbContext.ProjectMembers.Remove(member);
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

    private static async Task<bool> HasAccessAsync(ApplicationDbContext dbContext, Guid projectId, Guid userId)
    {
        return await dbContext.Projects.AnyAsync(p => p.Id == projectId && p.OwnerId == userId)
            || await dbContext.ProjectMembers.AnyAsync(m => m.ProjectId == projectId && m.UserId == userId);
    }

    private static async Task<List<MemberDto>> GetMemberDtosAsync(ApplicationDbContext dbContext, Guid projectId)
    {
        var members = await dbContext.ProjectMembers
            .Where(m => m.ProjectId == projectId)
            .Join(dbContext.Users, m => m.UserId, u => u.Id, (m, u) => new MemberDto(
                u.Id,
                u.Email ?? string.Empty,
                $"{u.FirstName} {u.LastName}".Trim(),
                null,
                ToRoleString(m.Role),
                m.PermissionsJson == null ? null : System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, bool>>(m.PermissionsJson),
                "active",
                m.CreatedAt))
            .ToListAsync();

        return members;
    }

    private static string ToRoleString(MemberRole role) => role.ToString().ToLowerInvariant();

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

