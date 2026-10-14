using Dairy.Application.Common;
using Dairy.Application.DTOs.Notifications;
using Dairy.Application.Interfaces;
using Dairy.Application.Interfaces.Notifications;
using Dairy.Application.Requests.Notifications;

namespace Dairy.Infrastructure.Data.Repositories.Notifications;

public class NotificationRepository : INotificationRepository
{
    private static readonly Dictionary<string, string> SortColumns = new(StringComparer.OrdinalIgnoreCase)
    {
        ["id"] = "n.NotificationId",
        ["title"] = "n.Title",
        ["severity"] = "n.Severity",
        ["isRead"] = "n.IsRead",
        ["createdDate"] = "n.CreatedDate"
    };

    // Visible = addressed to the user, or a broadcast to the user's role, or a global broadcast (no user, no role).
    // @SeeAll = 1 (Super Admin) additionally shows every broadcast.
    private const string Visible =
        "(n.UserId = @UserId OR (n.UserId IS NULL AND (@SeeAll = 1 OR n.RoleId IS NULL " +
        "OR n.RoleId = (SELECT u.RoleId FROM `User` u WHERE u.UserId = @UserId))))";

    private const string Select =
        "SELECT n.NotificationId AS Id, n.UserId, n.RoleId, n.Title, n.Message, n.Severity, n.Link, n.IsRead, n.CreatedDate, n.ReadDate " +
        "FROM `Notification` n";

    private readonly IDbConnectionFactory _db;

    public NotificationRepository(IDbConnectionFactory db) => _db = db;

    public async Task<PagedResult<NotificationDto>> GetPagedAsync(long userId, bool seeAllBroadcasts, NotificationQuery query, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        const string filter =
            " WHERE " + Visible +
            " AND (@UnreadOnly IS NULL OR @UnreadOnly = 0 OR n.IsRead = 0)" +
            " AND (@Severity IS NULL OR n.Severity = @Severity)" +
            " AND (@Search IS NULL OR n.Title LIKE @Search OR n.Message LIKE @Search)";

        var param = new
        {
            UserId = userId,
            SeeAll = seeAllBroadcasts,
            query.UnreadOnly,
            Severity = string.IsNullOrWhiteSpace(query.Severity) ? null : query.Severity.Trim(),
            Search = query.SearchPattern
        };
        return await conn.QueryPagedAsync<NotificationDto>(
            Select + filter,
            "SELECT COUNT(*) FROM `Notification` n" + filter,
            param, query, SortColumns, "n.CreatedDate DESC, n.NotificationId DESC", ct);
    }

    public async Task<NotificationDto?> GetVisibleAsync(long userId, bool seeAllBroadcasts, long id, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        return await conn.QueryFirstOrNullAsync<NotificationDto>(
            Select + " WHERE n.NotificationId = @Id AND " + Visible,
            new { Id = id, UserId = userId, SeeAll = seeAllBroadcasts }, ct);
    }

    public async Task<int> GetUnreadCountAsync(long userId, bool seeAllBroadcasts, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        return await conn.ScalarAsync<int>(
            "SELECT COUNT(*) FROM `Notification` n WHERE n.IsRead = 0 AND " + Visible,
            new { UserId = userId, SeeAll = seeAllBroadcasts }, ct);
    }

    public async Task<bool> MarkReadAsync(long userId, bool seeAllBroadcasts, long id, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        var affected = await conn.ExecAsync(
            "UPDATE `Notification` n SET n.IsRead = 1, n.ReadDate = NOW() WHERE n.NotificationId = @Id AND n.IsRead = 0 AND " + Visible,
            new { Id = id, UserId = userId, SeeAll = seeAllBroadcasts }, ct);
        return affected > 0;
    }

    public async Task<int> MarkAllReadAsync(long userId, bool seeAllBroadcasts, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        return await conn.ExecAsync(
            "UPDATE `Notification` n SET n.IsRead = 1, n.ReadDate = NOW() WHERE n.IsRead = 0 AND " + Visible,
            new { UserId = userId, SeeAll = seeAllBroadcasts }, ct);
    }

    public async Task GenerateAlertsAsync(CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        await conn.ExecuteSpAsync("sp_GenerateAlertNotifications", null, ct);
    }
}
