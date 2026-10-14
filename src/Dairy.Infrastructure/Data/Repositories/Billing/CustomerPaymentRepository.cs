using Dairy.Application.BusinessRules;
using Dairy.Application.Common;
using Dairy.Application.DTOs.Billing;
using Dairy.Application.Interfaces;
using Dairy.Application.Interfaces.Billing;
using Dairy.Application.Requests.Billing;

namespace Dairy.Infrastructure.Data.Repositories.Billing;

public class CustomerPaymentRepository : ICustomerPaymentRepository
{
    private static readonly Dictionary<string, string> SortColumns = new(StringComparer.OrdinalIgnoreCase)
    {
        ["id"] = "p.CustomerPaymentId",
        ["paymentNumber"] = "p.PaymentNumber",
        ["customerName"] = "c.Name",
        ["paymentDate"] = "p.PaymentDate",
        ["amount"] = "p.Amount",
        ["status"] = "p.Status",
        ["createdDate"] = "p.CreatedDate"
    };

    private const string FromJoins =
        " FROM `CustomerPayment` p" +
        " JOIN `Customer` c ON c.CustomerId = p.CustomerId" +
        " JOIN `PaymentMode` pm ON pm.PaymentModeId = p.PaymentModeId" +
        " LEFT JOIN `Invoice` i ON i.InvoiceId = p.InvoiceId" +
        " LEFT JOIN `User` ru ON ru.UserId = p.ReceivedBy";

    private const string Select =
        "SELECT p.CustomerPaymentId AS Id, p.PaymentNumber, p.CustomerId, c.Name AS CustomerName, c.CustomerCode, p.InvoiceId, i.InvoiceNumber, p.PaymentDate, p.Amount," +
        " p.AllocatedAmount, p.UnallocatedAmount, p.PaymentModeId, pm.Name AS PaymentModeName, p.ReferenceNo, p.Status, p.ReceivedBy, ru.DisplayName AS ReceivedByName," +
        " p.Remarks, p.CreatedDate" + FromJoins;

    private const string Where =
        " WHERE p.IsActive = 1" +
        " AND (@Search IS NULL OR p.PaymentNumber LIKE @Search OR c.Name LIKE @Search OR c.CustomerCode LIKE @Search OR p.ReferenceNo LIKE @Search)" +
        " AND (@CustomerId IS NULL OR p.CustomerId = @CustomerId)" +
        " AND (@InvoiceId IS NULL OR p.InvoiceId = @InvoiceId OR EXISTS (SELECT 1 FROM `CustomerPaymentAllocation` al WHERE al.CustomerPaymentId = p.CustomerPaymentId AND al.InvoiceId = @InvoiceId))" +
        " AND (@PaymentModeId IS NULL OR p.PaymentModeId = @PaymentModeId)" +
        " AND (@Status IS NULL OR p.Status = @Status)" +
        " AND (@From IS NULL OR p.PaymentDate >= @From)" +
        " AND (@To IS NULL OR p.PaymentDate <= @To)";

    private readonly IDbConnectionFactory _db;

    public CustomerPaymentRepository(IDbConnectionFactory db) => _db = db;

    public async Task<PagedResult<CustomerPaymentListDto>> GetPagedAsync(CustomerPaymentQuery query, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        var param = new
        {
            Search = query.SearchPattern,
            query.CustomerId,
            query.InvoiceId,
            query.PaymentModeId,
            Status = SalesValues.Clean(query.Status),
            From = query.From?.Date,
            To = query.To?.Date
        };
        return await conn.QueryPagedAsync<CustomerPaymentListDto>(Select + Where,
            "SELECT COUNT(*) FROM `CustomerPayment` p JOIN `Customer` c ON c.CustomerId = p.CustomerId" + Where,
            param, query, SortColumns, "p.PaymentDate DESC, p.CustomerPaymentId DESC", ct);
    }

    public async Task<CustomerPaymentDetailDto?> GetByIdAsync(long id, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        var dto = await conn.QueryFirstOrNullAsync<CustomerPaymentDetailDto>(Select + " WHERE p.CustomerPaymentId = @Id AND p.IsActive = 1", new { Id = id }, ct);
        if (dto is null) return null;

        dto.Allocations = await conn.QueryListAsync<PaymentAllocationDto>(
            "SELECT a.CustomerPaymentAllocationId AS Id, a.InvoiceId, i.InvoiceNumber, i.InvoiceDate, i.TotalAmount AS InvoiceTotal, a.Amount, a.AllocationDate" +
            " FROM `CustomerPaymentAllocation` a JOIN `Invoice` i ON i.InvoiceId = a.InvoiceId" +
            " WHERE a.CustomerPaymentId = @Id AND a.IsActive = 1 ORDER BY a.CustomerPaymentAllocationId",
            new { Id = id }, ct);
        return dto;
    }

    public async Task<CreatedResult> RecordAsync(RecordCustomerPaymentRequest r, long userId, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        return await conn.QueryCreatedSpAsync("sp_RecordCustomerPayment", new
        {
            p_CustomerId = r.CustomerId,
            p_PaymentDate = r.PaymentDate?.Date,
            p_Amount = r.Amount,
            p_PaymentModeId = r.PaymentModeId,
            p_ReferenceNo = SalesValues.Clean(r.ReferenceNo),
            p_InvoiceId = r.InvoiceId,
            p_Remarks = SalesValues.Clean(r.Remarks),
            p_UserId = userId
        }, ct);
    }
}
