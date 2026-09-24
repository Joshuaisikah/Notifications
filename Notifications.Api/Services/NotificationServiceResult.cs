using Notifications.Api.Enums;

namespace Notifications.Api.Services;

public class NotificationServiceResult<T>
{
    public bool IsSuccess { get; set; }
    public T? Value { get; set; }
    public NotificationServiceError? Error { get; }

    private NotificationServiceResult(bool isSuccess, T? value, NotificationServiceError? error)
    {
        IsSuccess = isSuccess;
        Value = value;
        Error = error;
    }

    public static NotificationServiceResult<T> Success(T value) => new(true, value, null);

    public static NotificationServiceResult<T> Failure(NotificationServiceError error) =>
        new(false, default, error);
}
