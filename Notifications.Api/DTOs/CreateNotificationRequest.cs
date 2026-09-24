namespace Notifications.Api.DTOs;

public record CreateNotificationRequest(string Recipient, string Message, string Channel);
