namespace Dairy.Application.DTOs.Notifications;

public class NotificationDto
{
    public long Id { get; set; }
    public long? UserId { get; set; }
    public long? RoleId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;

    /// <summary>info | warning | danger | success</summary>
    public string Severity { get; set; } = "info";
    public string? Link { get; set; }
    public bool IsRead { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime? ReadDate { get; set; }
}

public class UnreadCountDto
{
    public int Count { get; set; }
}

public class MarkAllReadResultDto
{
    public int Updated { get; set; }
}
