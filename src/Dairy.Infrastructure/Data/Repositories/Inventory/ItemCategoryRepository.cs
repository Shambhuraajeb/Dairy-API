using Dairy.Application.Common;
using Dairy.Application.DTOs.Inventory;
using Dairy.Application.Interfaces;
using Dairy.Application.Interfaces.Inventory;
using Dairy.Application.Requests.Inventory;

namespace Dairy.Infrastructure.Data.Repositories.Inventory;

public class ItemCategoryRepository : IItemCategoryRepository
{
    private static readonly Dictionary<string, string> SortColumns = new(StringComparer.OrdinalIgnoreCase)
    {
        ["id"] = "c.ItemCategoryId",
        ["code"] = "c.Code",
        ["name"] = "c.Name",
        ["isActive"] = "c.IsActive"
    };

    private const string Select =
        "SELECT c.ItemCategoryId AS Id, c.Code, c.Name, c.Description, c.IsActive," +
        " (SELECT COUNT(*) FROM `InventoryItem` i WHERE i.ItemCategoryId = c.ItemCategoryId) AS ItemCount" +
        " FROM `ItemCategory` c";

    private const string Where =
        " WHERE (@Search IS NULL OR c.Code LIKE @Search OR c.Name LIKE @Search OR c.Description LIKE @Search)" +
        " AND (@IsActive IS NULL OR c.IsActive = @IsActive)";

    private readonly IDbConnectionFactory _db;

    public ItemCategoryRepository(IDbConnectionFactory db) => _db = db;

    public async Task<PagedResult<ItemCategoryDto>> GetPagedAsync(ItemCategoryQuery query, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        var param = new { Search = query.SearchPattern, query.IsActive };
        return await conn.QueryPagedAsync<ItemCategoryDto>(
            Select + Where,
            "SELECT COUNT(*) FROM `ItemCategory` c" + Where,
            param, query, SortColumns, "c.Name ASC", ct);
    }

    public async Task<ItemCategoryDto?> GetByIdAsync(long id, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        return await conn.QueryFirstOrNullAsync<ItemCategoryDto>(Select + " WHERE c.ItemCategoryId = @Id", new { Id = id }, ct);
    }

    public async Task<bool> ExistsAsync(string code, string name, long? excludeId, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        var count = await conn.ScalarAsync<int>(
            "SELECT COUNT(1) FROM `ItemCategory` WHERE (Code = @Code OR Name = @Name)" +
            " AND (@ExcludeId IS NULL OR ItemCategoryId <> @ExcludeId)",
            new { Code = code, Name = name, ExcludeId = excludeId }, ct);
        return count > 0;
    }

    public async Task<long> CreateAsync(CreateItemCategoryRequest request, long userId, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        return await conn.InsertAsync(
            "INSERT INTO `ItemCategory` (Code, Name, Description, IsActive, CreatedBy) VALUES (@Code, @Name, @Description, 1, @UserId)",
            new { request.Code, request.Name, request.Description, UserId = userId }, ct);
    }

    public async Task UpdateAsync(long id, UpdateItemCategoryRequest request, bool isActive, long userId, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        await conn.ExecAsync(
            "UPDATE `ItemCategory` SET Code = @Code, Name = @Name, Description = @Description, IsActive = @IsActive, ModifiedBy = @UserId" +
            " WHERE ItemCategoryId = @Id",
            new { Id = id, request.Code, request.Name, request.Description, IsActive = isActive, UserId = userId }, ct);
    }
}
