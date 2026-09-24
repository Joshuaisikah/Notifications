using Microsoft.AspNetCore.Mvc;
using Notifications.Api.DTOs;
using Notifications.Api.Enums;
using Notifications.Api.Services;

namespace Notifications.Api.Controllers;

[ApiController]
[Route("api/notifications")]
public class NotificationsController : ControllerBase
{
    private readonly NotificationRetryService _notificationRetryService;
    private readonly NotificationService _notificationService;

    public NotificationsController(
        NotificationService notificationService,
        NotificationRetryService notificationRetryService
    )
    {
        _notificationService = notificationService;
        _notificationRetryService = notificationRetryService;
    }

    [HttpPost]
    public async Task<ActionResult<NotificationResponse>> Create(
        [FromBody] CreateNotificationRequest request,
        CancellationToken ct
    )
    {
        var result = await _notificationService.CreateAsync(request, ct);
        if (!result.IsSuccess)
        {
            return HandleError(result.Error!.Value);
        }
        return CreatedAtAction(nameof(Get), new { id = result.Value!.Id }, result.Value);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<NotificationStatusResponse>> Get(Guid id, CancellationToken ct)
    {
        var result = await _notificationService.GetAsync(id, ct);
        if (!result.IsSuccess)
        {
            return HandleError(result.Error!.Value);
        }
        return Ok(result.Value);
    }

    [HttpPost("{id:guid}/retry")]
    public async Task<ActionResult> Retry(Guid id, CancellationToken ct)
    {
        var result = await _notificationRetryService.RetryAsync(id, ct);
        if (!result.IsSuccess)
        {
            return HandleError(result.Error!.Value);
        }
        return Ok();
    }

    private ActionResult HandleError(NotificationServiceError error) =>
        error switch
        {
            NotificationServiceError.NotFound => NotFound(),
            NotificationServiceError.InvalidState => Conflict(),
        };
}
