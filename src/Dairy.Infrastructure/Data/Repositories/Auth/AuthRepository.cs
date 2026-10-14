using Dairy.Application.DTOs.Auth;
using Dairy.Application.Interfaces;
using Dairy.Application.Interfaces.Auth;

namespace Dairy.Infrastructure.Data.Repositories.Auth;

public class AuthRepository : IAuthRepository
{
    private readonly IDbConnectionFactory _db;

    public AuthRepository(IDbConnectionFactory db) => _db = db;

    public async Task<UserLoginRow?> GetUserForLoginAsync(string username, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        return await conn.QuerySpFirstOrDefaultAsync<UserLoginRow>("sp_User_Login", new { p_Username = username }, ct);
    }

    public async Task<List<string>> GetPermissionsAsync(long userId, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        var rows = await conn.QuerySpAsync<PermissionCodeRow>("sp_GetUserPermissions", new { p_UserId = userId }, ct);
        return rows.Select(r => r.Code).ToList();
    }

    public async Task RecordLoginAttemptAsync(long? userId, string username, bool isSuccess, string? failureReason, string? ipAddress, string? userAgent, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        await conn.ExecuteSpAsync("sp_RecordLoginAttempt", new
        {
            p_UserId = userId,
            p_Username = username,
            p_IsSuccess = isSuccess,
            p_FailureReason = failureReason,
            p_IpAddress = ipAddress,
            p_UserAgent = userAgent
        }, ct);
    }

    public async Task UpdatePasswordAsync(long userId, string passwordHash, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        await conn.ExecAsync(
            "UPDATE `User` SET PasswordHash = @Hash, MustChangePassword = 0, ModifiedBy = @UserId WHERE UserId = @UserId",
            new { Hash = passwordHash, UserId = userId }, ct);
    }
}
