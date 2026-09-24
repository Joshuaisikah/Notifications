namespace Notifications.Api.DTOs;

public record NotificationStatusResponse(
    Guid Id,
    string Status,
    int AttemptCount,
    string? LastError,
    DateTime? NextRetryAt
);
