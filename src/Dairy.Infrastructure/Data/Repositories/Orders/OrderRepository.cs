using System.Text.Json;
using Dairy.Application.BusinessRules;
using Dairy.Application.Common;
using Dairy.Application.DTOs.Orders;
using Dairy.Application.Interfaces;
using Dairy.Application.Interfaces.Orders;
using Dairy.Application.Requests.Orders;
using Dapper;

namespace Dairy.Infrastructure.Data.Repositories.Orders;

public class OrderRepository : IOrderRepository
{
    private static readonly Dictionary<string, string> SortColumns = new(StringComparer.OrdinalIgnoreCase)
    {
        ["id"] = "o.SalesOrderId",
        ["orderNumber"] = "o.OrderNumber",
        ["customerName"] = "c.Name",
        ["orderDate"] = "o.OrderDate",
        ["deliveryDate"] = "o.DeliveryDate",
        ["deliverySlot"] = "o.DeliverySlot",
        ["totalAmount"] = "o.TotalAmount",
        ["status"] = "o.Status",
        ["createdDate"] = "o.CreatedDate"
    };

    private const string ListColumns =
        "SELECT o.SalesOrderId AS Id, o.OrderNumber, o.CustomerId, c.Name AS CustomerName, c.CustomerCode, o.CustomerAddressId," +
        " CONCAT_WS(', ', a.Line1, a.Landmark, a.City) AS AddressText, o.CustomerSubscriptionId, o.RouteId, r.Name AS RouteName," +
        " o.OrderDate, o.DeliveryDate, o.DeliverySlot, o.Source, o.SubTotal, o.TaxAmount, o.DiscountAmount, o.DeliveryCharge, o.TotalAmount, o.PaidAmount," +
        " o.Status, o.PaymentStatus, o.InvoiceId, i.InvoiceNumber," +
        " (SELECT COALESCE(SUM(d.Quantity), 0) FROM `SalesOrderDetail` d WHERE d.SalesOrderId = o.SalesOrderId AND d.IsActive = 1) AS TotalQuantity," +
        " o.Remarks, o.CreatedDate";

    private const string DetailColumns = ", o.ConfirmedDate, o.CancelledDate, o.CancelReason";

    private const string FromJoins =
        " FROM `SalesOrder` o" +
        " JOIN `Customer` c ON c.CustomerId = o.CustomerId" +
        " LEFT JOIN `CustomerAddress` a ON a.CustomerAddressId = o.CustomerAddressId" +
        " LEFT JOIN `Route` r ON r.RouteId = o.RouteId" +
        " LEFT JOIN `Invoice` i ON i.InvoiceId = o.InvoiceId";

    private const string CountFrom = " FROM `SalesOrder` o JOIN `Customer` c ON c.CustomerId = o.CustomerId";

    private const string Where =
        " WHERE o.IsActive = 1" +
        " AND (@Search IS NULL OR o.OrderNumber LIKE @Search OR c.Name LIKE @Search OR c.CustomerCode LIKE @Search)" +
        " AND (@CustomerId IS NULL OR o.CustomerId = @CustomerId)" +
        " AND (@Status IS NULL OR o.Status = @Status)" +
        " AND (@From IS NULL OR o.DeliveryDate >= @From)" +
        " AND (@To IS NULL OR o.DeliveryDate <= @To)" +
        " AND (@Slot IS NULL OR o.DeliverySlot = @Slot)" +
        " AND (@RouteId IS NULL OR o.RouteId = @RouteId)" +
        " AND (@Source IS NULL OR o.Source = @Source)";

    private const string AwaitingClause =
        " AND o.Status IN ('Pending', 'Confirmed', 'Failed')" +
        " AND NOT EXISTS (SELECT 1 FROM `Delivery` dl WHERE dl.SalesOrderId = o.SalesOrderId AND dl.IsActive = 1 AND dl.Status IN ('Pending', 'Dispatched', 'Delivered'))";

    private readonly IDbConnectionFactory _db;

    public OrderRepository(IDbConnectionFactory db) => _db = db;

    public Task<PagedResult<OrderListDto>> GetPagedAsync(OrderQuery query, CancellationToken ct = default) =>
        PageAsync(query, string.Empty, "o.DeliveryDate DESC, o.SalesOrderId DESC", ct);

    public Task<PagedResult<OrderListDto>> GetAwaitingSchedulingAsync(OrderQuery query, CancellationToken ct = default)
    {
        // an explicit status filter makes no sense here: the awaiting set is defined by AwaitingClause
        query.Status = null;
        return PageAsync(query, AwaitingClause, "o.DeliveryDate ASC, o.DeliverySlot ASC, o.SalesOrderId ASC", ct);
    }

    private async Task<PagedResult<OrderListDto>> PageAsync(OrderQuery query, string extraWhere, string defaultSort, CancellationToken ct)
    {
        await using var conn = await _db.OpenAsync(ct);
        var param = new
        {
            Search = query.SearchPattern,
            query.CustomerId,
            Status = SalesValues.Clean(query.Status),
            From = query.From?.Date,
            To = query.To?.Date,
            Slot = SalesValues.Clean(query.Slot),
            query.RouteId,
            Source = SalesValues.Clean(query.Source)
        };
        return await conn.QueryPagedAsync<OrderListDto>(
            ListColumns + FromJoins + Where + extraWhere,
            "SELECT COUNT(*)" + CountFrom + Where + extraWhere,
            param, query, SortColumns, defaultSort, ct);
    }

