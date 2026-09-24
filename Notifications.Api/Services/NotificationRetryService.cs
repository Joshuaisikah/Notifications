using Microsoft.EntityFrameworkCore;
using Notifications.Api.Enums;
using Notifications.Infrastructure.Data;

namespace Notifications.Api.Services;

public class NotificationRetryService
{
    private readonly NotificationsDbContext _dbcontext;

    public NotificationRetryService(NotificationsDbContext dbContext)
    {
        _dbcontext = dbContext;
    }

    public async Task<NotificationServiceResult<bool>> RetryAsync(
        Guid notificationId,
        CancellationToken ct
    )
    {
        var outboxMessage = await _dbcontext.OutboxMessages.FirstOrDefaultAsync(
            o => o.NotificationId == notificationId,
            ct
        );
        if (outboxMessage is null)
        {
            return NotificationServiceResult<bool>.Failure(NotificationServiceError.NotFound);
        }
        if (outboxMessage.Status != NotificationStatus.Failed)
        {
            return NotificationServiceResult<bool>.Failure(NotificationServiceError.InvalidState);
        }
        outboxMessage.Status = NotificationStatus.Pending;
        outboxMessage.AttemptCount = 0;
        outboxMessage.LastError = null;
        outboxMessage.NextRetryAt = DateTime.UtcNow;
        outboxMessage.ClaimedAt = null;
        outboxMessage.LastAttemptAt = null;
        await _dbcontext.SaveChangesAsync(ct);
        return NotificationServiceResult<bool>.Success(true);
    }
}
