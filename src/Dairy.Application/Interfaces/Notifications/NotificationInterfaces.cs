using Dairy.Application.Common;
using Dairy.Application.DTOs.Notifications;
using Dairy.Application.Requests.Notifications;

namespace Dairy.Application.Interfaces.Notifications;

public interface INotificationRepository
{
    Task<PagedResult<NotificationDto>> GetPagedAsync(long userId, bool seeAllBroadcasts, NotificationQuery query, CancellationToken ct = default);
    Task<NotificationDto?> GetVisibleAsync(long userId, bool seeAllBroadcasts, long id, CancellationToken ct = default);
    Task<int> GetUnreadCountAsync(long userId, bool seeAllBroadcasts, CancellationToken ct = default);
    Task<bool> MarkReadAsync(long userId, bool seeAllBroadcasts, long id, CancellationToken ct = default);
    Task<int> MarkAllReadAsync(long userId, bool seeAllBroadcasts, CancellationToken ct = default);
    Task GenerateAlertsAsync(CancellationToken ct = default);
}

public interface INotificationService
{
    Task<PagedResult<NotificationDto>> GetPagedAsync(NotificationQuery query, CancellationToken ct = default);
    Task<UnreadCountDto> GetUnreadCountAsync(CancellationToken ct = default);
    Task<NotificationDto> MarkReadAsync(long id, CancellationToken ct = default);
    Task<MarkAllReadResultDto> MarkAllReadAsync(CancellationToken ct = default);
}
