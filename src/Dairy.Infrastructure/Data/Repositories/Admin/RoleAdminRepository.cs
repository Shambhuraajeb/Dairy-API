using Dairy.Application.Common;
using Dairy.Application.DTOs.Admin;
using Dairy.Application.Interfaces;
using Dairy.Application.Interfaces.Admin;
using Dairy.Application.Requests.Admin;
using Dapper;

namespace Dairy.Infrastructure.Data.Repositories.Admin;

public class RoleAdminRepository : IRoleAdminRepository
{
    private static readonly Dictionary<string, string> SortColumns = new(StringComparer.OrdinalIgnoreCase)
    {
        ["id"] = "r.RoleId",
        ["name"] = "r.Name",
        ["isSystem"] = "r.IsSystem",
        ["isActive"] = "r.IsActive"
    };

    private const string Select =
        "SELECT r.RoleId AS Id, r.Name, r.Description, r.IsSystem, r.IsActive, " +
        "(SELECT COUNT(*) FROM `User` u WHERE u.RoleId = r.RoleId) AS UserCount, " +
        "(SELECT COUNT(*) FROM `RolePermission` rp WHERE rp.RoleId = r.RoleId) AS PermissionCount " +
        "FROM `Role` r";

    private const string Where =
        " WHERE (@Search IS NULL OR r.Name LIKE @Search OR r.Description LIKE @Search)" +
        " AND (@IsActive IS NULL OR r.IsActive = @IsActive)";

    private readonly IDbConnectionFactory _db;

    public RoleAdminRepository(IDbConnectionFactory db) => _db = db;

    public async Task<PagedResult<RoleDto>> GetPagedAsync(RoleAdminQuery query, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        var param = new { Search = query.SearchPattern, query.IsActive };
        return await conn.QueryPagedAsync<RoleDto>(
            Select + Where,
            "SELECT COUNT(*) FROM `Role` r" + Where,
            param, query, SortColumns, "r.RoleId ASC", ct);
    }

    public async Task<RoleDto?> GetByIdAsync(long id, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        return await conn.QueryFirstOrNullAsync<RoleDto>(Select + " WHERE r.RoleId = @Id", new { Id = id }, ct);
    }

    public async Task<List<PermissionDto>> GetRolePermissionsAsync(long roleId, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        return await conn.QueryListAsync<PermissionDto>(
            "SELECT p.PermissionId AS Id, p.Code, p.Name, p.Module FROM `RolePermission` rp " +
            "JOIN `Permission` p ON p.PermissionId = rp.PermissionId WHERE rp.RoleId = @RoleId ORDER BY p.Code",
            new { RoleId = roleId }, ct);
    }

    public async Task<List<PermissionDto>> GetAllPermissionsAsync(CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        return await conn.QueryListAsync<PermissionDto>(
            "SELECT PermissionId AS Id, Code, Name, Module FROM `Permission` ORDER BY Module, Code", null, ct);
    }

    public async Task<List<PermissionDto>> FindPermissionsAsync(IReadOnlyCollection<long> ids, IReadOnlyCollection<string> codes, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        // Sentinels keep both IN lists non-empty.
        var idList = ids.Count > 0 ? ids.ToList() : new List<long> { -1 };
        var codeList = codes.Count > 0 ? codes.ToList() : new List<string> { string.Empty };
        return await conn.QueryListAsync<PermissionDto>(
            "SELECT PermissionId AS Id, Code, Name, Module FROM `Permission` WHERE PermissionId IN @Ids OR Code IN @Codes",
            new { Ids = idList, Codes = codeList }, ct);
    }

    public async Task<bool> NameExistsAsync(string name, long? excludeId, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        var count = await conn.ScalarAsync<int>(
            "SELECT COUNT(1) FROM `Role` WHERE Name = @Name AND (@ExcludeId IS NULL OR RoleId <> @ExcludeId)",
            new { Name = name, ExcludeId = excludeId }, ct);
        return count > 0;
    }

    public async Task<long> CreateAsync(string name, string? description, long actorId, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        return await conn.InsertAsync(
            "INSERT INTO `Role` (Name, Description, IsSystem, IsActive, CreatedBy) VALUES (@Name, @Description, 0, 1, @ActorId)",
            new { Name = name, Description = description, ActorId = actorId }, ct);
    }

    public async Task UpdateAsync(long id, string name, string? description, bool isActive, long actorId, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        await conn.ExecAsync(
            "UPDATE `Role` SET Name = @Name, Description = @Description, IsActive = @IsActive, ModifiedBy = @ActorId WHERE RoleId = @Id",
            new { Id = id, Name = name, Description = description, IsActive = isActive, ActorId = actorId }, ct);
    }

    public async Task DeleteAsync(long id, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        await using var tx = await conn.BeginTransactionAsync(ct);
        try
        {
            // RolePermission rows cascade; role-addressed notifications would block the delete (FK).
            await conn.ExecuteAsync(new CommandDefinition("DELETE FROM `Notification` WHERE RoleId = @Id", new { Id = id }, tx, cancellationToken: ct));
            await conn.ExecuteAsync(new CommandDefinition("DELETE FROM `Role` WHERE RoleId = @Id AND IsSystem = 0", new { Id = id }, tx, cancellationToken: ct));
            await tx.CommitAsync(ct);
        }
        catch
        {
            await tx.RollbackAsync(CancellationToken.None);
            throw;
        }
    }

    public async Task ReplacePermissionsAsync(long roleId, IReadOnlyCollection<long> permissionIds, long actorId, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        await using var tx = await conn.BeginTransactionAsync(ct);
        try
        {
            await conn.ExecuteAsync(new CommandDefinition("DELETE FROM `RolePermission` WHERE RoleId = @RoleId", new { RoleId = roleId }, tx, cancellationToken: ct));
            foreach (var permissionId in permissionIds)
            {
                await conn.ExecuteAsync(new CommandDefinition(
                    "INSERT INTO `RolePermission` (RoleId, PermissionId, CreatedBy) VALUES (@RoleId, @PermissionId, @ActorId)",
                    new { RoleId = roleId, PermissionId = permissionId, ActorId = actorId }, tx, cancellationToken: ct));
            }
            await tx.CommitAsync(ct);
        }
        catch
        {
            await tx.RollbackAsync(CancellationToken.None);
            throw;
        }
    }
}
