using System.Security.Claims;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using MujDomecek.API.Extensions;
using MujDomecek.Application.DTOs;
using MujDomecek.Domain.Aggregates.Notifications;
using MujDomecek.Domain.ValueObjects;
using MujDomecek.Infrastructure.Persistence;

namespace MujDomecek.API.Endpoints;

public static class NotificationEndpoints
{
    public static IEndpointRouteBuilder MapNotificationEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/notifications").RequireAuthorization();

        group.MapGet("/", GetNotificationsAsync);
        group.MapGet("/unread-count", GetUnreadCountAsync);
        group.MapPost("/{id:guid}/read", MarkReadAsync);
        group.MapPost("/read-all", MarkAllReadAsync);
        group.MapDelete("/{id:guid}", DeleteNotificationAsync);

        return endpoints;
    }

    private static async Task<IResult> GetNotificationsAsync(
        ClaimsPrincipal user,
        bool? unreadOnly,
        int? page,
        int? pageSize,
        ApplicationDbContext dbContext)
    {
        var userId = user.GetUserId();
        if (userId == Guid.Empty)
            return Results.Unauthorized();

        var currentPage = Math.Max(page ?? 1, 1);
        var size = Math.Clamp(pageSize ?? 20, 1, 100);

        var query = dbContext.Notifications
            .Where(n => n.UserId == userId);

        if (unreadOnly == true)
            query = query.Where(n => n.ReadAt == null);

        var total = await query.CountAsync();
        var unreadCount = await dbContext.Notifications.CountAsync(n => n.UserId == userId && n.ReadAt == null);

        var items = await query
            .OrderByDescending(n => n.CreatedAt)
            .Skip((currentPage - 1) * size)
            .Take(size)
            .ToListAsync();

        var dtos = items.Select(ToNotificationDto).ToList();
        return Results.Ok(new NotificationListResponse(dtos, total, unreadCount));
    }

    private static async Task<IResult> GetUnreadCountAsync(
        ClaimsPrincipal user,
        ApplicationDbContext dbContext)
    {
        var userId = user.GetUserId();
        if (userId == Guid.Empty)
            return Results.Unauthorized();

        var unreadCount = await dbContext.Notifications
            .CountAsync(n => n.UserId == userId && n.ReadAt == null);

        return Results.Ok(new { unreadCount });
    }

    private static async Task<IResult> MarkReadAsync(
        ClaimsPrincipal user,
        Guid id,
        ApplicationDbContext dbContext)
    {
        var userId = user.GetUserId();
        if (userId == Guid.Empty)
            return Results.Unauthorized();

        var notification = await dbContext.Notifications
            .FirstOrDefaultAsync(n => n.Id == id && n.UserId == userId);
        if (notification is null)
            return Results.NotFound();

        notification.ReadAt ??= DateTime.UtcNow;
        await dbContext.SaveChangesAsync();

        return Results.NoContent();
    }

    private static async Task<IResult> MarkAllReadAsync(
        ClaimsPrincipal user,
        ApplicationDbContext dbContext)
    {
        var userId = user.GetUserId();
        if (userId == Guid.Empty)
            return Results.Unauthorized();

        var unread = await dbContext.Notifications
            .Where(n => n.UserId == userId && n.ReadAt == null)
            .ToListAsync();

        var now = DateTime.UtcNow;
        foreach (var notification in unread)
            notification.ReadAt = now;

        await dbContext.SaveChangesAsync();
        return Results.NoContent();
    }

    private static async Task<IResult> DeleteNotificationAsync(
        ClaimsPrincipal user,
        Guid id,
        ApplicationDbContext dbContext)
    {
        var userId = user.GetUserId();
        if (userId == Guid.Empty)
            return Results.Unauthorized();

        var notification = await dbContext.Notifications
            .FirstOrDefaultAsync(n => n.Id == id && n.UserId == userId);
        if (notification is null)
            return Results.NotFound();

        dbContext.Notifications.Remove(notification);
        await dbContext.SaveChangesAsync();

        return Results.NoContent();
    }

    private static NotificationDto ToNotificationDto(Notification notification)
    {
        return new NotificationDto(
            notification.Id,
            ToApiType(notification.Type),
            DeserializePayload(notification.PayloadJson),
            notification.ReadAt,
            notification.CreatedAt);
    }

    private static IReadOnlyDictionary<string, object> DeserializePayload(string payloadJson)
    {
        if (string.IsNullOrWhiteSpace(payloadJson))
            return new Dictionary<string, object>();

        return JsonSerializer.Deserialize<Dictionary<string, object>>(payloadJson)
            ?? [];
    }

    private static string ToApiType(NotificationType type)
    {
        return type switch
        {
            NotificationType.InvitationReceived => "invitation_received",
            NotificationType.InvitationAccepted => "invitation_accepted",
            NotificationType.InvitationExpired => "invitation_expired",
            NotificationType.InvitationDeclined => "invitation_declined",
            NotificationType.MentionInComment => "mention_in_comment",
            NotificationType.CommentOnYourZaznam => "comment_on_zaznam",
            NotificationType.DraftReminder => "draft_reminder",
            NotificationType.DraftExpiring => "draft_expiring",
            _ => "unknown"
        };
    }
}