    public async Task<OrderDetailDto?> GetByIdAsync(long id, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        var dto = await conn.QueryFirstOrNullAsync<OrderDetailDto>(
            ListColumns + DetailColumns + FromJoins + " WHERE o.SalesOrderId = @Id AND o.IsActive = 1", new { Id = id }, ct);
        if (dto is null) return null;

        dto.Lines = await conn.QueryListAsync<OrderLineDto>(
            "SELECT d.SalesOrderDetailId AS Id, d.ProductId, p.ProductCode, p.Name AS ProductName, u.Code AS UnitCode, d.Quantity, d.DeliveredQuantity, d.UnitPrice," +
            " d.DiscountAmount, d.TaxPercent, d.TaxAmount, d.LineTotal, d.Remarks" +
            " FROM `SalesOrderDetail` d JOIN `Product` p ON p.ProductId = d.ProductId LEFT JOIN `Unit` u ON u.UnitId = p.UnitId" +
            " WHERE d.SalesOrderId = @Id AND d.IsActive = 1 ORDER BY d.SalesOrderDetailId",
            new { Id = id }, ct);

        dto.Delivery = await conn.QueryFirstOrNullAsync<OrderDeliveryDto>(
            "SELECT dl.DeliveryId AS Id, dl.DeliveryNumber, dl.Status, dl.DeliveryScheduleId, dl.RouteId, r.Name AS RouteName, dl.DriverEmployeeId," +
            " e.FullName AS DriverName, dl.AssignedUserId, dl.DispatchedDate, dl.DeliveredDate, dl.FailureReason" +
            " FROM `Delivery` dl LEFT JOIN `Route` r ON r.RouteId = dl.RouteId LEFT JOIN `Employee` e ON e.EmployeeId = dl.DriverEmployeeId" +
            " WHERE dl.SalesOrderId = @Id AND dl.IsActive = 1 AND dl.Status <> 'Cancelled' ORDER BY dl.DeliveryId DESC LIMIT 1",
            new { Id = id }, ct);
        return dto;
    }

    public async Task<CreatedResult> CreateAsync(long customerId, CreateOrderRequest r, bool allowPriceOverride, long userId, CancellationToken ct = default)
    {
        var items = r.Items.Select(i =>
        {
            var row = new Dictionary<string, object?> { ["productId"] = i.ProductId, ["quantity"] = i.Quantity };
            if (allowPriceOverride && i.UnitPrice.HasValue) row["unitPrice"] = i.UnitPrice.Value;
            return row;
        }).ToList();
        var itemsJson = JsonSerializer.Serialize(items);

        await using var conn = await _db.OpenAsync(ct);
        return await conn.QueryCreatedSpAsync("sp_CreateSalesOrder", new
        {
            p_CustomerId = customerId,
            p_CustomerAddressId = r.CustomerAddressId,
            p_DeliveryDate = r.DeliveryDate.Date,
            p_DeliverySlot = r.DeliverySlot,
            p_Remarks = SalesValues.Clean(r.Remarks),
            p_UserId = userId,
            p_ItemsJson = itemsJson
        }, ct);
    }

    public async Task<bool> AddressBelongsToCustomerAsync(long addressId, long customerId, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        return await conn.ScalarAsync<int>(
            "SELECT COUNT(1) FROM `CustomerAddress` WHERE CustomerAddressId = @AddressId AND CustomerId = @CustomerId AND IsActive = 1",
            new { AddressId = addressId, CustomerId = customerId }, ct) > 0;
    }

    public async Task UpdateAsync(long id, UpdateOrderRequest r, long userId, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        await conn.ExecAsync(
            "UPDATE `SalesOrder` SET CustomerAddressId = COALESCE(@AddressId, CustomerAddressId)," +
            " RouteId = IF(@AddressId IS NULL, RouteId, (SELECT a.RouteId FROM `CustomerAddress` a WHERE a.CustomerAddressId = @AddressId))," +
            " DeliveryDate = COALESCE(@DeliveryDate, DeliveryDate), DeliverySlot = COALESCE(@Slot, DeliverySlot), Remarks = @Remarks, ModifiedBy = @UserId" +
            " WHERE SalesOrderId = @Id",
            new
            {
                Id = id,
                AddressId = r.CustomerAddressId,
                DeliveryDate = r.DeliveryDate?.Date,
                Slot = SalesValues.Clean(r.DeliverySlot),
                Remarks = SalesValues.Clean(r.Remarks),
                UserId = userId
            }, ct);
    }

    public async Task<bool> CancelAsync(long id, string? reason, long userId, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        await using var tx = await conn.BeginTransactionAsync(ct);

        var affected = await conn.ExecuteAsync(new CommandDefinition(
            "UPDATE `SalesOrder` SET Status = 'Cancelled', CancelledBy = @UserId, CancelledDate = NOW(), CancelReason = @Reason, ModifiedBy = @UserId" +
            " WHERE SalesOrderId = @Id AND Status IN ('Pending', 'Confirmed', 'Scheduled', 'Failed') AND InvoiceId IS NULL",
            new { Id = id, Reason = SalesValues.Clean(reason), UserId = userId }, tx, cancellationToken: ct));

        if (affected == 0)
        {
            await tx.RollbackAsync(ct);
            return false;
        }

        await conn.ExecuteAsync(new CommandDefinition(
            "UPDATE `Delivery` SET Status = 'Cancelled', ModifiedBy = @UserId WHERE SalesOrderId = @Id AND IsActive = 1 AND Status = 'Pending'",
            new { Id = id, UserId = userId }, tx, cancellationToken: ct));

        await tx.CommitAsync(ct);
        return true;
    }
}
