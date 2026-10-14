using Dairy.Application.BusinessRules;
using Dairy.Application.Common;
using Dairy.Application.DTOs.Subscriptions;
using Dairy.Application.Interfaces;
using Dairy.Application.Interfaces.Subscriptions;
using Dairy.Application.Requests.Subscriptions;
using Dapper;

namespace Dairy.Infrastructure.Data.Repositories.Subscriptions;

public class SubscriptionRepository : ISubscriptionRepository
{
    private static readonly Dictionary<string, string> SortColumns = new(StringComparer.OrdinalIgnoreCase)
    {
        ["id"] = "s.CustomerSubscriptionId",
        ["subscriptionNumber"] = "s.SubscriptionNumber",
        ["customerName"] = "c.Name",
        ["productName"] = "p.Name",
        ["status"] = "s.Status",
        ["startDate"] = "s.StartDate",
        ["nextOrderDate"] = "s.NextOrderDate",
        ["createdDate"] = "s.CreatedDate"
    };

    private const string FromJoins =
        " FROM `CustomerSubscription` s" +
        " JOIN `Customer` c ON c.CustomerId = s.CustomerId" +
        " JOIN `SubscriptionPlan` sp ON sp.SubscriptionPlanId = s.SubscriptionPlanId" +
        " JOIN `CustomerAddress` a ON a.CustomerAddressId = s.CustomerAddressId" +
        " JOIN `Product` p ON p.ProductId = s.ProductId";

    private const string Select =
        "SELECT s.CustomerSubscriptionId AS Id, s.SubscriptionNumber, s.CustomerId, c.Name AS CustomerName, c.CustomerCode, s.SubscriptionPlanId, sp.Name AS PlanName," +
        " s.CustomerAddressId, CONCAT_WS(', ', a.Line1, a.City) AS AddressText, s.ProductId, p.Name AS ProductName, s.Quantity, s.UnitPrice, s.Frequency, s.DaysOfWeek," +
        " s.StartDate, s.EndDate, s.DeliverySlot, s.Status, s.NextOrderDate, s.LastOrderDate, s.CancelledDate, s.CancelReason, s.Remarks, s.CreatedDate" + FromJoins;

    private const string Where =
        " WHERE s.IsActive = 1" +
        " AND (@Search IS NULL OR s.SubscriptionNumber LIKE @Search OR c.Name LIKE @Search OR c.CustomerCode LIKE @Search OR p.Name LIKE @Search)" +
        " AND (@CustomerId IS NULL OR s.CustomerId = @CustomerId)" +
        " AND (@Status IS NULL OR s.Status = @Status)" +
        " AND (@ProductId IS NULL OR s.ProductId = @ProductId)" +
        " AND (@Slot IS NULL OR s.DeliverySlot = @Slot)";

    private readonly IDbConnectionFactory _db;

    public SubscriptionRepository(IDbConnectionFactory db) => _db = db;

    public async Task<PagedResult<SubscriptionDto>> GetPagedAsync(SubscriptionQuery query, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        var param = new
        {
            Search = query.SearchPattern,
            query.CustomerId,
            Status = SalesValues.Clean(query.Status),
            query.ProductId,
            Slot = SalesValues.Clean(query.Slot)
        };
        return await conn.QueryPagedAsync<SubscriptionDto>(Select + Where, "SELECT COUNT(*)" + FromJoins + Where, param, query, SortColumns, "s.CustomerSubscriptionId DESC", ct);
    }

    public async Task<SubscriptionDetailDto?> GetByIdAsync(long id, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        var dto = await conn.QueryFirstOrNullAsync<SubscriptionDetailDto>(Select + " WHERE s.CustomerSubscriptionId = @Id AND s.IsActive = 1", new { Id = id }, ct);
        if (dto is null) return null;

        dto.Pauses = await conn.QueryListAsync<SubscriptionPauseDto>(
            "SELECT SubscriptionPauseId AS Id, CustomerSubscriptionId, PauseFrom, PauseTo, Reason, ResumedDate, ResumedBy, Status" +
            " FROM `SubscriptionPause` WHERE CustomerSubscriptionId = @Id AND IsActive = 1 ORDER BY PauseFrom DESC, SubscriptionPauseId DESC",
            new { Id = id }, ct);
        return dto;
    }

