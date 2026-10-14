namespace Dairy.Application.Interfaces;

public interface ICurrentUser
{
    bool IsAuthenticated { get; }

    /// <summary>User.UserId, or 0 when not authenticated.</summary>
    long UserId { get; }
    string Username { get; }
    string Role { get; }
    long? FarmerId { get; }
    long? CustomerId { get; }
    long? EmployeeId { get; }
    string? IpAddress { get; }
    string? UserAgent { get; }

    /// <summary>True when the JWT carries the permission claim (Super Admin always true).</summary>
    bool HasPermission(string permission);
    bool IsInRole(string role);
}
