using Microsoft.EntityFrameworkCore;
using Notifications.Api.DTOs;
using Notifications.Api.Enums;
using Notifications.Infrastructure.Data;

namespace Notifications.Api.Services;

public class NotificationService
{
    private readonly NotificationsDbContext _dbcontext;

    public NotificationService(NotificationsDbContext dbcontext)
    {
        _dbcontext = dbcontext;
    }

    public async Task<NotificationServiceResult<Notification>> CreateAsync(
        CreateNotificationRequest request,
        CancellationToken ct
    )
    {
        var notification = new Notification
        {
            Recipient = request.Recipient,
            Message = request.Message,
            Channel = request.Channel,
        };
        var outboxmessage = new OutboxMessage
        {
            Status = NotificationStatus.Pending,
            NextRetryAt = DateTime.UtcNow,
        };
        notification.OutboxMessage = outboxmessage;
        // IMPORTANT: entities must be explicitly linked (via nav property or FK) before
        // SaveChangesAsync. Add()-ing two related entities separately does NOT tie them
        // together on its own — without the line above, this insert would violate the
        // OutboxMessage.NotificationId FK constraint (or point at nothing).
        _dbcontext.Notifications.Add(notification);
        await _dbcontext.SaveChangesAsync(ct);
        return NotificationServiceResult<Notification>.Success(notification);
    }
}
