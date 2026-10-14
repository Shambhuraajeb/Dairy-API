using Dairy.Api.Authorization;
using Dairy.Application.Interfaces.Notifications;
using Dairy.Application.Requests.Notifications;
using Dairy.Domain.Constants;
using Microsoft.AspNetCore.Mvc;

namespace Dairy.Api.Controllers.Notifications;

[Route("api/notifications")]
[HasPermission(Permissions.NotificationView)]
public class NotificationsController : ApiControllerBase
{
    private readonly INotificationService _notifications;

    public NotificationsController(INotificationService notifications) => _notifications = notifications;

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] NotificationQuery query, CancellationToken ct) =>
        Paged(await _notifications.GetPagedAsync(query, ct));

    /// <summary>Also (at most every 10 minutes) generates low-stock / expiry / maintenance / vaccination / approval alerts.</summary>
    [HttpGet("unread-count")]
    public async Task<IActionResult> UnreadCount(CancellationToken ct) =>
        Ok(await _notifications.GetUnreadCountAsync(ct));

    [HttpPost("{id:long}/read")]
    [HttpPost("{id:long}/mark-read")]
    public async Task<IActionResult> MarkRead(long id, CancellationToken ct) =>
        Ok(await _notifications.MarkReadAsync(id, ct));

    [HttpPost("read-all")]
    [HttpPost("mark-all-read")]
    public async Task<IActionResult> MarkAllRead(CancellationToken ct) =>
        Ok(await _notifications.MarkAllReadAsync(ct));
}
