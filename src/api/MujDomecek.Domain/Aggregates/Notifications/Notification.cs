using MujDomecek.Domain.ValueObjects;

namespace MujDomecek.Domain.Aggregates.Notifications;

public sealed class Notification
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public NotificationType Type { get; set; }

    public string PayloadJson { get; set; } = string.Empty;

    public DateTime? ReadAt { get; set; }

    public DateTime CreatedAt { get; set; }
}
