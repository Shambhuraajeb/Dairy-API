using Dairy.Application.BusinessRules;
using Dairy.Application.Common;
using Dairy.Application.DTOs.Products;
using Dairy.Application.Interfaces;
using Dairy.Application.Interfaces.Products;
using Dairy.Application.Requests.Products;

namespace Dairy.Infrastructure.Data.Repositories.Products;

public class ProductRepository : IProductRepository
{
    private static readonly Dictionary<string, string> SortColumns = new(StringComparer.OrdinalIgnoreCase)
    {
        ["id"] = "p.ProductId",
        ["productCode"] = "p.ProductCode",
        ["name"] = "p.Name",
        ["categoryName"] = "pc.Name",
        ["defaultPrice"] = "p.DefaultPrice",
        ["currentStock"] = "p.CurrentStock",
        ["isActive"] = "p.IsActive"
    };

    private const string PriceSubquery =
        "(SELECT pp.Price FROM `ProductPrice` pp WHERE pp.ProductId = p.ProductId AND pp.CustomerType = '{TYPE}' AND pp.IsActive = 1" +
        " AND pp.EffectiveFrom <= CURDATE() AND (pp.EffectiveTo IS NULL OR pp.EffectiveTo >= CURDATE())" +
        " ORDER BY pp.EffectiveFrom DESC, pp.ProductPriceId DESC LIMIT 1)";

    private static readonly string Select =
        "SELECT p.ProductId AS Id, p.ProductCode, p.Name, p.ProductCategoryId, pc.Name AS CategoryName, p.UnitId, u.Code AS UnitCode, u.Name AS UnitName," +
        " p.Description, p.TaxPercent, p.HsnCode, p.IsMilkBased, p.MilkLitersPerUnit, p.DefaultPrice," +
        " COALESCE(" + PriceSubquery.Replace("{TYPE}", "Retail") + ", p.DefaultPrice) AS CurrentPrice," +
        " COALESCE(" + PriceSubquery.Replace("{TYPE}", "Distributor") + ", p.DefaultPrice) AS DistributorPrice," +
        " p.CurrentStock, p.ReorderLevel, p.ShelfLifeDays, p.ImagePath, p.IsActive" +
        " FROM `Product` p JOIN `ProductCategory` pc ON pc.ProductCategoryId = p.ProductCategoryId JOIN `Unit` u ON u.UnitId = p.UnitId";

    private const string CountFrom = " FROM `Product` p JOIN `ProductCategory` pc ON pc.ProductCategoryId = p.ProductCategoryId";

    private const string Where =
        " WHERE (@Search IS NULL OR p.ProductCode LIKE @Search OR p.Name LIKE @Search OR pc.Name LIKE @Search)" +
        " AND (@CategoryId IS NULL OR p.ProductCategoryId = @CategoryId)" +
        " AND (@IsActive IS NULL OR p.IsActive = @IsActive)" +
        " AND (@IsMilkBased IS NULL OR p.IsMilkBased = @IsMilkBased)" +
        " AND (@LowStock IS NULL OR @LowStock = 0 OR (p.ReorderLevel > 0 AND p.CurrentStock <= p.ReorderLevel))";

    private readonly IDbConnectionFactory _db;

    public ProductRepository(IDbConnectionFactory db) => _db = db;

    public async Task<PagedResult<ProductDto>> GetPagedAsync(ProductQuery query, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        var param = new { Search = query.SearchPattern, query.CategoryId, query.IsActive, query.IsMilkBased, query.LowStock };
        return await conn.QueryPagedAsync<ProductDto>(Select + Where, "SELECT COUNT(*)" + CountFrom + Where, param, query, SortColumns, "p.Name ASC", ct);
    }

    public async Task<ProductDto?> GetByIdAsync(long id, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        return await conn.QueryFirstOrNullAsync<ProductDto>(Select + " WHERE p.ProductId = @Id", new { Id = id }, ct);
    }

    public async Task<bool> CodeExistsAsync(string code, long? excludeId, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        return await conn.ScalarAsync<int>(
            "SELECT COUNT(1) FROM `Product` WHERE ProductCode = @Code AND (@ExcludeId IS NULL OR ProductId <> @ExcludeId)",
            new { Code = code, ExcludeId = excludeId }, ct) > 0;
    }

    public async Task<bool> CategoryExistsAsync(long categoryId, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        return await conn.ScalarAsync<int>("SELECT COUNT(1) FROM `ProductCategory` WHERE ProductCategoryId = @Id AND IsActive = 1", new { Id = categoryId }, ct) > 0;
    }

    public async Task<bool> UnitExistsAsync(long unitId, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        return await conn.ScalarAsync<int>("SELECT COUNT(1) FROM `Unit` WHERE UnitId = @Id AND IsActive = 1", new { Id = unitId }, ct) > 0;
    }

