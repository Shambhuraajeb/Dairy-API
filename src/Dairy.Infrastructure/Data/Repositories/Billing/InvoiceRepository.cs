using System.Text.Json;
using Dairy.Application.BusinessRules;
using Dairy.Application.Common;
using Dairy.Application.DTOs.Billing;
using Dairy.Application.Interfaces;
using Dairy.Application.Interfaces.Billing;
using Dairy.Application.Requests.Billing;
using Dapper;

namespace Dairy.Infrastructure.Data.Repositories.Billing;

public class InvoiceRepository : IInvoiceRepository
{
    private static readonly Dictionary<string, string> SortColumns = new(StringComparer.OrdinalIgnoreCase)
    {
        ["id"] = "i.InvoiceId",
        ["invoiceNumber"] = "i.InvoiceNumber",
        ["customerName"] = "c.Name",
        ["invoiceDate"] = "i.InvoiceDate",
        ["dueDate"] = "i.DueDate",
        ["totalAmount"] = "i.TotalAmount",
        ["balanceAmount"] = "i.BalanceAmount",
        ["status"] = "i.Status"
    };

    private const string FromJoin = " FROM `Invoice` i JOIN `Customer` c ON c.CustomerId = i.CustomerId";

    private const string Select =
        "SELECT i.InvoiceId AS Id, i.InvoiceNumber, i.CustomerId, c.Name AS CustomerName, c.CustomerCode, c.Mobile AS CustomerMobile, c.GstNumber AS CustomerGstNumber," +
        " i.InvoiceType, i.InvoiceDate, i.DueDate, i.PeriodFrom, i.PeriodTo, i.SubTotal, i.TaxAmount, i.DiscountAmount, i.TotalAmount, i.PaidAmount, i.BalanceAmount, i.Status," +
        " (SELECT COUNT(DISTINCT d.SalesOrderId) FROM `InvoiceDetail` d WHERE d.InvoiceId = i.InvoiceId AND d.IsActive = 1) AS OrderCount," +
        " i.IssuedDate, i.CancelledDate, i.CancelReason, i.Remarks, i.CreatedDate" + FromJoin;

    private const string Where =
        " WHERE i.IsActive = 1" +
        " AND (@Search IS NULL OR i.InvoiceNumber LIKE @Search OR c.Name LIKE @Search OR c.CustomerCode LIKE @Search)" +
        " AND (@CustomerId IS NULL OR i.CustomerId = @CustomerId)" +
        " AND (@Status IS NULL" +
        "      OR (@Status = 'Overdue' AND i.Status IN ('Issued', 'PartiallyPaid') AND i.BalanceAmount > 0 AND i.DueDate < CURDATE())" +
        "      OR (@Status <> 'Overdue' AND i.Status = @Status))" +
        " AND (@From IS NULL OR i.InvoiceDate >= @From)" +
        " AND (@To IS NULL OR i.InvoiceDate <= @To)";

    private readonly IDbConnectionFactory _db;

    public InvoiceRepository(IDbConnectionFactory db) => _db = db;

    public async Task<PagedResult<InvoiceListDto>> GetPagedAsync(InvoiceQuery query, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        var param = new
        {
            Search = query.SearchPattern,
            query.CustomerId,
            Status = SalesValues.Clean(query.Status),
            From = query.From?.Date,
            To = query.To?.Date
        };
        return await conn.QueryPagedAsync<InvoiceListDto>(Select + Where, "SELECT COUNT(*)" + FromJoin + Where, param, query, SortColumns,
            "i.InvoiceDate DESC, i.InvoiceId DESC", ct);
    }

