namespace MujDomecek.Application.DTOs;

public sealed record NotificationDto(
    Guid Id,
    string Type,
    IReadOnlyDictionary<string, object> Payload,
    DateTime? ReadAt,
    DateTime CreatedAt
);

public sealed record NotificationListResponse(
    IReadOnlyList<NotificationDto> Items,
    int Total,
    int UnreadCount
);