    public async Task<CreatedResult> CreateAsync(CreateSubscriptionRequest r, long userId, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        return await conn.QueryCreatedSpAsync("sp_CreateCustomerSubscription", new
        {
            p_CustomerId = r.CustomerId,
            p_SubscriptionPlanId = r.SubscriptionPlanId,
            p_CustomerAddressId = r.CustomerAddressId,
            p_ProductId = r.ProductId,
            p_Quantity = r.Quantity,
            p_StartDate = r.StartDate?.Date,
            p_EndDate = r.EndDate?.Date,
            p_DeliverySlot = r.DeliverySlot,
            p_UserId = userId
        }, ct);
    }

    public async Task<bool> AddressBelongsToCustomerAsync(long addressId, long customerId, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        return await conn.ScalarAsync<int>(
            "SELECT COUNT(1) FROM `CustomerAddress` WHERE CustomerAddressId = @AddressId AND CustomerId = @CustomerId AND IsActive = 1",
            new { AddressId = addressId, CustomerId = customerId }, ct) > 0;
    }

    public async Task UpdateAsync(long id, UpdateSubscriptionRequest r, long userId, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        await conn.ExecAsync(
            "UPDATE `CustomerSubscription` SET Quantity = COALESCE(@Quantity, Quantity), EndDate = @EndDate, DeliverySlot = COALESCE(@DeliverySlot, DeliverySlot)," +
            " CustomerAddressId = COALESCE(@CustomerAddressId, CustomerAddressId), Remarks = @Remarks, ModifiedBy = @UserId" +
            " WHERE CustomerSubscriptionId = @Id",
            new
            {
                Id = id,
                r.Quantity,
                EndDate = r.EndDate?.Date,
                DeliverySlot = SalesValues.Clean(r.DeliverySlot),
                r.CustomerAddressId,
                Remarks = SalesValues.Clean(r.Remarks),
                UserId = userId
            }, ct);
    }

    public async Task PauseAsync(long id, PauseSubscriptionRequest r, long userId, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        await conn.ExecuteSpAsync("sp_PauseSubscription", new
        {
            p_SubscriptionId = id,
            p_PauseFrom = r.PauseFrom?.Date,
            p_PauseTo = r.PauseTo?.Date,
            p_Reason = SalesValues.Clean(r.Reason),
            p_UserId = userId
        }, ct);
    }

    public async Task ResumeAsync(long id, long userId, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        await conn.ExecuteSpAsync("sp_ResumeSubscription", new { p_SubscriptionId = id, p_UserId = userId }, ct);
    }

    public async Task CancelAsync(long id, string? reason, long userId, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        await using var tx = await conn.BeginTransactionAsync(ct);

        await conn.ExecuteAsync(new CommandDefinition(
            "UPDATE `CustomerSubscription` SET Status = 'Cancelled', CancelledDate = NOW(), CancelReason = @Reason, NextOrderDate = NULL, ModifiedBy = @UserId" +
            " WHERE CustomerSubscriptionId = @Id AND Status IN ('Active', 'Paused')",
            new { Id = id, Reason = SalesValues.Clean(reason), UserId = userId }, tx, cancellationToken: ct));

        await conn.ExecuteAsync(new CommandDefinition(
            "UPDATE `SubscriptionPause` SET Status = 'Cancelled', ModifiedBy = @UserId WHERE CustomerSubscriptionId = @Id AND Status = 'Active'",
            new { Id = id, UserId = userId }, tx, cancellationToken: ct));

        await tx.CommitAsync(ct);
    }

    public async Task<SubscriptionOrdersGeneratedDto> GenerateOrdersAsync(DateTime forDate, long userId, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        var result = await conn.QuerySpFirstOrDefaultAsync<SubscriptionOrdersGeneratedDto>(
            "sp_CreateSubscriptionOrders", new { p_ForDate = forDate.Date, p_UserId = userId }, ct);
        return result ?? new SubscriptionOrdersGeneratedDto { OrdersCreated = 0, ForDate = forDate.Date };
    }
}
