using Microsoft.EntityFrameworkCore;
using Notifications.Infrastructure.Data;

namespace Notifications.Api.Workers;

public class OutboxWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<OutboxWorker> _logger;
    private static readonly TimeSpan PollingInterval = TimeSpan.FromSeconds(2);
    private static readonly TimeSpan StalenessThreshold = TimeSpan.FromSeconds(5);

    public OutboxWorker(
        IServiceScopeFactory scopeFactory,
        IHttpClientFactory httpClientFactory,
        ILogger<OutboxWorker> logger
    )
    {
        _scopeFactory = scopeFactory;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                await ProcessOneAsync(ct);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Outbox poll cycle failed");
            }
            await Task.Delay(PollingInterval, ct);
        }
    }

    private async Task ProcessOneAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<NotificationsDbContext>();
        var claimedAt = DateTime.UtcNow;
        var staleCutoff = claimedAt - StalenessThreshold;
        var claimedCount = await dbContext
            .OutboxMessages.Where(o =>
                (o.Status == NotificationStatus.Pending && o.NextRetryAt <= claimedAt)
                || (
                    o.Status == NotificationStatus.Sending
                    && o.ClaimedAt < staleCutoff
                    && o.AttemptCount < o.MaxAttemptCount
                )
            )
            .ExecuteUpdateAsync(
                setters =>
                    setters
                        .SetProperty(o => o.Status, NotificationStatus.Sending)
                        .SetProperty(o => o.ClaimedAt, claimedAt)
                        .SetProperty(o => o.AttemptCount, o => o.AttemptCount + 1),
                ct
            );
        if (claimedCount == 0)
        {
            return;
        }
        var claimedRows = await dbContext
            .OutboxMessages.Where(o =>
                o.Status == NotificationStatus.Sending && o.ClaimedAt == claimedAt
            )
            .ToListAsync(ct);
        foreach (var outboxMessage in claimedRows)
        {
            await SendAsync(outboxMessage, ct);
            await dbContext.SaveChangesAsync(ct);
        }
    }

    private async Task SendAsync(OutboxMessage outboxMessage, CancellationToken ct)
    {
        var httpClient = _httpClientFactory.CreateClient("TestReceiver");
        string? errorMessage = null;
        var succeeded = false;
        try
        {
            var response = await httpClient.PostAsync("/deliver", content: null, ct);
            if (response.IsSuccessStatusCode)
            {
                succeeded = true;
            }
            else
            {
                errorMessage =
                    $"Provider returned {(int)response.StatusCode} {response.StatusCode}";
            }
        }
        catch (TaskCanceledException) when (!ct.IsCancellationRequested)
        {
            errorMessage = "Request to provider timed out";
        }
        catch (HttpRequestException ex)
        {
            errorMessage = $"Request to provider failed: {ex.Message}";
        }
        catch (Exception ex)
        {
            // Catch-all: guarantees this row always gets a written outcome, even for
            // exception types we didn't anticipate (a NullReferenceException from a bug
            // in our own code, for instance). Without this, a row can be silently
            // stranded in Sending forever if something unexpected throws here.
            errorMessage = $"Unexpected error: {ex.Message}";
        }

        outboxMessage.LastAttemptAt = DateTime.UtcNow;

        if (succeeded)
        {
            outboxMessage.Status = NotificationStatus.Sent;
            outboxMessage.NextRetryAt = null;
            outboxMessage.ClaimedAt = null;
            outboxMessage.LastError = null;
            return;
        }

        outboxMessage.LastError = errorMessage;

        if (outboxMessage.AttemptCount >= outboxMessage.MaxAttemptCount)
        {
            outboxMessage.Status = NotificationStatus.Failed;
            outboxMessage.NextRetryAt = null;
            outboxMessage.ClaimedAt = null;
        }
        else
        {
            var delaySeconds = Math.Pow(2, outboxMessage.AttemptCount - 1);
            outboxMessage.Status = NotificationStatus.Pending;
            outboxMessage.NextRetryAt = DateTime.UtcNow.AddSeconds(delaySeconds);
            outboxMessage.ClaimedAt = null;
        }
    }
}
