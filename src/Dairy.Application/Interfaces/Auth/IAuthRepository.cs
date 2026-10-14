using Dairy.Application.DTOs.Auth;

namespace Dairy.Application.Interfaces.Auth;

public interface IAuthRepository
{
    /// <summary>Calls sp_User_Login; null when the user does not exist.</summary>
    Task<UserLoginRow?> GetUserForLoginAsync(string username, CancellationToken ct = default);

    /// <summary>Calls sp_GetUserPermissions.</summary>
    Task<List<string>> GetPermissionsAsync(long userId, CancellationToken ct = default);

    /// <summary>Calls sp_RecordLoginAttempt (counters, lockout, login log).</summary>
    Task RecordLoginAttemptAsync(long? userId, string username, bool isSuccess, string? failureReason, string? ipAddress, string? userAgent, CancellationToken ct = default);

    Task UpdatePasswordAsync(long userId, string passwordHash, CancellationToken ct = default);
}
