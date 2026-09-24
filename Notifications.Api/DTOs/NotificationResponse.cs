namespace Notifications.Api.DTOs;

public record NotificationResponse(
    Guid Id,
    string Recipient,
    string Message,
    string Channel,
    DateTime CreatedAt
);
