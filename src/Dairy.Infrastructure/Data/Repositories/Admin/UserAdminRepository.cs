using Dairy.Application.Common;
using Dairy.Application.DTOs.Admin;
using Dairy.Application.Interfaces;
using Dairy.Application.Interfaces.Admin;
using Dairy.Application.Requests.Admin;

namespace Dairy.Infrastructure.Data.Repositories.Admin;

public class UserAdminRepository : IUserAdminRepository
{
    private static readonly Dictionary<string, string> SortColumns = new(StringComparer.OrdinalIgnoreCase)
    {
        ["id"] = "u.UserId",
        ["username"] = "u.Username",
        ["displayName"] = "u.DisplayName",
        ["email"] = "u.Email",
        ["roleName"] = "r.Name",
        ["isActive"] = "u.IsActive",
        ["lastLoginDate"] = "u.LastLoginDate",
        ["createdDate"] = "u.CreatedDate"
    };

    // PasswordHash is intentionally never selected.
    private const string From =
        " FROM `User` u" +
        " JOIN `Role` r ON r.RoleId = u.RoleId" +
        " LEFT JOIN `Farmer` f ON f.FarmerId = u.FarmerId" +
        " LEFT JOIN `Customer` c ON c.CustomerId = u.CustomerId" +
        " LEFT JOIN `Employee` e ON e.EmployeeId = u.EmployeeId" +
        " LEFT JOIN `Branch` b ON b.BranchId = u.BranchId" +
        " LEFT JOIN `CollectionCenter` cc ON cc.CollectionCenterId = u.CollectionCenterId";

    private const string Select =
        "SELECT u.UserId AS Id, u.Username, u.DisplayName, u.Email, u.Mobile, u.RoleId, r.Name AS RoleName, " +
        "u.FarmerId, f.FullName AS FarmerName, u.CustomerId, c.Name AS CustomerName, u.EmployeeId, e.FullName AS EmployeeName, " +
        "u.BranchId, b.Name AS BranchName, u.CollectionCenterId, cc.Name AS CollectionCenterName, u.IsActive, " +
        "(u.LockedUntil IS NOT NULL AND u.LockedUntil > NOW()) AS IsLocked, u.LockedUntil, u.FailedLoginCount, u.LastLoginDate, " +
        "u.MustChangePassword, u.CreatedDate" + From;

    private const string Where =
        " WHERE (@Search IS NULL OR u.Username LIKE @Search OR u.DisplayName LIKE @Search OR u.Email LIKE @Search OR u.Mobile LIKE @Search)" +
        " AND (@RoleId IS NULL OR u.RoleId = @RoleId)" +
        " AND (@IsActive IS NULL OR u.IsActive = @IsActive)" +
        " AND (@IsLocked IS NULL OR (u.LockedUntil IS NOT NULL AND u.LockedUntil > NOW()) = @IsLocked)";

    private readonly IDbConnectionFactory _db;

    public UserAdminRepository(IDbConnectionFactory db) => _db = db;

    public async Task<PagedResult<UserAdminDto>> GetPagedAsync(UserAdminQuery query, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        var param = new { Search = query.SearchPattern, query.RoleId, query.IsActive, query.IsLocked };
        return await conn.QueryPagedAsync<UserAdminDto>(
            Select + Where,
            "SELECT COUNT(*)" + From + Where,
            param, query, SortColumns, "u.Username ASC", ct);
    }

    public async Task<UserAdminDto?> GetByIdAsync(long id, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        return await conn.QueryFirstOrNullAsync<UserAdminDto>(Select + " WHERE u.UserId = @Id", new { Id = id }, ct);
    }

    public async Task<bool> UsernameExistsAsync(string username, long? excludeId, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        var count = await conn.ScalarAsync<int>(
            "SELECT COUNT(1) FROM `User` WHERE Username = @Username AND (@ExcludeId IS NULL OR UserId <> @ExcludeId)",
            new { Username = username, ExcludeId = excludeId }, ct);
        return count > 0;
    }

    public async Task<string?> GetActiveRoleNameAsync(long roleId, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        return await conn.ScalarAsync<string?>("SELECT Name FROM `Role` WHERE RoleId = @Id AND IsActive = 1", new { Id = roleId }, ct);
    }

