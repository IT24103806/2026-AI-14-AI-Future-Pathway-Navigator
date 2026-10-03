using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PathwayNavigator.Api.Services;

namespace PathwayNavigator.Api.Controllers;

/// <summary>
/// The notification inbox. Deliberately tiny and cheap so a client can poll
/// <c>GET /api/notifications/unread-count</c> frequently without load: it is a single indexed count.
/// </summary>
[ApiController]
[Authorize]
[Route("api/notifications")]
public class NotificationController : ControllerBase
{
    private readonly INotificationService _notifications;

    public NotificationController(INotificationService notifications) => _notifications = notifications;

    private Guid CurrentUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        return Guid.TryParse(value, out var id)
            ? id
            : throw new UnauthorizedAccessException("Valid user ID claim is required.");
    }

    [HttpGet]
    public async Task<IActionResult> GetMine(
        [FromQuery] bool unreadOnly = false, [FromQuery] int page = 1, [FromQuery] int pageSize = 20) =>
        Ok(await _notifications.GetForUserAsync(CurrentUserId(), unreadOnly, page, pageSize));

    [HttpGet("unread-count")]
    public async Task<IActionResult> GetUnreadCount() =>
        Ok(new { unreadCount = await _notifications.GetUnreadCountAsync(CurrentUserId()) });

    [HttpPost("{id:guid}/read")]
    public async Task<IActionResult> MarkRead(Guid id)
    {
        var ok = await _notifications.MarkReadAsync(CurrentUserId(), id);
        return ok ? Ok(new { message = "Notification marked as read." }) : NotFound(new { message = "Notification not found." });
    }

    [HttpPost("read-all")]
    public async Task<IActionResult> MarkAllRead() =>
        Ok(new { marked = await _notifications.MarkAllReadAsync(CurrentUserId()) });
}
