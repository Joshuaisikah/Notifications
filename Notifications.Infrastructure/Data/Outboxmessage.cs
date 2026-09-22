using Notifications.Infrastructure.Data;

namespace Notifications.Infrastructure.Data
{
    public class Outboxmessage
    {
        public Guid Id { get; set; }
        public Guid NotificationId { get; set; }
        public NotificationStatus Status { get; set; } = NotificationStatus.Pending;
        public int AttemptCount { get; set; } = 0;
        public int MaxAttemptCount { get; set; } = 5;
        public string? NextRetryAt { get; set; }
        public DateTime? ClaimedAt { get; set; }
        public string? LastError { get; set; }
        public DateTime? LastAttemptAt { get; set; }
    }
}
