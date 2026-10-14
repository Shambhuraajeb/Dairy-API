using Dairy.Application.Common;
using Dairy.Application.DTOs.Hr;
using Dairy.Application.Interfaces;
using Dairy.Application.Interfaces.Hr;
using Dairy.Application.Requests.Hr;

namespace Dairy.Infrastructure.Data.Repositories.Hr;

public class DesignationRepository : IDesignationRepository
{
    private static readonly Dictionary<string, string> SortColumns = new(StringComparer.OrdinalIgnoreCase)
    {
        ["id"] = "d.DesignationId",
        ["name"] = "d.Name",
        ["department"] = "d.Department",
        ["isActive"] = "d.IsActive"
    };

    private const string Select =
        "SELECT d.DesignationId AS Id, d.Name, d.Department, d.Description, d.IsActive, " +
        "(SELECT COUNT(*) FROM `Employee` e WHERE e.DesignationId = d.DesignationId AND e.IsActive = 1) AS EmployeeCount " +
        "FROM `Designation` d";

    private const string Where =
        " WHERE (@Search IS NULL OR d.Name LIKE @Search OR d.Department LIKE @Search)" +
        " AND (@IsActive IS NULL OR d.IsActive = @IsActive)";

    private readonly IDbConnectionFactory _db;

    public DesignationRepository(IDbConnectionFactory db) => _db = db;

    public async Task<PagedResult<DesignationDto>> GetPagedAsync(DesignationQuery query, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        var param = new { Search = query.SearchPattern, query.IsActive };
        return await conn.QueryPagedAsync<DesignationDto>(
            Select + Where,
            "SELECT COUNT(*) FROM `Designation` d" + Where,
            param, query, SortColumns, "d.Name ASC", ct);
    }

    public async Task<DesignationDto?> GetByIdAsync(long id, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        return await conn.QueryFirstOrNullAsync<DesignationDto>(Select + " WHERE d.DesignationId = @Id", new { Id = id }, ct);
    }

    public async Task<bool> NameExistsAsync(string name, long? excludeId, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        var count = await conn.ScalarAsync<int>(
            "SELECT COUNT(1) FROM `Designation` WHERE Name = @Name AND (@ExcludeId IS NULL OR DesignationId <> @ExcludeId)",
            new { Name = name, ExcludeId = excludeId }, ct);
        return count > 0;
    }

    public async Task<long> CreateAsync(string name, string? department, string? description, long userId, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        return await conn.InsertAsync(
            "INSERT INTO `Designation` (Name, Department, Description, IsActive, CreatedBy) VALUES (@Name, @Department, @Description, 1, @UserId)",
            new { Name = name, Department = department, Description = description, UserId = userId }, ct);
    }

    public async Task UpdateAsync(long id, string name, string? department, string? description, bool isActive, long userId, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        await conn.ExecAsync(
            "UPDATE `Designation` SET Name = @Name, Department = @Department, Description = @Description, IsActive = @IsActive, ModifiedBy = @UserId " +
            "WHERE DesignationId = @Id",
            new { Id = id, Name = name, Department = department, Description = description, IsActive = isActive, UserId = userId }, ct);
    }
}
