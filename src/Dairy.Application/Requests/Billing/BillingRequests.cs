using Dairy.Application.Common;

namespace Dairy.Application.Requests.Billing;

public class InvoiceQuery : PagedQuery
{
    /// <summary>Forced to the signed-in customer for Customer/Distributor roles.</summary>
    public long? CustomerId { get; set; }

    /// <summary>Issued | PartiallyPaid | Paid | Cancelled | Draft, or Overdue (open and past due).</summary>
    public string? Status { get; set; }

    /// <summary>Invoice date range (inclusive).</summary>
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }
}

public class GenerateInvoiceRequest
{
    public long CustomerId { get; set; }

    /// <summary>Delivery-date period of the delivered, un-invoiced orders to bill (ignored when salesOrderIds is given).</summary>
    public DateTime? PeriodFrom { get; set; }
    public DateTime? PeriodTo { get; set; }

    /// <summary>Optional explicit list of delivered orders to invoice.</summary>
    public List<long>? SalesOrderIds { get; set; }
}

public class CancelInvoiceRequest
{
    public string? Reason { get; set; }
}

public class CustomerPaymentQuery : PagedQuery
{
    /// <summary>Forced to the signed-in customer for Customer/Distributor roles.</summary>
    public long? CustomerId { get; set; }
    public long? InvoiceId { get; set; }
    public long? PaymentModeId { get; set; }

    /// <summary>Received | Cancelled.</summary>
    public string? Status { get; set; }

    /// <summary>Payment date range (inclusive).</summary>
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }
}

public class RecordCustomerPaymentRequest
{
    public long CustomerId { get; set; }

    /// <summary>Null = today; cannot be in the future.</summary>
    public DateTime? PaymentDate { get; set; }
    public decimal Amount { get; set; }
    public long PaymentModeId { get; set; }

    /// <summary>Mandatory for modes that require a reference (cheque / UPI / transfer).</summary>
    public string? ReferenceNo { get; set; }

    /// <summary>Optional: apply only to this invoice; null = oldest open invoices first.</summary>
    public long? InvoiceId { get; set; }
    public string? Remarks { get; set; }
}
