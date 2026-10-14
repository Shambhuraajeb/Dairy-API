using Dairy.Application.Common;
using Dairy.Application.DTOs.Notifications;
using Dairy.Application.Interfaces;
using Dairy.Application.Interfaces.Notifications;
using Dairy.Application.Requests.Notifications;
using Dairy.Domain.Constants;

namespace Dairy.Application.Services.Notifications;

public class NotificationService : INotificationService
{
    // Alerts (low stock, expiry, maintenance, vaccination, approvals) are generated lazily: at most once per interval, process-wide.
    private static readonly long AlertIntervalTicks = TimeSpan.FromMinutes(10).Ticks;
    private static long _lastAlertRunTicks;

    private readonly INotificationRepository _repo;
    private readonly ICurrentUser _user;

    public NotificationService(INotificationRepository repo, ICurrentUser user)
    {
        _repo = repo;
        _user = user;
    }

    public Task<PagedResult<NotificationDto>> GetPagedAsync(NotificationQuery query, CancellationToken ct = default) =>
        _repo.GetPagedAsync(_user.UserId, SeesAllBroadcasts, query, ct);

    public async Task<UnreadCountDto> GetUnreadCountAsync(CancellationToken ct = default)
    {
        if (ShouldGenerateAlerts())
        {
            try
            {
                await _repo.GenerateAlertsAsync(ct);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch
            {
                // Alert generation must never break the notification bell; it is retried after the next interval.
            }
        }

        return new UnreadCountDto { Count = await _repo.GetUnreadCountAsync(_user.UserId, SeesAllBroadcasts, ct) };
    }

    public async Task<NotificationDto> MarkReadAsync(long id, CancellationToken ct = default)
    {
        var existing = await _repo.GetVisibleAsync(_user.UserId, SeesAllBroadcasts, id, ct)
                       ?? throw new NotFoundException("Notification", id);
        if (!existing.IsRead)
            await _repo.MarkReadAsync(_user.UserId, SeesAllBroadcasts, id, ct);

        return await _repo.GetVisibleAsync(_user.UserId, SeesAllBroadcasts, id, ct)
               ?? throw new NotFoundException("Notification", id);
    }

    public async Task<MarkAllReadResultDto> MarkAllReadAsync(CancellationToken ct = default) =>
        new() { Updated = await _repo.MarkAllReadAsync(_user.UserId, SeesAllBroadcasts, ct) };

    /// <summary>Super Admin sees every role broadcast, not only those addressed to their own role.</summary>
    private bool SeesAllBroadcasts => _user.IsInRole(Roles.SuperAdmin);

    private static bool ShouldGenerateAlerts()
    {
        var now = DateTime.UtcNow.Ticks;
        var last = Interlocked.Read(ref _lastAlertRunTicks);
        if (last != 0 && now - last < AlertIntervalTicks) return false;
        return Interlocked.CompareExchange(ref _lastAlertRunTicks, now, last) == last;
    }
}
