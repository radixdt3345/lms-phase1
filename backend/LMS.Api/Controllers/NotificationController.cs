using LMS.Application.DTOs.Notification;
using LMS.Application.Interfaces;
using LMS.Domain.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace LMS.Api.Controllers;

/// <summary>
/// In-app notification inbox — F-09 API layer (FR-72 to FR-75).
/// All responses are wrapped in ApiResponse&lt;T&gt;.
/// </summary>
[ApiController]
[Route("api/notifications")]
[Authorize]
[Produces("application/json")]
public class NotificationController : ControllerBase
{
    private readonly INotificationService _notificationService;
    private readonly ILogger<NotificationController> _logger;

    public NotificationController(INotificationService notificationService, ILogger<NotificationController> logger)
    {
        _notificationService = notificationService;
        _logger = logger;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private Guid CurrentUserId()
    {
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier)
                  ?? User.FindFirstValue("sub")
                  ?? throw new UnauthorizedAccessException("User identity could not be resolved.");
        return Guid.Parse(sub);
    }

    // ── Endpoints ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Returns the current user's notifications, newest first, paginated.
    /// AC-61: GET /api/notifications returns HTTP 200 with list of notifications.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<NotificationPageDto>>> GetMyNotifications(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var userId = CurrentUserId();
        var result = await _notificationService.GetMyNotificationsAsync(userId, page, pageSize);
        return Ok(ApiResponse<NotificationPageDto>.Ok(result));
    }

    /// <summary>
    /// Returns the count of unread notifications for the current user.
    /// Used by the navbar bell icon (FR-75 — refreshed every 60 s).
    /// </summary>
    [HttpGet("unread-count")]
    public async Task<ActionResult<ApiResponse<int>>> GetUnreadCount()
    {
        var userId = CurrentUserId();
        var count = await _notificationService.GetUnreadCountAsync(userId);
        return Ok(ApiResponse<int>.Ok(count));
    }

    /// <summary>
    /// Marks a single notification as read.
    /// AC-62: PUT /api/notifications/{id}/read sets is_read=true.
    /// </summary>
    [HttpPut("{id:guid}/read")]
    public async Task<ActionResult<ApiResponse<bool>>> MarkAsRead(Guid id)
    {
        var userId = CurrentUserId();
        var success = await _notificationService.MarkAsReadAsync(id, userId);

        if (!success)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Notification not found",
                Detail = $"No notification with ID '{id}' exists for this user.",
                Status = StatusCodes.Status404NotFound,
                Extensions = { ["error_code"] = "NOTIFICATION_NOT_FOUND" }
            });
        }

        return Ok(ApiResponse<bool>.Ok(true));
    }

    /// <summary>
    /// Marks all notifications for the current user as read.
    /// AC-62: PUT /api/notifications/read-all sets is_read=true on all.
    /// </summary>
    [HttpPut("read-all")]
    public async Task<ActionResult<ApiResponse<bool>>> MarkAllAsRead()
    {
        var userId = CurrentUserId();
        await _notificationService.MarkAllAsReadAsync(userId);
        return Ok(ApiResponse<bool>.Ok(true));
    }

    /// <summary>
    /// Deletes (removes) a notification for the current user.
    /// </summary>
    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<ApiResponse<bool>>> Delete(Guid id)
    {
        var userId = CurrentUserId();
        var success = await _notificationService.DeleteAsync(id, userId);

        if (!success)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Notification not found",
                Detail = $"No notification with ID '{id}' exists for this user.",
                Status = StatusCodes.Status404NotFound,
                Extensions = { ["error_code"] = "NOTIFICATION_NOT_FOUND" }
            });
        }

        return Ok(ApiResponse<bool>.Ok(true));
    }
}
