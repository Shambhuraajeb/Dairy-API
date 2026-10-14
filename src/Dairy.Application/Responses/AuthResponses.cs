namespace Dairy.Application.Responses;

public class AuthUserDto
{
    public long Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public List<string> Permissions { get; set; } = new();
    public string? Email { get; set; }
    public long? FarmerId { get; set; }
    public long? CustomerId { get; set; }
    public long? EmployeeId { get; set; }
    public bool MustChangePassword { get; set; }
}

public class LoginResponse
{
    public string AccessToken { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public AuthUserDto User { get; set; } = new();
}
