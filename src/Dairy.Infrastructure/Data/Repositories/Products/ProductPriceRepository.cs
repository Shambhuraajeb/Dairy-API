using Dairy.Application.BusinessRules;
using Dairy.Application.Common;
using Dairy.Application.DTOs.Products;
using Dairy.Application.Interfaces;
using Dairy.Application.Interfaces.Products;
using Dairy.Application.Requests.Products;
using Dapper;

namespace Dairy.Infrastructure.Data.Repositories.Products;

public class ProductPriceRepository : IProductPriceRepository
{
    private static readonly Dictionary<string, string> SortColumns = new(StringComparer.OrdinalIgnoreCase)
    {
        ["id"] = "pp.ProductPriceId",
        ["productName"] = "p.Name",
        ["customerType"] = "pp.CustomerType",
        ["price"] = "pp.Price",
        ["effectiveFrom"] = "pp.EffectiveFrom",
        ["effectiveTo"] = "pp.EffectiveTo"
    };

    private const string FromJoin = " FROM `ProductPrice` pp JOIN `Product` p ON p.ProductId = pp.ProductId";

    private const string Select =
        "SELECT pp.ProductPriceId AS Id, pp.ProductId, p.Name AS ProductName, p.ProductCode, pp.CustomerType, pp.Price, pp.EffectiveFrom, pp.EffectiveTo," +
        " pp.IsActive, pp.CreatedDate" + FromJoin;

    private const string Where =
        " WHERE (@Search IS NULL OR p.Name LIKE @Search OR p.ProductCode LIKE @Search)" +
        " AND (@ProductId IS NULL OR pp.ProductId = @ProductId)" +
        " AND (@CustomerType IS NULL OR pp.CustomerType = @CustomerType)" +
        " AND (@IsActive IS NULL OR pp.IsActive = @IsActive)" +
        " AND (@CurrentOnly IS NULL OR @CurrentOnly = 0 OR (pp.IsActive = 1 AND pp.EffectiveFrom <= CURDATE() AND (pp.EffectiveTo IS NULL OR pp.EffectiveTo >= CURDATE())))";

    private readonly IDbConnectionFactory _db;

    public ProductPriceRepository(IDbConnectionFactory db) => _db = db;

    public async Task<PagedResult<ProductPriceDto>> GetPagedAsync(ProductPriceQuery query, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        var param = new
        {
            Search = query.SearchPattern,
            query.ProductId,
            CustomerType = SalesValues.Clean(query.CustomerType),
            query.IsActive,
            query.CurrentOnly
        };
        return await conn.QueryPagedAsync<ProductPriceDto>(Select + Where, "SELECT COUNT(*)" + FromJoin + Where, param, query, SortColumns,
            "pp.EffectiveFrom DESC, pp.ProductPriceId DESC", ct);
    }

    public async Task<ProductPriceDto?> GetByIdAsync(long id, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        return await conn.QueryFirstOrNullAsync<ProductPriceDto>(Select + " WHERE pp.ProductPriceId = @Id", new { Id = id }, ct);
    }

    public async Task<bool> ProductExistsAsync(long productId, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        return await conn.ScalarAsync<int>("SELECT COUNT(1) FROM `Product` WHERE ProductId = @Id AND IsActive = 1", new { Id = productId }, ct) > 0;
    }

    public async Task<bool> HasLaterOverlapAsync(long productId, string customerType, DateTime from, DateTime? to, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        return await conn.ScalarAsync<int>(
            "SELECT COUNT(1) FROM `ProductPrice` WHERE ProductId = @ProductId AND CustomerType = @CustomerType AND IsActive = 1" +
            " AND EffectiveFrom >= @From AND EffectiveFrom <= COALESCE(@To, '9999-12-31')",
            new { ProductId = productId, CustomerType = customerType, From = from.Date, To = to?.Date }, ct) > 0;
    }

    public async Task<long> CreateAsync(CreateProductPriceRequest r, long userId, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        await using var tx = await conn.BeginTransactionAsync(ct);

        var p = new
        {
            r.ProductId,
            r.CustomerType,
            r.Price,
            From = r.EffectiveFrom.Date,
            To = r.EffectiveTo?.Date,
            UserId = userId
        };

        // close the currently open price that this one supersedes
        await conn.ExecuteAsync(new CommandDefinition(
            "UPDATE `ProductPrice` SET EffectiveTo = DATE_SUB(@From, INTERVAL 1 DAY), ModifiedBy = @UserId" +
            " WHERE ProductId = @ProductId AND CustomerType = @CustomerType AND IsActive = 1" +
            " AND EffectiveFrom < @From AND (EffectiveTo IS NULL OR EffectiveTo >= @From)",
            p, tx, cancellationToken: ct));

        var id = await conn.ExecuteScalarAsync<long>(new CommandDefinition(
            "INSERT INTO `ProductPrice` (ProductId, CustomerType, Price, EffectiveFrom, EffectiveTo, CreatedBy)" +
            " VALUES (@ProductId, @CustomerType, @Price, @From, @To, @UserId); SELECT LAST_INSERT_ID();",
            p, tx, cancellationToken: ct));

        await tx.CommitAsync(ct);
        return id;
    }

    public async Task DeactivateAsync(long id, long userId, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        await conn.ExecAsync("UPDATE `ProductPrice` SET IsActive = 0, ModifiedBy = @UserId WHERE ProductPriceId = @Id", new { Id = id, UserId = userId }, ct);
    }
}
