using Microsoft.EntityFrameworkCore;
using Notifications.Infrastructure.Data;
using Xunit;

namespace Notifications.Tests.Integration;

// Adjust connection string to your test DB — this hits ExecuteUpdateAsync,
// which translates to real SQL and isn't exercised by the EF InMemory provider.
public class OutboxClaimQueryTests
{
    private static NotificationsDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<NotificationsDbContext>()
            .UseNpgsql(
                "Host=localhost;Port=5432;Database=notification_outbox;Username=postgres;Password=postgres"
            )
            .Options;
        return new NotificationsDbContext(options);
    }

    // This is the exact bug found manually: a row stuck in Sending with a stale
    // ClaimedAt, but AttemptCount already at MaxAttemptCount, was being reclaimed
    // and re-incremented forever. This test locks that behavior down.
    [Fact]
    public async Task StaleReclaim_DoesNotReclaimRow_ThatAlreadyExhaustedAttempts()
    {
        await using var dbContext = CreateContext();

        var notification = new Notification
        {
            Recipient = "test@example.com",
            Message = "test",
            Channel = "Email",
        };

        var outboxMessage = new OutboxMessage
        {
            Status = NotificationStatus.Sending,
            AttemptCount = 5,
            MaxAttemptCount = 5,
            ClaimedAt = DateTime.UtcNow.AddSeconds(-30), // well past the 15s staleness threshold
        };

        notification.OutboxMessage = outboxMessage;
        dbContext.Notifications.Add(notification);
        dbContext.OutboxMessages.Add(outboxMessage);
        await dbContext.SaveChangesAsync();

        var claimedAt = DateTime.UtcNow;
        var staleCutoff = claimedAt.AddSeconds(-15);

        var claimedCount = await dbContext
            .OutboxMessages.Where(o =>
                (o.Status == NotificationStatus.Pending && o.NextRetryAt <= claimedAt)
                || (
                    o.Status == NotificationStatus.Sending
                    && o.ClaimedAt < staleCutoff
                    && o.AttemptCount < o.MaxAttemptCount
                )
            )
            .ExecuteUpdateAsync(setters =>
                setters
                    .SetProperty(o => o.Status, NotificationStatus.Sending)
                    .SetProperty(o => o.ClaimedAt, claimedAt)
                    .SetProperty(o => o.AttemptCount, o => o.AttemptCount + 1)
            );

        Assert.Equal(0, claimedCount);

        var reloaded = await dbContext
            .OutboxMessages.AsNoTracking()
            .FirstAsync(o => o.Id == outboxMessage.Id);

        Assert.Equal(5, reloaded.AttemptCount); // must NOT have been bumped to 6
    }

    // Sanity check the same query still reclaims a genuinely stuck row with room left.
    [Fact]
    public async Task StaleReclaim_DoesReclaimRow_WithAttemptsRemaining()
    {
        await using var dbContext = CreateContext();

        var notification = new Notification
        {
            Recipient = "test@example.com",
            Message = "test",
            Channel = "Email",
        };

        var outboxMessage = new OutboxMessage
        {
            Status = NotificationStatus.Sending,
            AttemptCount = 2,
            MaxAttemptCount = 5,
            ClaimedAt = DateTime.UtcNow.AddSeconds(-30),
        };

        notification.OutboxMessage = outboxMessage;
        dbContext.Notifications.Add(notification);
        dbContext.OutboxMessages.Add(outboxMessage);
        await dbContext.SaveChangesAsync();

        var claimedAt = DateTime.UtcNow;
        var staleCutoff = claimedAt.AddSeconds(-15);

        var claimedCount = await dbContext
            .OutboxMessages.Where(o =>
                (o.Status == NotificationStatus.Pending && o.NextRetryAt <= claimedAt)
                || (
                    o.Status == NotificationStatus.Sending
                    && o.ClaimedAt < staleCutoff
                    && o.AttemptCount < o.MaxAttemptCount
                )
            )
            .ExecuteUpdateAsync(setters =>
                setters
                    .SetProperty(o => o.Status, NotificationStatus.Sending)
                    .SetProperty(o => o.ClaimedAt, claimedAt)
                    .SetProperty(o => o.AttemptCount, o => o.AttemptCount + 1)
            );

        Assert.Equal(1, claimedCount);

        var reloaded = await dbContext
            .OutboxMessages.AsNoTracking()
            .FirstAsync(o => o.Id == outboxMessage.Id);

        Assert.Equal(3, reloaded.AttemptCount); // bumped from 2 to 3
    }
}
