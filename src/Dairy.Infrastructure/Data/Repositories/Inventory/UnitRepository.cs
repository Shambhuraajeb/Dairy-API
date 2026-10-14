using Dairy.Application.Common;
using Dairy.Application.DTOs.Inventory;
using Dairy.Application.Interfaces;
using Dairy.Application.Interfaces.Inventory;
using Dairy.Application.Requests.Inventory;

namespace Dairy.Infrastructure.Data.Repositories.Inventory;

public class UnitRepository : IUnitRepository
{
    private static readonly Dictionary<string, string> SortColumns = new(StringComparer.OrdinalIgnoreCase)
    {
        ["id"] = "u.UnitId",
        ["code"] = "u.Code",
        ["name"] = "u.Name",
        ["isActive"] = "u.IsActive"
    };

    private const string Select = "SELECT u.UnitId AS Id, u.Code, u.Name, u.IsActive FROM `Unit` u";

    private const string Where =
        " WHERE (@Search IS NULL OR u.Code LIKE @Search OR u.Name LIKE @Search)" +
        " AND (@IsActive IS NULL OR u.IsActive = @IsActive)";

    private readonly IDbConnectionFactory _db;

    public UnitRepository(IDbConnectionFactory db) => _db = db;

    public async Task<PagedResult<UnitDto>> GetPagedAsync(UnitQuery query, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        var param = new { Search = query.SearchPattern, query.IsActive };
        return await conn.QueryPagedAsync<UnitDto>(
            Select + Where,
            "SELECT COUNT(*) FROM `Unit` u" + Where,
            param, query, SortColumns, "u.Name ASC", ct);
    }

    public async Task<UnitDto?> GetByIdAsync(long id, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        return await conn.QueryFirstOrNullAsync<UnitDto>(Select + " WHERE u.UnitId = @Id", new { Id = id }, ct);
    }

    public async Task<bool> CodeExistsAsync(string code, long? excludeId, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        var count = await conn.ScalarAsync<int>(
            "SELECT COUNT(1) FROM `Unit` WHERE Code = @Code AND (@ExcludeId IS NULL OR UnitId <> @ExcludeId)",
            new { Code = code, ExcludeId = excludeId }, ct);
        return count > 0;
    }

    public async Task<long> CreateAsync(string code, string name, long userId, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        return await conn.InsertAsync(
            "INSERT INTO `Unit` (Code, Name, IsActive, CreatedBy) VALUES (@Code, @Name, 1, @UserId)",
            new { Code = code, Name = name, UserId = userId }, ct);
    }

    public async Task UpdateAsync(long id, string code, string name, bool isActive, long userId, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        await conn.ExecAsync(
            "UPDATE `Unit` SET Code = @Code, Name = @Name, IsActive = @IsActive, ModifiedBy = @UserId WHERE UnitId = @Id",
            new { Id = id, Code = code, Name = name, IsActive = isActive, UserId = userId }, ct);
    }
}
