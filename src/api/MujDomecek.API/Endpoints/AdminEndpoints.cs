using System.Diagnostics;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using MujDomecek.API.Extensions;
using MujDomecek.Application.DTOs;
using MujDomecek.Domain.Aggregates.Admin;
using MujDomecek.Domain.Aggregates.Audit;
using MujDomecek.Domain.Aggregates.User;
using MujDomecek.Domain.Aggregates.Zaznam;
using MujDomecek.Domain.ValueObjects;
using MujDomecek.Infrastructure.Persistence;
using Quartz;
using Quartz.Impl.Matchers;

namespace MujDomecek.API.Endpoints;

public static class AdminEndpoints
{
    private const int DefaultPageSize = 20;
    private const int MaxPageSize = 100;
    private const int MaxAuditPageSize = 100;

    public static IEndpointRouteBuilder MapAdminEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/admin")
            .RequireAuthorization(new AuthorizeAttribute { Roles = "Admin" });

        group.MapGet("/stats/dashboard", GetDashboardAsync);
        group.MapGet("/stats/health", GetHealthAsync);
        group.MapGet("/users", GetUsersAsync);
        group.MapGet("/users/{id:guid}", GetUserAsync);
        group.MapPost("/users/{id:guid}/block", BlockUserAsync);
        group.MapPost("/users/{id:guid}/unblock", UnblockUserAsync);
        group.MapDelete("/users/{id:guid}", DeleteUserAsync);
        group.MapGet("/users/{id:guid}/sessions", GetUserSessionsAsync);
        group.MapDelete("/users/{id:guid}/sessions", RevokeUserSessionsAsync);

        group.MapGet("/tags", GetTagsAsync);
        group.MapPost("/tags", CreateTagAsync);
        group.MapPut("/tags/{id:int}", UpdateTagAsync);
        group.MapPost("/tags/{id:int}/deactivate", DeactivateTagAsync);
        group.MapPost("/tags/reorder", ReorderTagsAsync);

        group.MapGet("/settings", GetSettingsAsync);
        group.MapPut("/settings", UpdateSettingsAsync);

        group.MapGet("/audit", GetAuditAsync);
        group.MapGet("/audit/export", ExportAuditAsync);