    public async Task<bool> LinkedEntityExistsAsync(string kind, long id, CancellationToken ct = default)
    {
        var sql = kind switch
        {
            "farmer" => "SELECT COUNT(1) FROM `Farmer` WHERE FarmerId = @Id",
            "customer" => "SELECT COUNT(1) FROM `Customer` WHERE CustomerId = @Id",
            "employee" => "SELECT COUNT(1) FROM `Employee` WHERE EmployeeId = @Id",
            "branch" => "SELECT COUNT(1) FROM `Branch` WHERE BranchId = @Id",
            "collectionCenter" => "SELECT COUNT(1) FROM `CollectionCenter` WHERE CollectionCenterId = @Id",
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown linked entity kind.")
        };
        await using var conn = await _db.OpenAsync(ct);
        return await conn.ScalarAsync<int>(sql, new { Id = id }, ct) > 0;
    }

    public async Task<int> CountOtherActiveSuperAdminsAsync(long excludeUserId, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        return await conn.ScalarAsync<int>(
            "SELECT COUNT(1) FROM `User` u JOIN `Role` r ON r.RoleId = u.RoleId " +
            "WHERE r.Name = 'Super Admin' AND u.IsActive = 1 AND u.UserId <> @ExcludeId",
            new { ExcludeId = excludeUserId }, ct);
    }

    public async Task<long> CreateAsync(CreateUserRequest r, string passwordHash, bool mustChangePassword, bool isActive, long actorId, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        return await conn.InsertAsync(
            "INSERT INTO `User` (Username, PasswordHash, DisplayName, Email, Mobile, RoleId, FarmerId, CustomerId, EmployeeId, BranchId, CollectionCenterId, " +
            "MustChangePassword, IsActive, CreatedBy) " +
            "VALUES (@Username, @PasswordHash, @DisplayName, @Email, @Mobile, @RoleId, @FarmerId, @CustomerId, @EmployeeId, @BranchId, @CollectionCenterId, " +
            "@MustChangePassword, @IsActive, @ActorId)",
            new
            {
                r.Username,
                PasswordHash = passwordHash,
                r.DisplayName,
                r.Email,
                r.Mobile,
                r.RoleId,
                r.FarmerId,
                r.CustomerId,
                r.EmployeeId,
                r.BranchId,
                r.CollectionCenterId,
                MustChangePassword = mustChangePassword,
                IsActive = isActive,
                ActorId = actorId
            }, ct);
    }

    public async Task UpdateAsync(long id, UpdateUserRequest r, bool isActive, long actorId, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        await conn.ExecAsync(
            "UPDATE `User` SET DisplayName = @DisplayName, Email = @Email, Mobile = @Mobile, RoleId = @RoleId, FarmerId = @FarmerId, " +
            "CustomerId = @CustomerId, EmployeeId = @EmployeeId, BranchId = @BranchId, CollectionCenterId = @CollectionCenterId, " +
            "IsActive = @IsActive, ModifiedBy = @ActorId WHERE UserId = @Id",
            new
            {
                Id = id,
                r.DisplayName,
                r.Email,
                r.Mobile,
                r.RoleId,
                r.FarmerId,
                r.CustomerId,
                r.EmployeeId,
                r.BranchId,
                r.CollectionCenterId,
                IsActive = isActive,
                ActorId = actorId
            }, ct);
    }

    public async Task SetPasswordAsync(long id, string passwordHash, bool mustChangePassword, long actorId, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        await conn.ExecAsync(
            "UPDATE `User` SET PasswordHash = @PasswordHash, MustChangePassword = @MustChangePassword, FailedLoginCount = 0, LockedUntil = NULL, " +
            "ModifiedBy = @ActorId WHERE UserId = @Id",
            new { Id = id, PasswordHash = passwordHash, MustChangePassword = mustChangePassword, ActorId = actorId }, ct);
    }

    public async Task UnlockAsync(long id, long actorId, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        await conn.ExecAsync(
            "UPDATE `User` SET FailedLoginCount = 0, LockedUntil = NULL, ModifiedBy = @ActorId WHERE UserId = @Id",
            new { Id = id, ActorId = actorId }, ct);
    }
}
