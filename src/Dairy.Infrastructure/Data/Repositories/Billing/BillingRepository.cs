using Dairy.Application.DTOs.Billing;
using Dairy.Application.Interfaces;
using Dairy.Application.Interfaces.Billing;

namespace Dairy.Infrastructure.Data.Repositories.Billing;

public class BillingRepository : IBillingRepository
{
    private const string OpenInvoice = "i.IsActive = 1 AND i.Status IN ('Issued', 'PartiallyPaid') AND i.BalanceAmount > 0 AND (@CustomerId IS NULL OR i.CustomerId = @CustomerId)";

    private readonly IDbConnectionFactory _db;

    public BillingRepository(IDbConnectionFactory db) => _db = db;

    public async Task<List<CustomerOutstandingDto>> GetOutstandingAsync(long? customerId, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        return await conn.QuerySpAsync<CustomerOutstandingDto>("sp_GetCustomerOutstanding", new { p_CustomerId = customerId }, ct);
    }

    public async Task<BillingSummaryDto> GetSummaryAsync(long? customerId, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        var summary = await conn.QueryFirstOrNullAsync<BillingSummaryDto>(
            "SELECT" +
            " (SELECT COALESCE(SUM(i.TotalAmount), 0) FROM `Invoice` i WHERE i.IsActive = 1 AND i.Status <> 'Cancelled'" +
            "   AND i.InvoiceDate >= DATE_FORMAT(CURDATE(), '%Y-%m-01') AND (@CustomerId IS NULL OR i.CustomerId = @CustomerId)) AS InvoicedThisMonth," +
            " (SELECT COALESCE(SUM(p.Amount), 0) FROM `CustomerPayment` p WHERE p.IsActive = 1 AND p.Status = 'Received'" +
            "   AND p.PaymentDate >= DATE_FORMAT(CURDATE(), '%Y-%m-01') AND (@CustomerId IS NULL OR p.CustomerId = @CustomerId)) AS CollectedThisMonth," +
            " (SELECT COALESCE(SUM(p.Amount), 0) FROM `CustomerPayment` p WHERE p.IsActive = 1 AND p.Status = 'Received'" +
            "   AND p.PaymentDate = CURDATE() AND (@CustomerId IS NULL OR p.CustomerId = @CustomerId)) AS CollectedToday," +
            " (SELECT COALESCE(SUM(i.BalanceAmount), 0) FROM `Invoice` i WHERE " + OpenInvoice + ") AS TotalOutstanding," +
            " (SELECT COALESCE(SUM(i.BalanceAmount), 0) FROM `Invoice` i WHERE " + OpenInvoice + " AND i.DueDate < CURDATE()) AS OverdueAmount," +
            " (SELECT COUNT(*) FROM `Invoice` i WHERE " + OpenInvoice + ") AS OpenInvoices," +
            " (SELECT COUNT(*) FROM `Invoice` i WHERE " + OpenInvoice + " AND i.DueDate < CURDATE()) AS OverdueInvoices," +
            " (SELECT COALESCE(SUM(p.UnallocatedAmount), 0) FROM `CustomerPayment` p WHERE p.IsActive = 1 AND p.Status = 'Received'" +
            "   AND (@CustomerId IS NULL OR p.CustomerId = @CustomerId)) AS UnallocatedCredit," +
            " (SELECT COUNT(*) FROM `SalesOrder` o WHERE o.IsActive = 1 AND o.Status = 'Delivered' AND o.InvoiceId IS NULL" +
            "   AND (@CustomerId IS NULL OR o.CustomerId = @CustomerId)) AS UninvoicedOrders," +
            " (SELECT COALESCE(SUM(o.TotalAmount), 0) FROM `SalesOrder` o WHERE o.IsActive = 1 AND o.Status = 'Delivered' AND o.InvoiceId IS NULL" +
            "   AND (@CustomerId IS NULL OR o.CustomerId = @CustomerId)) AS UninvoicedAmount," +
            " (SELECT COALESCE(SUM(i.BalanceAmount), 0) FROM `Invoice` i WHERE " + OpenInvoice + " AND DATEDIFF(CURDATE(), i.DueDate) <= 0) AS CurrentAmount," +
            " (SELECT COALESCE(SUM(i.BalanceAmount), 0) FROM `Invoice` i WHERE " + OpenInvoice + " AND DATEDIFF(CURDATE(), i.DueDate) BETWEEN 1 AND 30) AS Days1To30," +
            " (SELECT COALESCE(SUM(i.BalanceAmount), 0) FROM `Invoice` i WHERE " + OpenInvoice + " AND DATEDIFF(CURDATE(), i.DueDate) BETWEEN 31 AND 60) AS Days31To60," +
            " (SELECT COALESCE(SUM(i.BalanceAmount), 0) FROM `Invoice` i WHERE " + OpenInvoice + " AND DATEDIFF(CURDATE(), i.DueDate) BETWEEN 61 AND 90) AS Days61To90," +
            " (SELECT COALESCE(SUM(i.BalanceAmount), 0) FROM `Invoice` i WHERE " + OpenInvoice + " AND DATEDIFF(CURDATE(), i.DueDate) > 90) AS Over90Days",
            new { CustomerId = customerId }, ct);
        return summary ?? new BillingSummaryDto();
    }
}