        return endpoints;
    }
    private static async Task<IResult> GetDashboardAsync(ApplicationDbContext dbContext)
    {
        var now = DateTime.UtcNow;
        var usersQuery = dbContext.Users.IgnoreQueryFilters();

        var totalUsers = await usersQuery.CountAsync();
        var active7d = await usersQuery.CountAsync(u => u.LastLoginAt != null && u.LastLoginAt >= now.AddDays(-7));
        var blocked = await usersQuery.CountAsync(u => u.IsBlocked && !u.IsDeleted);

        var projects = await dbContext.Projects.CountAsync();
        var properties = await dbContext.Properties.CountAsync();
        var zaznamy = await dbContext.Zaznamy.CountAsync();
        var zaznamy24h = await dbContext.Zaznamy.CountAsync(z => z.CreatedAt >= now.AddHours(-24));

        var storageTotal = await dbContext.Media.SumAsync(d => (long?)d.SizeBytes) ?? 0;
        var storageCount = await dbContext.Media.CountAsync();
        var pendingInvitations = await dbContext.Invitations.CountAsync(i => i.Status == InvitationStatus.Pending);

        var response = new AdminDashboardResponse(
            new AdminDashboardUsers(totalUsers, active7d, blocked),
            new AdminDashboardContent(projects, properties, zaznamy, zaznamy24h),
            new AdminDashboardStorage(storageTotal, storageCount),
            new AdminDashboardInvitations(pendingInvitations));

        return Results.Ok(response);
    }
    private static async Task<IResult> GetHealthAsync(
        ApplicationDbContext dbContext,
        ISchedulerFactory schedulerFactory)
    {
        var stopwatch = Stopwatch.StartNew();
        var dbOk = await dbContext.Database.CanConnectAsync();
        stopwatch.Stop();

        var database = new HealthCheckItem(dbOk ? "healthy" : "unhealthy", dbOk ? (int)stopwatch.ElapsedMilliseconds : null);
        var storage = new HealthCheckItem("unknown", null);
        var email = new HealthCheckItem("unknown", null);
        var backgroundJobs = await GetBackgroundJobsStatusAsync(schedulerFactory);

        var status = dbOk ? "degraded" : "unhealthy";
        var response = new HealthCheckResponse(status, new HealthCheckDetails(database, storage, email, backgroundJobs));
        return Results.Ok(response);
    }
    private static async Task<IResult> GetUsersAsync(
        string? status,
        string? search,
        DateTime? from,
        DateTime? to,
        int? page,
        int? pageSize,
        string? sort,
        string? order,
        ApplicationDbContext dbContext)
    {
        var pageValue = page.GetValueOrDefault(1);
        if (pageValue < 1)
            pageValue = 1;

        var pageSizeValue = pageSize.GetValueOrDefault(DefaultPageSize);
        if (pageSizeValue < 1)
            pageSizeValue = DefaultPageSize;
        if (pageSizeValue > MaxPageSize)
            pageSizeValue = MaxPageSize;

        var query = dbContext.Users.IgnoreQueryFilters();
        if (!string.IsNullOrWhiteSpace(status))
        {
            var normalized = status.Trim().ToLowerInvariant();
            query = normalized switch
            {
                "active" => query.Where(u => !u.IsDeleted && !u.IsBlocked),
                "blocked" => query.Where(u => u.IsBlocked && !u.IsDeleted),
                "deleted" => query.Where(u => u.IsDeleted),
                _ => query
            };
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLowerInvariant();
            query = query.Where(u =>
                u.FirstName.ToLower().Contains(term)
                || u.LastName.ToLower().Contains(term)
                || (u.Email ?? string.Empty).ToLower().Contains(term));
        }

        if (from.HasValue)
            query = query.Where(u => u.CreatedAt >= from.Value);

        if (to.HasValue)
            query = query.Where(u => u.CreatedAt <= to.Value);

        query = ApplyUserSorting(query, sort, order);
        var total = await query.CountAsync();
        var users = await query
            .Skip((pageValue - 1) * pageSizeValue)
            .Take(pageSizeValue)
            .ToListAsync();

        var userIds = users.Select(u => u.Id).ToList();
        var projectCounts = await dbContext.Projects
            .Where(p => userIds.Contains(p.OwnerId))
            .GroupBy(p => p.OwnerId)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Key, x => x.Count);

        var items = users.Select(u => new AdminUserDto(
            u.Id,
            u.FirstName,
            u.LastName,
            u.Email ?? string.Empty,
            u.EmailConfirmed,
            null,
            GetUserStatus(u),
            projectCounts.TryGetValue(u.Id, out var count) ? count : 0,
            u.LastLoginAt,
            u.CreatedAt)).ToList();

        return Results.Ok(new AdminUserListResponse(items, total, pageValue, pageSizeValue));
    }
    private static async Task<IResult> GetUserAsync(
        Guid id,
        ApplicationDbContext dbContext)
    {
        var user = await dbContext.Users.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.Id == id);
        if (user is null)
            return Results.NotFound();

        var projectCount = await dbContext.Projects.CountAsync(p => p.OwnerId == user.Id);

        var dto = new AdminUserDto(
            user.Id,
            user.FirstName,
            user.LastName,
            user.Email ?? string.Empty,
            user.EmailConfirmed,
            null,
            GetUserStatus(user),
            projectCount,
            user.LastLoginAt,
            user.CreatedAt);

        return Results.Ok(dto);
    }
    private static async Task<IResult> BlockUserAsync(
        ClaimsPrincipal user,
        Guid id,
        [FromBody] BlockUserRequest request,
        ApplicationDbContext dbContext)
    {
        var adminId = user.GetUserId();
        if (adminId == Guid.Empty)
            return Results.Unauthorized();

        if (string.IsNullOrWhiteSpace(request.Reason))
            return Results.BadRequest();

        var target = await dbContext.Users.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.Id == id);
        if (target is null)
            return Results.NotFound();

        if (target.IsDeleted)
            return Results.BadRequest();

        target.IsBlocked = true;
        target.BlockedReason = request.Reason.Trim();

        var tokens = await dbContext.RefreshTokens
            .Where(t => t.UserId == id && t.RevokedAt == null)
            .ToListAsync();
        foreach (var token in tokens)
            token.RevokedAt = DateTime.UtcNow;

        AddAuditLog(dbContext, adminId, "user", target.Id, "update", new Dictionary<string, object?>
        {
            ["blocked"] = new { old = false, @new = true },
            ["reason"] = new { old = (string?)null, @new = target.BlockedReason }
        });

        await dbContext.SaveChangesAsync();
        return Results.NoContent();
    }
    private static async Task<IResult> UnblockUserAsync(
        ClaimsPrincipal user,
        Guid id,
        ApplicationDbContext dbContext)
    {
        var adminId = user.GetUserId();
        if (adminId == Guid.Empty)
            return Results.Unauthorized();

        var target = await dbContext.Users.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.Id == id);
        if (target is null)
            return Results.NotFound();

        if (!target.IsBlocked)
            return Results.NoContent();

        target.IsBlocked = false;
        target.BlockedReason = null;

        AddAuditLog(dbContext, adminId, "user", target.Id, "update", new Dictionary<string, object?>
        {
            ["blocked"] = new { old = true, @new = false }
        });

        await dbContext.SaveChangesAsync();
        return Results.NoContent();
    }
    private static async Task<IResult> DeleteUserAsync(
        ClaimsPrincipal user,
        Guid id,
        UserManager<AppUser> userManager,
        ApplicationDbContext dbContext)
    {
        var adminId = user.GetUserId();
        if (adminId == Guid.Empty)
            return Results.Unauthorized();

        var target = await dbContext.Users.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.Id == id);
        if (target is null)
            return Results.NotFound();

        if (target.IsDeleted)
            return Results.NoContent();

        var deletedEmail = $"deleted-{target.Id}@deleted.local";
        target.IsDeleted = true;
        target.DeletedAt = DateTime.UtcNow;
        target.IsBlocked = true;
        target.BlockedReason = "deleted";
        target.FirstName = "Deleted";
        target.LastName = "User";
        target.Email = deletedEmail;
        target.UserName = deletedEmail;
        target.NormalizedEmail = deletedEmail.ToUpperInvariant();
        target.NormalizedUserName = deletedEmail.ToUpperInvariant();
        target.EmailConfirmed = false;

        var tokens = await dbContext.RefreshTokens
            .Where(t => t.UserId == id && t.RevokedAt == null)
            .ToListAsync();
        foreach (var token in tokens)
            token.RevokedAt = DateTime.UtcNow;

        AddAuditLog(dbContext, adminId, "user", target.Id, "delete", new Dictionary<string, object?>
        {
            ["deleted"] = new { old = false, @new = true }
        });

        var result = await userManager.UpdateAsync(target);
        if (!result.Succeeded)
            return Results.BadRequest(result.Errors);

        return Results.NoContent();
    }
    private static async Task<IResult> GetUserSessionsAsync(
        Guid id,
        ApplicationDbContext dbContext)
    {
        var sessions = await dbContext.RefreshTokens
            .Where(t => t.UserId == id)
            .OrderByDescending(t => t.CreatedAt)
            .Select(t => new AdminSessionDto(
                t.Id,
                t.DeviceInfo,
                t.CreatedAt,
                t.ExpiresAt,
                t.RevokedAt))
            .ToListAsync();

        return Results.Ok(new AdminSessionListResponse(sessions));
    }

    private static async Task<IResult> RevokeUserSessionsAsync(
        ClaimsPrincipal user,
        Guid id,
        ApplicationDbContext dbContext)
    {
        var adminId = user.GetUserId();
        if (adminId == Guid.Empty)
            return Results.Unauthorized();

        var tokens = await dbContext.RefreshTokens
            .Where(t => t.UserId == id && t.RevokedAt == null)
            .ToListAsync();
        foreach (var token in tokens)
            token.RevokedAt = DateTime.UtcNow;

        AddAuditLog(dbContext, adminId, "refreshToken", id, "delete", new Dictionary<string, object?>
        {
            ["revoked"] = new { old = 0, @new = tokens.Count }
        });

        await dbContext.SaveChangesAsync();
        return Results.NoContent();
    }
    private static async Task<IResult> GetTagsAsync(ApplicationDbContext dbContext)
    {
        var tags = await dbContext.Tags
            .OrderBy(t => t.SortOrder)
            .ToListAsync();

        var usageCounts = await dbContext.ZaznamTags
            .GroupBy(t => t.TagId)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Key, x => x.Count);

        var items = tags.Select(tag => new TagDto(
            tag.Id,
            tag.Name,
            tag.Icon,
            tag.SortOrder,
            tag.IsActive,
            usageCounts.TryGetValue(tag.Id, out var count) ? count : 0)).ToList();

        return Results.Ok(items);
    }
    private static async Task<IResult> CreateTagAsync(
        ClaimsPrincipal user,
        [FromBody] CreateTagRequest request,
        ApplicationDbContext dbContext)
    {
        var adminId = user.GetUserId();
        if (adminId == Guid.Empty)
            return Results.Unauthorized();

        if (string.IsNullOrWhiteSpace(request.Name) || string.IsNullOrWhiteSpace(request.Icon))
            return Results.BadRequest();

        var name = request.Name.Trim();
        var icon = request.Icon.Trim();

        var exists = await dbContext.Tags.AnyAsync(t => t.Name.ToLower() == name.ToLower());
        if (exists)
            return Results.Conflict();

        var nextId = (short)((await dbContext.Tags.MaxAsync(t => (short?)t.Id) ?? 0) + 1);
        var nextSort = request.SortOrder ?? (short)((await dbContext.Tags.MaxAsync(t => (short?)t.SortOrder) ?? 0) + 1);

        var tag = new Tag
        {
            Id = nextId,
            Name = name,
            Icon = icon,
            SortOrder = nextSort,
            IsActive = true
        };

        dbContext.Tags.Add(tag);

        AddAuditLog(dbContext, adminId, "tag", Guid.Empty, "create", new Dictionary<string, object?>
        {
            ["id"] = new { old = (short?)null, @new = tag.Id },
            ["name"] = new { old = (string?)null, @new = tag.Name }
        });

        await dbContext.SaveChangesAsync();

        var dto = new TagDto(tag.Id, tag.Name, tag.Icon, tag.SortOrder, tag.IsActive, 0);
        return Results.Created($"/admin/tags/{tag.Id}", dto);
    }
    private static async Task<IResult> UpdateTagAsync(
        ClaimsPrincipal user,
        int id,
        [FromBody] UpdateTagRequest request,
        ApplicationDbContext dbContext)
    {
        var adminId = user.GetUserId();
        if (adminId == Guid.Empty)
            return Results.Unauthorized();

        if (!TryGetTagId(id, out var tagId))
            return Results.BadRequest();

        var tag = await dbContext.Tags.FirstOrDefaultAsync(t => t.Id == tagId);
        if (tag is null)
            return Results.NotFound();

        if (!string.IsNullOrWhiteSpace(request.Name))
        {
            var newName = request.Name.Trim();
            var exists = await dbContext.Tags.AnyAsync(t => t.Id != tag.Id && t.Name.ToLower() == newName.ToLower());
            if (exists)
                return Results.Conflict();

            tag.Name = newName;
        }

        if (!string.IsNullOrWhiteSpace(request.Icon))
            tag.Icon = request.Icon.Trim();

        if (request.SortOrder.HasValue)
            tag.SortOrder = request.SortOrder.Value;

        AddAuditLog(dbContext, adminId, "tag", Guid.Empty, "update", new Dictionary<string, object?>
        {
            ["id"] = new { old = (short?)null, @new = tag.Id }
        });

        await dbContext.SaveChangesAsync();

        var usageCount = await dbContext.ZaznamTags.CountAsync(t => t.TagId == tag.Id);
        var dto = new TagDto(tag.Id, tag.Name, tag.Icon, tag.SortOrder, tag.IsActive, usageCount);
        return Results.Ok(dto);
    }
    private static async Task<IResult> DeactivateTagAsync(
        ClaimsPrincipal user,
        int id,
        ApplicationDbContext dbContext)
    {
        var adminId = user.GetUserId();
        if (adminId == Guid.Empty)
            return Results.Unauthorized();

        if (!TryGetTagId(id, out var tagId))
            return Results.BadRequest();

        var tag = await dbContext.Tags.FirstOrDefaultAsync(t => t.Id == tagId);
        if (tag is null)
            return Results.NotFound();

        if (!tag.IsActive)
            return Results.NoContent();

        tag.IsActive = false;

        AddAuditLog(dbContext, adminId, "tag", Guid.Empty, "update", new Dictionary<string, object?>
        {
            ["active"] = new { old = true, @new = false }
        });

        await dbContext.SaveChangesAsync();
        return Results.NoContent();
    }
    private static async Task<IResult> ReorderTagsAsync(
        ClaimsPrincipal user,
        [FromBody] ReorderTagsRequest request,
        ApplicationDbContext dbContext)
    {
        var adminId = user.GetUserId();
        if (adminId == Guid.Empty)
            return Results.Unauthorized();

        if (request.Order is null || request.Order.Count == 0)
            return Results.BadRequest();

        var ids = request.Order.Select(o => o.Id).Distinct().ToList();
        var tags = await dbContext.Tags.Where(t => ids.Contains(t.Id)).ToListAsync();
        if (tags.Count != ids.Count)
            return Results.BadRequest();

        var orderMap = request.Order.ToDictionary(o => o.Id, o => o.SortOrder);
        foreach (var tag in tags)
        {
            if (orderMap.TryGetValue(tag.Id, out var sortOrder))
                tag.SortOrder = sortOrder;
        }

        AddAuditLog(dbContext, adminId, "tag", Guid.Empty, "update", new Dictionary<string, object?>
        {
            ["reorder"] = new { old = (string?)null, @new = "updated" }
        });

        await dbContext.SaveChangesAsync();
        return Results.NoContent();
    }
    private static async Task<IResult> GetSettingsAsync(
        IConfiguration configuration,
        ApplicationDbContext dbContext)
    {
        var defaults = GetDefaultSettings();
        var keys = defaults.Keys.ToList();

        var dbSettings = await dbContext.AppSettings
            .Where(s => keys.Contains(s.Key))
            .ToListAsync();

        var updatedByIds = dbSettings
            .Where(s => s.UpdatedByUserId.HasValue)
            .Select(s => s.UpdatedByUserId!.Value)
            .Distinct()
            .ToList();

        var updatedByUsers = updatedByIds.Count == 0
            ? []
            : await dbContext.Users.IgnoreQueryFilters()
                .Where(u => updatedByIds.Contains(u.Id))
                .ToDictionaryAsync(u => u.Id);

        var items = new List<AdminSettingDto>();
        foreach (var kvp in defaults)
        {
            var key = kvp.Key;
            var value = kvp.Value;
            var source = "default";
            DateTime? updatedAt = null;
            AdminSettingUpdatedBy? updatedBy = null;

            var configValue = configuration[key.Replace('.', ':')];
            if (!string.IsNullOrWhiteSpace(configValue))
            {
                value = ParseConfigValue(configValue);
                source = "appsettings";
            }

            var dbSetting = dbSettings.FirstOrDefault(s => s.Key == key);
            if (dbSetting is not null)
            {
                value = ParseJson(dbSetting.ValueJson);
                source = "db";
                updatedAt = dbSetting.UpdatedAt;
                if (dbSetting.UpdatedByUserId.HasValue
                    && updatedByUsers.TryGetValue(dbSetting.UpdatedByUserId.Value, out var updatedByUser))
                {
                    var name = $"{updatedByUser.FirstName} {updatedByUser.LastName}".Trim();
                    updatedBy = new AdminSettingUpdatedBy(updatedByUser.Id, name);
                }
            }

            items.Add(new AdminSettingDto(key, value, source, updatedAt, updatedBy));
        }

        var response = new AdminSettingsResponse(items.OrderBy(i => i.Key).ToList());
        return Results.Ok(response);
    }
    private static async Task<IResult> UpdateSettingsAsync(
        ClaimsPrincipal user,
        [FromBody] UpdateAdminSettingsRequest request,
        ApplicationDbContext dbContext)
    {
        var adminId = user.GetUserId();
        if (adminId == Guid.Empty)
            return Results.Unauthorized();

        if (request.Items is null || request.Items.Count == 0)
            return Results.BadRequest();

        var allowedKeys = GetAllowedSettingKeys();
        var invalidKeys = request.Items
            .Select(i => i.Key)
            .Where(key => !allowedKeys.Contains(key))
            .Distinct()
            .ToList();

        if (invalidKeys.Count > 0)
            return Results.BadRequest(new { invalidKeys });

        if (request.Items.Any(item => item.Value.ValueKind == JsonValueKind.Undefined))
            return Results.BadRequest();

        var now = DateTime.UtcNow;
        foreach (var item in request.Items)
        {
            var setting = await dbContext.AppSettings.FirstOrDefaultAsync(s => s.Key == item.Key);
            if (setting is null)
            {
                setting = new AppSetting { Key = item.Key };
                dbContext.AppSettings.Add(setting);
            }

            setting.ValueJson = item.Value.GetRawText();
            setting.UpdatedAt = now;
            setting.UpdatedByUserId = adminId;
        }

        AddAuditLog(dbContext, adminId, "appSetting", Guid.Empty, "update", new Dictionary<string, object?>
        {
            ["count"] = new { old = 0, @new = request.Items.Count }
        });

        await dbContext.SaveChangesAsync();
        return Results.NoContent();
    }
    private static async Task<IResult> GetAuditAsync(
        Guid? userId,
        string? entityType,
        string? action,
        DateTime? from,
        DateTime? to,
        int? page,
        int? pageSize,
        ApplicationDbContext dbContext)
    {
        var pageValue = page.GetValueOrDefault(1);
        if (pageValue < 1)
            pageValue = 1;

        var pageSizeValue = pageSize.GetValueOrDefault(DefaultPageSize);
        if (pageSizeValue < 1)
            pageSizeValue = DefaultPageSize;
        if (pageSizeValue > MaxAuditPageSize)
            pageSizeValue = MaxAuditPageSize;

        var query = ApplyAuditFilters(dbContext.AuditLogs.AsQueryable(), userId, entityType, action, from, to);
        var total = await query.CountAsync();

        var logs = await query
            .OrderByDescending(a => a.CreatedAt)
            .Skip((pageValue - 1) * pageSizeValue)
            .Take(pageSizeValue)
            .ToListAsync();

        var actorIds = logs.Select(l => l.ActorUserId).Distinct().ToList();
        var actors = actorIds.Count == 0
            ? []
            : await dbContext.Users.IgnoreQueryFilters()
                .Where(u => actorIds.Contains(u.Id))
                .ToDictionaryAsync(u => u.Id);

        var items = logs.Select(log => new AuditLogDto(
            log.Id,
            log.EntityType,
            log.EntityId,
            log.Action,
            BuildAuditActor(actors, log.ActorUserId),
            ParseDiffSummary(log.DiffSummaryJson),
            log.CreatedAt)).ToList();

        return Results.Ok(new AuditLogListResponse(items, total, pageValue, pageSizeValue));
    }
    private static async Task<IResult> ExportAuditAsync(
        Guid? userId,
        string? entityType,
        string? action,
        DateTime? from,
        DateTime? to,
        ApplicationDbContext dbContext)
    {
        var query = ApplyAuditFilters(dbContext.AuditLogs.AsQueryable(), userId, entityType, action, from, to);

        var logs = await query
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync();

        var actorIds = logs.Select(l => l.ActorUserId).Distinct().ToList();
        var actors = actorIds.Count == 0
            ? []
            : await dbContext.Users.IgnoreQueryFilters()
                .Where(u => actorIds.Contains(u.Id))
                .ToDictionaryAsync(u => u.Id);

        var csv = BuildAuditCsv(logs, actors);
        return Results.File(Encoding.UTF8.GetBytes(csv), "text/csv", "audit.csv");
    }
    private static IQueryable<AppUser> ApplyUserSorting(
        IQueryable<AppUser> query,
        string? sort,
        string? order)
    {
        var descending = !string.Equals(order, "asc", StringComparison.OrdinalIgnoreCase);
        var key = sort?.Trim().ToLowerInvariant();

        return key switch
        {
            "lastloginat" => descending
                ? query.OrderByDescending(u => u.LastLoginAt)
                : query.OrderBy(u => u.LastLoginAt),
            "name" => descending
                ? query.OrderByDescending(u => u.LastName).ThenByDescending(u => u.FirstName)
                : query.OrderBy(u => u.LastName).ThenBy(u => u.FirstName),
            _ => descending
                ? query.OrderByDescending(u => u.CreatedAt)
                : query.OrderBy(u => u.CreatedAt)
        };
    }

    private static string GetUserStatus(AppUser user)
    {
        if (user.IsDeleted)
            return "deleted";
        if (user.IsBlocked)
            return "blocked";
        return "active";
    }

    private static bool TryGetTagId(int id, out short tagId)
    {
        if (id < short.MinValue || id > short.MaxValue)
        {
            tagId = 0;
            return false;
        }

        tagId = (short)id;
        return true;
    }
    private static AuditActorDto BuildAuditActor(
        IReadOnlyDictionary<Guid, AppUser> actors,
        Guid actorId)
    {
        if (actors.TryGetValue(actorId, out var actor))
        {
            var name = $"{actor.FirstName} {actor.LastName}".Trim();
            return new AuditActorDto(actor.Id, name, actor.Email ?? string.Empty);
        }

        return new AuditActorDto(actorId, "Unknown", string.Empty);
    }

    private static IQueryable<AuditLog> ApplyAuditFilters(
        IQueryable<AuditLog> query,
        Guid? userId,
        string? entityType,
        string? action,
        DateTime? from,
        DateTime? to)
    {
        if (userId.HasValue && userId.Value != Guid.Empty)
            query = query.Where(a => a.ActorUserId == userId.Value);

        if (!string.IsNullOrWhiteSpace(entityType))
        {
            var normalized = entityType.Trim().ToLowerInvariant();
            query = query.Where(a => a.EntityType.ToLower() == normalized);
        }

        if (!string.IsNullOrWhiteSpace(action))
        {
            var normalized = action.Trim().ToLowerInvariant();
            query = query.Where(a => a.Action.ToLower() == normalized);
        }

        if (from.HasValue)
            query = query.Where(a => a.CreatedAt >= from.Value);

        if (to.HasValue)
            query = query.Where(a => a.CreatedAt <= to.Value);

        return query;
    }
    private static Dictionary<string, AuditDiff>? ParseDiffSummary(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return null;

        try
        {
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.ValueKind != JsonValueKind.Object)
                return null;

            var result = new Dictionary<string, AuditDiff>(StringComparer.OrdinalIgnoreCase);
            foreach (var prop in doc.RootElement.EnumerateObject())
            {
                if (prop.Value.ValueKind != JsonValueKind.Object)
                    continue;

                JsonElement? oldValue = null;
                JsonElement? newValue = null;

                if (prop.Value.TryGetProperty("old", out var oldProp))
                    oldValue = oldProp.Clone();
                if (prop.Value.TryGetProperty("new", out var newProp))
                    newValue = newProp.Clone();

                result[prop.Name] = new AuditDiff(oldValue, newValue);
            }

            return result.Count == 0 ? null : result;
        }
        catch (JsonException)
        {
            return null;
        }
    }
    private static string BuildAuditCsv(
        IReadOnlyList<AuditLog> logs,
        IReadOnlyDictionary<Guid, AppUser> actors)
    {
        var builder = new StringBuilder();
        builder.AppendLine("id,entityType,entityId,action,actorId,actorName,actorEmail,createdAt");

        foreach (var log in logs)
        {
            var actor = BuildAuditActor(actors, log.ActorUserId);
            var line = string.Join(",",
                CsvEscape(log.Id.ToString()),
                CsvEscape(log.EntityType),
                CsvEscape(log.EntityId.ToString()),
                CsvEscape(log.Action),
                CsvEscape(actor.Id.ToString()),
                CsvEscape(actor.Name),
                CsvEscape(actor.Email),
                CsvEscape(log.CreatedAt.ToString("O")));

            builder.AppendLine(line);
        }

        return builder.ToString();
    }

    private static string CsvEscape(string? value)
    {
        var text = value ?? string.Empty;
        if (text.Contains('"') || text.Contains(',') || text.Contains('\n') || text.Contains('\r'))
        {
            text = text.Replace("\"", "\"\"");
            return $"\"{text}\"";
        }

        return text;
    }
    private static void AddAuditLog(
        ApplicationDbContext dbContext,
        Guid actorUserId,
        string entityType,
        Guid entityId,
        string action,
        object? diffSummary)
    {
        var diffJson = diffSummary is null ? null : JsonSerializer.Serialize(diffSummary);

        dbContext.AuditLogs.Add(new AuditLog
        {
            Id = Guid.NewGuid(),
            EntityType = entityType,
            EntityId = entityId,
            Action = action,
            ActorUserId = actorUserId,
            DiffSummaryJson = diffJson,
            CreatedAt = DateTime.UtcNow
        });
    }

    private static async Task<HealthCheckJobs> GetBackgroundJobsStatusAsync(
        ISchedulerFactory schedulerFactory)
    {
        try
        {
            var scheduler = await schedulerFactory.GetScheduler();
            var jobKeys = await scheduler.GetJobKeys(GroupMatcher<JobKey>.AnyGroup());
            var triggerCount = 0;

            foreach (var jobKey in jobKeys)
            {
                var triggers = await scheduler.GetTriggersOfJob(jobKey);
                triggerCount += triggers.Count;
            }

            var status = scheduler.IsStarted ? "healthy" : "degraded";
            return new HealthCheckJobs(status, triggerCount);
        }
        catch (SchedulerException)
        {
            return new HealthCheckJobs("unknown", 0);
        }
    }
    private static Dictionary<string, JsonElement> GetDefaultSettings()
    {
        return new Dictionary<string, JsonElement>(StringComparer.OrdinalIgnoreCase)
        {
            ["Constraints.Zaznam.TitleMaxLength"] = JsonSerializer.SerializeToElement(200),
            ["Constraints.Zaznam.DescriptionMaxLength"] = JsonSerializer.SerializeToElement(5000),
            ["Constraints.Files.MaxPhotosPerZaznamFree"] = JsonSerializer.SerializeToElement(10),
            ["Constraints.Files.MaxPhotosPerZaznamPremium"] = JsonSerializer.SerializeToElement(50),
            ["Constraints.Files.MaxPhotoSizeBytes"] = JsonSerializer.SerializeToElement(10_485_760),
            ["Constraints.Files.MaxDocumentSizeBytes"] = JsonSerializer.SerializeToElement(20_971_520),
            ["Constraints.Files.PhotoCompression.FreeEnabled"] = JsonSerializer.SerializeToElement(true),
            ["Constraints.Files.PhotoCompression.FreeMaxDimensionPx"] = JsonSerializer.SerializeToElement(2000),
            ["Constraints.Files.PhotoCompression.FreeJpegQuality"] = JsonSerializer.SerializeToElement(80)
        };
    }

    private static HashSet<string> GetAllowedSettingKeys()
    {
        return new HashSet<string>(GetDefaultSettings().Keys, StringComparer.OrdinalIgnoreCase);
    }
    private static JsonElement ParseConfigValue(string raw)
    {
        if (TryParseJsonValue(raw, out var element))
            return element;

        return JsonSerializer.SerializeToElement(raw);
    }

    private static bool TryParseJsonValue(string raw, out JsonElement element)
    {
        try
        {
            using var doc = JsonDocument.Parse(raw);
            element = doc.RootElement.Clone();
            return true;
        }
        catch (JsonException)
        {
            element = default;
            return false;
        }
    }

    private static JsonElement ParseJson(string raw)
    {
        try
        {
            using var doc = JsonDocument.Parse(raw);
            return doc.RootElement.Clone();
        }
        catch (JsonException)
        {
            return JsonSerializer.SerializeToElement(raw);
        }
    }
}

