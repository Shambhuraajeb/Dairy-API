using Dairy.Application.BusinessRules;
using Dairy.Application.Common;
using Dairy.Application.DTOs.Subscriptions;
using Dairy.Application.Interfaces;
using Dairy.Application.Interfaces.Subscriptions;
using Dairy.Application.Requests.Subscriptions;

namespace Dairy.Infrastructure.Data.Repositories.Subscriptions;

public class SubscriptionPlanRepository : ISubscriptionPlanRepository
{
    private static readonly Dictionary<string, string> SortColumns = new(StringComparer.OrdinalIgnoreCase)
    {
        ["id"] = "sp.SubscriptionPlanId",
        ["planCode"] = "sp.PlanCode",
        ["name"] = "sp.Name",
        ["frequency"] = "sp.Frequency",
        ["defaultQuantity"] = "sp.DefaultQuantity",
        ["isActive"] = "sp.IsActive"
    };

    private const string Select =
        "SELECT sp.SubscriptionPlanId AS Id, sp.PlanCode, sp.Name, sp.Description, sp.Frequency, sp.DaysOfWeek, sp.ProductId, p.Name AS ProductName," +
        " sp.DefaultQuantity, sp.PricePerUnit, sp.DiscountPercent, sp.BillingCycle, sp.MinimumDays, sp.IsActive," +
        " (SELECT COUNT(*) FROM `CustomerSubscription` s WHERE s.SubscriptionPlanId = sp.SubscriptionPlanId AND s.Status IN ('Active', 'Paused') AND s.IsActive = 1) AS ActiveSubscriptions" +
        " FROM `SubscriptionPlan` sp LEFT JOIN `Product` p ON p.ProductId = sp.ProductId";

    private const string CountFrom = " FROM `SubscriptionPlan` sp LEFT JOIN `Product` p ON p.ProductId = sp.ProductId";

    private const string Where =
        " WHERE (@Search IS NULL OR sp.PlanCode LIKE @Search OR sp.Name LIKE @Search OR p.Name LIKE @Search)" +
        " AND (@Frequency IS NULL OR sp.Frequency = @Frequency)" +
        " AND (@ProductId IS NULL OR sp.ProductId = @ProductId)" +
        " AND (@IsActive IS NULL OR sp.IsActive = @IsActive)";

    private readonly IDbConnectionFactory _db;

    public SubscriptionPlanRepository(IDbConnectionFactory db) => _db = db;

    public async Task<PagedResult<SubscriptionPlanDto>> GetPagedAsync(SubscriptionPlanQuery query, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        var param = new { Search = query.SearchPattern, Frequency = SalesValues.Clean(query.Frequency), query.ProductId, query.IsActive };
        return await conn.QueryPagedAsync<SubscriptionPlanDto>(Select + Where, "SELECT COUNT(*)" + CountFrom + Where, param, query, SortColumns, "sp.Name ASC", ct);
    }

    public async Task<SubscriptionPlanDto?> GetByIdAsync(long id, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        return await conn.QueryFirstOrNullAsync<SubscriptionPlanDto>(Select + " WHERE sp.SubscriptionPlanId = @Id", new { Id = id }, ct);
    }

    public async Task<bool> CodeExistsAsync(string code, long? excludeId, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        return await conn.ScalarAsync<int>(
            "SELECT COUNT(1) FROM `SubscriptionPlan` WHERE PlanCode = @Code AND (@ExcludeId IS NULL OR SubscriptionPlanId <> @ExcludeId)",
            new { Code = code, ExcludeId = excludeId }, ct) > 0;
    }

    public async Task<bool> ProductExistsAsync(long productId, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        return await conn.ScalarAsync<int>("SELECT COUNT(1) FROM `Product` WHERE ProductId = @Id AND IsActive = 1", new { Id = productId }, ct) > 0;
    }

    public async Task<CreatedResult> CreateAsync(string? code, CreateSubscriptionPlanRequest r, long userId, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        var (id, finalCode) = await AutoCode.InsertAsync(conn, "SubscriptionPlan", "PlanCode", "PLN", 4, code, c => conn.InsertAsync(
            "INSERT INTO `SubscriptionPlan` (PlanCode, Name, Description, Frequency, DaysOfWeek, ProductId, DefaultQuantity, PricePerUnit, DiscountPercent, BillingCycle," +
            " MinimumDays, CreatedBy)" +
            " VALUES (@Code, @Name, @Description, @Frequency, @DaysOfWeek, @ProductId, @DefaultQuantity, @PricePerUnit, @DiscountPercent, @BillingCycle, @MinimumDays, @UserId)",
            new
            {
                Code = c,
                Name = r.Name.Trim(),
                Description = SalesValues.Clean(r.Description),
                r.Frequency,
                DaysOfWeek = SalesValues.Clean(r.DaysOfWeek),
                r.ProductId,
                r.DefaultQuantity,
                r.PricePerUnit,
                r.DiscountPercent,
                r.BillingCycle,
                r.MinimumDays,
                UserId = userId
            }, ct), ct);
        return new CreatedResult { Id = id, Number = finalCode };
    }

    public async Task UpdateAsync(long id, UpdateSubscriptionPlanRequest r, bool isActive, long userId, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        await conn.ExecAsync(
            "UPDATE `SubscriptionPlan` SET Name = @Name, Description = @Description, Frequency = @Frequency, DaysOfWeek = @DaysOfWeek, ProductId = @ProductId," +
            " DefaultQuantity = @DefaultQuantity, PricePerUnit = @PricePerUnit, DiscountPercent = @DiscountPercent, BillingCycle = @BillingCycle," +
            " MinimumDays = @MinimumDays, IsActive = @IsActive, ModifiedBy = @UserId WHERE SubscriptionPlanId = @Id",
            new
            {
                Id = id,
                Name = r.Name.Trim(),
                Description = SalesValues.Clean(r.Description),
                r.Frequency,
                DaysOfWeek = SalesValues.Clean(r.DaysOfWeek),
                r.ProductId,
                r.DefaultQuantity,
                r.PricePerUnit,
                r.DiscountPercent,
                r.BillingCycle,
                r.MinimumDays,
                IsActive = isActive,
                UserId = userId
            }, ct);
    }

    public async Task<int> CountOpenSubscriptionsAsync(long id, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        return await conn.ScalarAsync<int>(
            "SELECT COUNT(1) FROM `CustomerSubscription` WHERE SubscriptionPlanId = @Id AND IsActive = 1 AND Status IN ('Active', 'Paused')",
            new { Id = id }, ct);
    }

    public async Task SetActiveAsync(long id, bool isActive, long userId, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        await conn.ExecAsync("UPDATE `SubscriptionPlan` SET IsActive = @IsActive, ModifiedBy = @UserId WHERE SubscriptionPlanId = @Id",
            new { Id = id, IsActive = isActive, UserId = userId }, ct);
    }
}