    public async Task<CreatedResult> CreateAsync(string? code, CreateProductRequest r, long userId, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        var (id, finalCode) = await AutoCode.InsertAsync(conn, "Product", "ProductCode", "PRD", 4, code, c => conn.InsertAsync(
            "INSERT INTO `Product` (ProductCode, Name, ProductCategoryId, UnitId, Description, TaxPercent, HsnCode, IsMilkBased, MilkLitersPerUnit, DefaultPrice," +
            " CurrentStock, ReorderLevel, ShelfLifeDays, CreatedBy)" +
            " VALUES (@Code, @Name, @ProductCategoryId, @UnitId, @Description, @TaxPercent, @HsnCode, @IsMilkBased, @MilkLitersPerUnit, @DefaultPrice," +
            " @CurrentStock, @ReorderLevel, @ShelfLifeDays, @UserId)",
            new
            {
                Code = c,
                Name = r.Name.Trim(),
                r.ProductCategoryId,
                r.UnitId,
                Description = SalesValues.Clean(r.Description),
                r.TaxPercent,
                HsnCode = SalesValues.Clean(r.HsnCode),
                r.IsMilkBased,
                r.MilkLitersPerUnit,
                r.DefaultPrice,
                r.CurrentStock,
                r.ReorderLevel,
                r.ShelfLifeDays,
                UserId = userId
            }, ct), ct);
        return new CreatedResult { Id = id, Number = finalCode };
    }

    public async Task UpdateAsync(long id, UpdateProductRequest r, bool isActive, long userId, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        await conn.ExecAsync(
            "UPDATE `Product` SET ProductCode = @Code, Name = @Name, ProductCategoryId = @ProductCategoryId, UnitId = @UnitId, Description = @Description," +
            " TaxPercent = @TaxPercent, HsnCode = @HsnCode, IsMilkBased = @IsMilkBased, MilkLitersPerUnit = @MilkLitersPerUnit, DefaultPrice = @DefaultPrice," +
            " CurrentStock = COALESCE(@CurrentStock, CurrentStock), ReorderLevel = @ReorderLevel, ShelfLifeDays = @ShelfLifeDays, IsActive = @IsActive, ModifiedBy = @UserId" +
            " WHERE ProductId = @Id",
            new
            {
                Id = id,
                Code = r.ProductCode.Trim().ToUpperInvariant(),
                Name = r.Name.Trim(),
                r.ProductCategoryId,
                r.UnitId,
                Description = SalesValues.Clean(r.Description),
                r.TaxPercent,
                HsnCode = SalesValues.Clean(r.HsnCode),
                r.IsMilkBased,
                r.MilkLitersPerUnit,
                r.DefaultPrice,
                r.CurrentStock,
                r.ReorderLevel,
                r.ShelfLifeDays,
                IsActive = isActive,
                UserId = userId
            }, ct);
    }

    /// <summary>Open orders + active subscriptions that still use the product.</summary>
    public async Task<int> CountOpenOrdersAsync(long id, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        return await conn.ScalarAsync<int>(
            "SELECT (SELECT COUNT(*) FROM `SalesOrderDetail` d JOIN `SalesOrder` o ON o.SalesOrderId = d.SalesOrderId" +
            "         WHERE d.ProductId = @Id AND o.Status IN ('Pending', 'Confirmed', 'Scheduled', 'Dispatched'))" +
            "      + (SELECT COUNT(*) FROM `CustomerSubscription` s WHERE s.ProductId = @Id AND s.IsActive = 1 AND s.Status IN ('Active', 'Paused'))",
            new { Id = id }, ct);
    }

    public async Task SetActiveAsync(long id, bool isActive, long userId, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        await conn.ExecAsync("UPDATE `Product` SET IsActive = @IsActive, ModifiedBy = @UserId WHERE ProductId = @Id",
            new { Id = id, IsActive = isActive, UserId = userId }, ct);
    }

    public async Task<ProductPriceLookupDto?> GetPriceAsync(long productId, string customerType, DateTime date, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        return await conn.QueryFirstOrNullAsync<ProductPriceLookupDto>(
            "SELECT p.ProductId, CAST(@CustomerType AS CHAR) AS CustomerType, CAST(@Date AS DATE) AS `Date`, COALESCE(pp.Price, p.DefaultPrice) AS Price," +
            " IF(pp.Price IS NULL, 'Default', 'PriceList') AS Source, p.TaxPercent" +
            " FROM `Product` p" +
            " LEFT JOIN `ProductPrice` pp ON pp.ProductPriceId = (" +
            "   SELECT x.ProductPriceId FROM `ProductPrice` x WHERE x.ProductId = p.ProductId AND x.CustomerType = @CustomerType AND x.IsActive = 1" +
            "   AND x.EffectiveFrom <= @Date AND (x.EffectiveTo IS NULL OR x.EffectiveTo >= @Date)" +
            "   ORDER BY x.EffectiveFrom DESC, x.ProductPriceId DESC LIMIT 1)" +
            " WHERE p.ProductId = @ProductId",
            new { ProductId = productId, CustomerType = customerType, Date = date.Date }, ct);
    }
}
