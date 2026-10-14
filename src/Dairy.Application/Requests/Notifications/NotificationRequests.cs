using Dairy.Application.Common;

namespace Dairy.Application.Requests.Notifications;

public class NotificationQuery : PagedQuery
{
    /// <summary>true = only unread.</summary>
    public bool? UnreadOnly { get; set; }
    public string? Severity { get; set; }
}