    public async Task<InvoiceDetailDto?> GetByIdAsync(long id, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        var dto = await conn.QueryFirstOrNullAsync<InvoiceDetailDto>(Select + " WHERE i.InvoiceId = @Id AND i.IsActive = 1", new { Id = id }, ct);
        if (dto is null) return null;

        dto.Lines = await conn.QueryListAsync<InvoiceLineDto>(
            "SELECT d.InvoiceDetailId AS Id, d.SalesOrderId, o.OrderNumber, d.DeliveryId, d.ProductId, d.Description, d.Quantity, d.UnitPrice, d.DiscountAmount," +
            " d.TaxPercent, d.TaxAmount, d.LineTotal" +
            " FROM `InvoiceDetail` d LEFT JOIN `SalesOrder` o ON o.SalesOrderId = d.SalesOrderId" +
            " WHERE d.InvoiceId = @Id AND d.IsActive = 1 ORDER BY d.InvoiceDetailId",
            new { Id = id }, ct);

        dto.Payments = await conn.QueryListAsync<InvoiceAllocationDto>(
            "SELECT a.CustomerPaymentAllocationId AS Id, a.CustomerPaymentId, p.PaymentNumber, p.PaymentDate, pm.Name AS PaymentModeName, p.ReferenceNo, a.Amount, a.AllocationDate" +
            " FROM `CustomerPaymentAllocation` a" +
            " JOIN `CustomerPayment` p ON p.CustomerPaymentId = a.CustomerPaymentId" +
            " LEFT JOIN `PaymentMode` pm ON pm.PaymentModeId = p.PaymentModeId" +
            " WHERE a.InvoiceId = @Id AND a.IsActive = 1 ORDER BY a.AllocationDate, a.CustomerPaymentAllocationId",
            new { Id = id }, ct);
        return dto;
    }

    public async Task<CreatedResult> GenerateAsync(GenerateInvoiceRequest r, long userId, CancellationToken ct = default)
    {
        var ids = r.SalesOrderIds?.Where(i => i > 0).Distinct().ToList();
        string? idsJson = ids is { Count: > 0 } ? JsonSerializer.Serialize(ids) : null;

        await using var conn = await _db.OpenAsync(ct);
        return await conn.QueryCreatedSpAsync("sp_GenerateInvoice", new
        {
            p_CustomerId = r.CustomerId,
            p_PeriodFrom = r.PeriodFrom?.Date,
            p_PeriodTo = r.PeriodTo?.Date,
            p_SalesOrderIdsJson = idsJson,
            p_UserId = userId
        }, ct);
    }

    public async Task<int> CountAllocationsAsync(long id, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        return await conn.ScalarAsync<int>(
            "SELECT COUNT(1) FROM `CustomerPaymentAllocation` WHERE InvoiceId = @Id AND IsActive = 1", new { Id = id }, ct);
    }

    public async Task<bool> CancelAsync(long id, string? reason, long userId, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        await using var tx = await conn.BeginTransactionAsync(ct);
        var p = new { Id = id, Reason = SalesValues.Clean(reason), UserId = userId };

        var affected = await conn.ExecuteAsync(new CommandDefinition(
            "UPDATE `Invoice` SET Status = 'Cancelled', CancelledBy = @UserId, CancelledDate = NOW(), CancelReason = @Reason, ModifiedBy = @UserId" +
            " WHERE InvoiceId = @Id AND Status = 'Issued' AND PaidAmount = 0" +
            " AND NOT EXISTS (SELECT 1 FROM `CustomerPaymentAllocation` a WHERE a.InvoiceId = @Id AND a.IsActive = 1)",
            p, tx, cancellationToken: ct));
        if (affected == 0)
        {
            await tx.RollbackAsync(ct);
            return false;
        }

        // the orders/deliveries become billable again
        await conn.ExecuteAsync(new CommandDefinition(
            "UPDATE `SalesOrder` SET InvoiceId = NULL, PaymentStatus = 'Unpaid', ModifiedBy = @UserId WHERE InvoiceId = @Id", p, tx, cancellationToken: ct));
        await conn.ExecuteAsync(new CommandDefinition(
            "UPDATE `Delivery` SET InvoiceId = NULL, ModifiedBy = @UserId WHERE InvoiceId = @Id", p, tx, cancellationToken: ct));

        // reverse what sp_GenerateInvoice added to the customer's outstanding balance
        await conn.ExecuteAsync(new CommandDefinition(
            "UPDATE `Customer` c JOIN `Invoice` i ON i.CustomerId = c.CustomerId" +
            " SET c.OutstandingBalance = GREATEST(c.OutstandingBalance - i.TotalAmount, 0), c.ModifiedBy = @UserId WHERE i.InvoiceId = @Id",
            p, tx, cancellationToken: ct));

        await tx.CommitAsync(ct);
        return true;
    }
}
