using Dairy.Application.BusinessRules;
using Dairy.Application.Common;
using Dairy.Application.DTOs.Delivery;
using Dairy.Application.DTOs.Orders;
using Dairy.Application.Interfaces;
using Dairy.Application.Interfaces.Delivery;
using Dairy.Application.Requests.Delivery;
using MySqlConnector;

namespace Dairy.Infrastructure.Data.Repositories.Delivery;

public class DeliveryRepository : IDeliveryRepository
{
    private static readonly Dictionary<string, string> SortColumns = new(StringComparer.OrdinalIgnoreCase)
    {
        ["id"] = "dl.DeliveryId",
        ["deliveryNumber"] = "dl.DeliveryNumber",
        ["orderNumber"] = "o.OrderNumber",
        ["customerName"] = "c.Name",
        ["deliveryDate"] = "o.DeliveryDate",
        ["deliverySlot"] = "o.DeliverySlot",
        ["status"] = "dl.Status",
        ["sequenceNo"] = "dl.SequenceNo",
        ["routeName"] = "r.Name"
    };

    private const string FromJoins =
        " FROM `Delivery` dl" +
        " JOIN `SalesOrder` o ON o.SalesOrderId = dl.SalesOrderId" +
        " JOIN `Customer` c ON c.CustomerId = o.CustomerId" +
        " LEFT JOIN `CustomerAddress` a ON a.CustomerAddressId = o.CustomerAddressId" +
        " LEFT JOIN `Route` r ON r.RouteId = dl.RouteId" +
        " LEFT JOIN `Employee` e ON e.EmployeeId = dl.DriverEmployeeId" +
        " LEFT JOIN `User` au ON au.UserId = dl.AssignedUserId";

    private const string Select =
        "SELECT dl.DeliveryId AS Id, dl.DeliveryNumber, dl.SalesOrderId, o.OrderNumber, o.CustomerId, c.Name AS CustomerName, c.Mobile AS CustomerMobile," +
        " CONCAT_WS(', ', a.Line1, a.Landmark, a.City) AS AddressText, a.DeliveryInstructions, o.DeliveryDate, o.DeliverySlot, dl.DeliveryScheduleId, dl.RouteId," +
        " r.Name AS RouteName, dl.DriverEmployeeId, e.FullName AS DriverName, dl.AssignedUserId, au.DisplayName AS AssignedUserName, dl.SequenceNo, dl.Status," +
        " dl.DispatchedDate, dl.DeliveredDate, dl.FailureReason, o.TotalAmount AS OrderTotal," +
        " (SELECT COALESCE(SUM(d.Quantity), 0) FROM `SalesOrderDetail` d WHERE d.SalesOrderId = o.SalesOrderId AND d.IsActive = 1) AS TotalQuantity," +
        " dl.InvoiceId, dl.Remarks" + FromJoins;

    private const string ScopeClause =
        " AND (@ScopeUserId IS NULL OR dl.AssignedUserId = @ScopeUserId OR (@ScopeEmployeeId IS NOT NULL AND dl.DriverEmployeeId = @ScopeEmployeeId))";

    private const string Where =
        " WHERE dl.IsActive = 1" +
        " AND (@Search IS NULL OR dl.DeliveryNumber LIKE @Search OR o.OrderNumber LIKE @Search OR c.Name LIKE @Search)" +
        " AND (@Date IS NULL OR o.DeliveryDate = @Date)" +
        " AND (@Status IS NULL OR dl.Status = @Status)" +
        " AND (@RouteId IS NULL OR dl.RouteId = @RouteId)" +
        " AND (@DriverEmployeeId IS NULL OR dl.DriverEmployeeId = @DriverEmployeeId)" +
        " AND (@ScheduleId IS NULL OR dl.DeliveryScheduleId = @ScheduleId)" +
        " AND (@Slot IS NULL OR o.DeliverySlot = @Slot)" +
        " AND (@CustomerId IS NULL OR o.CustomerId = @CustomerId)" + ScopeClause;

    private readonly IDbConnectionFactory _db;

    public DeliveryRepository(IDbConnectionFactory db) => _db = db;

    public async Task<PagedResult<DeliveryListDto>> GetPagedAsync(DeliveryQuery query, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        var param = new
        {
            Search = query.SearchPattern,
            Date = query.Date?.Date,
            Status = SalesValues.Clean(query.Status),
            query.RouteId,
            query.DriverEmployeeId,
            query.ScheduleId,
            Slot = SalesValues.Clean(query.Slot),
            query.CustomerId,
            query.ScopeUserId,
            query.ScopeEmployeeId
        };
        return await conn.QueryPagedAsync<DeliveryListDto>(Select + Where, "SELECT COUNT(*)" + FromJoins + Where, param, query, SortColumns,
            "o.DeliveryDate DESC, dl.DeliveryScheduleId ASC, dl.SequenceNo ASC, dl.DeliveryId DESC", ct);
    }

