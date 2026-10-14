using Dairy.Application.DTOs.Admin;
using Dairy.Application.Interfaces;
using Dairy.Application.Interfaces.Admin;

namespace Dairy.Infrastructure.Data.Repositories.Admin;

public class ProfileRepository : IProfileRepository
{
    private readonly IDbConnectionFactory _db;

    public ProfileRepository(IDbConnectionFactory db) => _db = db;

    public async Task<ProfileDto?> GetAsync(long userId, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        return await conn.QueryFirstOrNullAsync<ProfileDto>(
            "SELECT u.UserId AS Id, u.Username, u.DisplayName, u.Email, u.Mobile, r.Name AS RoleName, u.FarmerId, u.CustomerId, u.EmployeeId, " +
            "u.BranchId, b.Name AS BranchName, u.CollectionCenterId, cc.Name AS CollectionCenterName, u.LastLoginDate " +
            "FROM `User` u JOIN `Role` r ON r.RoleId = u.RoleId " +
            "LEFT JOIN `Branch` b ON b.BranchId = u.BranchId " +
            "LEFT JOIN `CollectionCenter` cc ON cc.CollectionCenterId = u.CollectionCenterId " +
            "WHERE u.UserId = @UserId",
            new { UserId = userId }, ct);
    }

    public async Task UpdateAsync(long userId, string displayName, string? email, string? mobile, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        await conn.ExecAsync(
            "UPDATE `User` SET DisplayName = @DisplayName, Email = @Email, Mobile = @Mobile, ModifiedBy = @UserId WHERE UserId = @UserId",
            new { UserId = userId, DisplayName = displayName, Email = email, Mobile = mobile }, ct);
    }
}
