using System.Text.Json;
using MujDomecek.Domain.Aggregates.Notifications;
using MujDomecek.Domain.Aggregates.Property;
using MujDomecek.Domain.ValueObjects;
using MujDomecek.Infrastructure.Persistence;

namespace MujDomecek.API.Services;

public static class ActivityNotificationHelper
{
    public static void AddActivity(
        ApplicationDbContext dbContext,
        Guid propertyId,
        Guid actorUserId,
        ActivityType type,
        string? targetType,
        Guid? targetId,
        object? metadata)
    {
        dbContext.Activities.Add(new Activity
        {
            Id = Guid.NewGuid(),
            PropertyId = propertyId,
            ActorUserId = actorUserId,
            Type = type,
            TargetType = targetType,
            TargetId = targetId,
            MetadataJson = metadata is null ? null : JsonSerializer.Serialize(metadata),
            CreatedAt = DateTime.UtcNow
        });
    }

    public static void AddNotification(
        ApplicationDbContext dbContext,
        Guid userId,
        NotificationType type,
        object? payload)
    {
        dbContext.Notifications.Add(new Notification
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Type = type,
            PayloadJson = payload is null ? "{}" : JsonSerializer.Serialize(payload),
            CreatedAt = DateTime.UtcNow
        });
    }
}