    public async Task<List<DeliveryListDto>> GetMyAsync(long userId, long? employeeId, DateTime date, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        return await conn.QueryListAsync<DeliveryListDto>(
            Select + " WHERE dl.IsActive = 1 AND dl.Status <> 'Cancelled' AND o.DeliveryDate = @Date" +
            " AND (dl.AssignedUserId = @UserId OR (@EmployeeId IS NOT NULL AND dl.DriverEmployeeId = @EmployeeId))" +
            " ORDER BY o.DeliverySlot ASC, dl.DeliveryScheduleId ASC, dl.SequenceNo ASC, dl.DeliveryId ASC",
            new { Date = date.Date, UserId = userId, EmployeeId = employeeId }, ct);
    }

    public async Task<DeliveryDetailDto?> GetByIdAsync(long id, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        var dto = await conn.QueryFirstOrNullAsync<DeliveryDetailDto>(Select + " WHERE dl.DeliveryId = @Id AND dl.IsActive = 1", new { Id = id }, ct);
        if (dto is null) return null;

        dto.Lines = await conn.QueryListAsync<OrderLineDto>(
            "SELECT d.SalesOrderDetailId AS Id, d.ProductId, p.ProductCode, p.Name AS ProductName, u.Code AS UnitCode, d.Quantity, d.DeliveredQuantity, d.UnitPrice," +
            " d.DiscountAmount, d.TaxPercent, d.TaxAmount, d.LineTotal, d.Remarks" +
            " FROM `SalesOrderDetail` d JOIN `Product` p ON p.ProductId = d.ProductId LEFT JOIN `Unit` u ON u.UnitId = p.UnitId" +
            " WHERE d.SalesOrderId = @OrderId AND d.IsActive = 1 ORDER BY d.SalesOrderDetailId",
            new { OrderId = dto.SalesOrderId }, ct);

        dto.Confirmation = await conn.QueryFirstOrNullAsync<DeliveryConfirmationDto>(
            "SELECT dc.DeliveryConfirmationId AS Id, dc.DeliveryId, dc.ConfirmedBy, u.DisplayName AS ConfirmedByName, dc.ConfirmedDate, dc.Outcome," +
            " dc.DeliveredQuantity, dc.ReceivedBy, dc.CollectedAmount, dc.Latitude, dc.Longitude, dc.FailureReason, dc.Remarks" +
            " FROM `DeliveryConfirmation` dc LEFT JOIN `User` u ON u.UserId = dc.ConfirmedBy" +
            " WHERE dc.DeliveryId = @Id AND dc.IsActive = 1 ORDER BY dc.DeliveryConfirmationId DESC LIMIT 1",
            new { Id = id }, ct);
        return dto;
    }

    public async Task<CreatedResult> CreateAsync(CreateDeliveryRequest r, long userId, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        return await QueryCreateAsync(conn, r.SalesOrderId, r.DeliveryScheduleId, r.DriverEmployeeId, r.AssignedUserId, userId, ct);
    }

    public async Task<BulkDeliveryResultDto> CreateBulkAsync(BulkDeliveryRequest r, long userId, CancellationToken ct = default)
    {
        var ids = r.SalesOrderIds.Where(i => i > 0).Distinct().ToList();
        var result = new BulkDeliveryResultDto { RequestedCount = ids.Count };

        await using var conn = await _db.OpenAsync(ct);
        foreach (var orderId in ids)
        {
            try
            {
                var created = await QueryCreateAsync(conn, orderId, r.DeliveryScheduleId, r.DriverEmployeeId, r.AssignedUserId, userId, ct);
                result.Created.Add(new BulkDeliveryCreatedDto { SalesOrderId = orderId, DeliveryId = created.Id, DeliveryNumber = created.Number });
            }
            catch (MySqlException ex) when (ex.SqlState == "45000")
            {
                // business-rule failure for this order only (the procedure rolled back its own transaction)
                result.Failed.Add(new BulkDeliveryFailedDto { SalesOrderId = orderId, Message = ex.Message });
            }
        }

        result.CreatedCount = result.Created.Count;
        result.FailedCount = result.Failed.Count;
        return result;
    }

    public async Task DispatchAsync(long id, long userId, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        await conn.ExecuteSpAsync("sp_DispatchDelivery", new { p_DeliveryId = id, p_UserId = userId }, ct);
    }

    public async Task ConfirmAsync(long id, ConfirmDeliveryRequest r, bool isPrivileged, long userId, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        await conn.ExecuteSpAsync("sp_ConfirmDelivery", new
        {
            p_DeliveryId = id,
            p_UserId = userId,
            p_IsDelivered = r.IsDelivered ? 1 : 0,
            p_ReceivedBy = SalesValues.Clean(r.ReceivedBy),
            p_Remarks = SalesValues.Clean(r.Remarks),
            p_FailureReason = SalesValues.Clean(r.FailureReason),
            p_Latitude = r.Latitude,
            p_Longitude = r.Longitude,
            p_IsPrivileged = isPrivileged ? 1 : 0
        }, ct);
    }

    private static Task<CreatedResult> QueryCreateAsync(MySqlConnection conn, long orderId, long? scheduleId, long? driverEmployeeId, long? assignedUserId,
        long userId, CancellationToken ct) =>
        conn.QueryCreatedSpAsync("sp_CreateDelivery", new
        {
            p_SalesOrderId = orderId,
            p_DeliveryScheduleId = scheduleId,
            p_DriverEmployeeId = driverEmployeeId,
            p_AssignedUserId = assignedUserId,
            p_UserId = userId
        }, ct);
}
