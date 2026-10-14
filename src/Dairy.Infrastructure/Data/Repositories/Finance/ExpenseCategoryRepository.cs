using Dairy.Application.Common;
using Dairy.Application.DTOs.Finance;
using Dairy.Application.Interfaces;
using Dairy.Application.Interfaces.Finance;
using Dairy.Application.Requests.Finance;

namespace Dairy.Infrastructure.Data.Repositories.Finance;

public class ExpenseCategoryRepository : IExpenseCategoryRepository
{
    private static readonly Dictionary<string, string> SortColumns = new(StringComparer.OrdinalIgnoreCase)
    {
        ["id"] = "c.ExpenseCategoryId",
        ["code"] = "c.Code",
        ["name"] = "c.Name",
        ["isActive"] = "c.IsActive"
    };

    private const string Select =
        "SELECT c.ExpenseCategoryId AS Id, c.Code, c.Name, c.Description, c.IsActive," +
        " (SELECT COUNT(*) FROM `Expense` e WHERE e.ExpenseCategoryId = c.ExpenseCategoryId) AS ExpenseCount" +
        " FROM `ExpenseCategory` c";

    private const string Where =
        " WHERE (@Search IS NULL OR c.Code LIKE @Search OR c.Name LIKE @Search OR c.Description LIKE @Search)" +
        " AND (@IsActive IS NULL OR c.IsActive = @IsActive)";

    private readonly IDbConnectionFactory _db;

    public ExpenseCategoryRepository(IDbConnectionFactory db) => _db = db;

    public async Task<PagedResult<ExpenseCategoryDto>> GetPagedAsync(ExpenseCategoryQuery query, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        var param = new { Search = query.SearchPattern, query.IsActive };
        return await conn.QueryPagedAsync<ExpenseCategoryDto>(
            Select + Where,
            "SELECT COUNT(*) FROM `ExpenseCategory` c" + Where,
            param, query, SortColumns, "c.Name ASC", ct);
    }

    public async Task<ExpenseCategoryDto?> GetByIdAsync(long id, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        return await conn.QueryFirstOrNullAsync<ExpenseCategoryDto>(Select + " WHERE c.ExpenseCategoryId = @Id", new { Id = id }, ct);
    }

    public async Task<bool> ExistsAsync(string code, string name, long? excludeId, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        var count = await conn.ScalarAsync<int>(
            "SELECT COUNT(1) FROM `ExpenseCategory` WHERE (Code = @Code OR Name = @Name)" +
            " AND (@ExcludeId IS NULL OR ExpenseCategoryId <> @ExcludeId)",
            new { Code = code, Name = name, ExcludeId = excludeId }, ct);
        return count > 0;
    }

    public async Task<long> CreateAsync(CreateExpenseCategoryRequest request, long userId, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        return await conn.InsertAsync(
            "INSERT INTO `ExpenseCategory` (Code, Name, Description, IsActive, CreatedBy) VALUES (@Code, @Name, @Description, 1, @UserId)",
            new { request.Code, request.Name, request.Description, UserId = userId }, ct);
    }

    public async Task UpdateAsync(long id, UpdateExpenseCategoryRequest request, bool isActive, long userId, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        await conn.ExecAsync(
            "UPDATE `ExpenseCategory` SET Code = @Code, Name = @Name, Description = @Description, IsActive = @IsActive, ModifiedBy = @UserId" +
            " WHERE ExpenseCategoryId = @Id",
            new { Id = id, request.Code, request.Name, request.Description, IsActive = isActive, UserId = userId }, ct);
    }
}
