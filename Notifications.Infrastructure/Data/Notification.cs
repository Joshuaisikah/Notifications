namespace Notifications.Infrastructure.Data;

public class Notification
{
    public Guid Id { get; set; }
    public string Recipient { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Channel { get; set; } = "Email";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    //this is the navigation property one Notification has exactly one outboxmessage.
    public OutboxMessage? OutboxMessage { get; set; }
}
