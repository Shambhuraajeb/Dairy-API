namespace Dairy.Application.DTOs.Auth;

/// <summary>Row returned by sp_User_Login. Contains the password hash: NEVER return it from an API.</summary>
public class UserLoginRow
{
    public long UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public long RoleId { get; set; }
    public string RoleName { get; set; } = string.Empty;
    public long? FarmerId { get; set; }
    public long? CustomerId { get; set; }
    public long? EmployeeId { get; set; }
    public bool IsActive { get; set; }
    public int FailedLoginCount { get; set; }
    public DateTime? LockedUntil { get; set; }
    public bool MustChangePassword { get; set; }
}
