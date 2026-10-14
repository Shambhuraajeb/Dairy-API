using Dairy.Application.BusinessRules;
using Dairy.Application.Common;
using Dairy.Application.DTOs.Products;
using Dairy.Application.Interfaces;
using Dairy.Application.Interfaces.Products;
using Dairy.Application.Requests.Products;

namespace Dairy.Infrastructure.Data.Repositories.Products;

public class ProductCategoryRepository : IProductCategoryRepository
{
    private static readonly Dictionary<string, string> SortColumns = new(StringComparer.OrdinalIgnoreCase)
    {
        ["id"] = "pc.ProductCategoryId",
        ["code"] = "pc.Code",
        ["name"] = "pc.Name",
        ["isActive"] = "pc.IsActive"
    };

    private const string Select =
        "SELECT pc.ProductCategoryId AS Id, pc.Code, pc.Name, pc.Description, pc.IsActive," +
        " (SELECT COUNT(*) FROM `Product` p WHERE p.ProductCategoryId = pc.ProductCategoryId AND p.IsActive = 1) AS ProductCount" +
        " FROM `ProductCategory` pc";

    private const string Where =
        " WHERE (@Search IS NULL OR pc.Code LIKE @Search OR pc.Name LIKE @Search)" +
        " AND (@IsActive IS NULL OR pc.IsActive = @IsActive)";

    private readonly IDbConnectionFactory _db;

    public ProductCategoryRepository(IDbConnectionFactory db) => _db = db;

    public async Task<PagedResult<ProductCategoryDto>> GetPagedAsync(ProductCategoryQuery query, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        var param = new { Search = query.SearchPattern, query.IsActive };
        return await conn.QueryPagedAsync<ProductCategoryDto>(Select + Where, "SELECT COUNT(*) FROM `ProductCategory` pc" + Where, param, query, SortColumns, "pc.Name ASC", ct);
    }

    public async Task<ProductCategoryDto?> GetByIdAsync(long id, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        return await conn.QueryFirstOrNullAsync<ProductCategoryDto>(Select + " WHERE pc.ProductCategoryId = @Id", new { Id = id }, ct);
    }

    public async Task<bool> CodeExistsAsync(string code, long? excludeId, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        return await conn.ScalarAsync<int>(
            "SELECT COUNT(1) FROM `ProductCategory` WHERE Code = @Code AND (@ExcludeId IS NULL OR ProductCategoryId <> @ExcludeId)",
            new { Code = code, ExcludeId = excludeId }, ct) > 0;
    }

    public async Task<bool> NameExistsAsync(string name, long? excludeId, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        return await conn.ScalarAsync<int>(
            "SELECT COUNT(1) FROM `ProductCategory` WHERE Name = @Name AND (@ExcludeId IS NULL OR ProductCategoryId <> @ExcludeId)",
            new { Name = name, ExcludeId = excludeId }, ct) > 0;
    }

    public async Task<CreatedResult> CreateAsync(string? code, CreateProductCategoryRequest r, long userId, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        var (id, finalCode) = await AutoCode.InsertAsync(conn, "ProductCategory", "Code", "PCAT", 3, code, c => conn.InsertAsync(
            "INSERT INTO `ProductCategory` (Code, Name, Description, CreatedBy) VALUES (@Code, @Name, @Description, @UserId)",
            new { Code = c, Name = r.Name.Trim(), Description = SalesValues.Clean(r.Description), UserId = userId }, ct), ct);
        return new CreatedResult { Id = id, Number = finalCode };
    }

    public async Task UpdateAsync(long id, UpdateProductCategoryRequest r, bool isActive, long userId, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        await conn.ExecAsync(
            "UPDATE `ProductCategory` SET Code = @Code, Name = @Name, Description = @Description, IsActive = @IsActive, ModifiedBy = @UserId WHERE ProductCategoryId = @Id",
            new { Id = id, Code = r.Code.Trim().ToUpperInvariant(), Name = r.Name.Trim(), Description = SalesValues.Clean(r.Description), IsActive = isActive, UserId = userId }, ct);
    }

    public async Task<int> CountActiveProductsAsync(long id, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        return await conn.ScalarAsync<int>("SELECT COUNT(1) FROM `Product` WHERE ProductCategoryId = @Id AND IsActive = 1", new { Id = id }, ct);
    }

    public async Task SetActiveAsync(long id, bool isActive, long userId, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        await conn.ExecAsync("UPDATE `ProductCategory` SET IsActive = @IsActive, ModifiedBy = @UserId WHERE ProductCategoryId = @Id",
            new { Id = id, IsActive = isActive, UserId = userId }, ct);
    }
}
