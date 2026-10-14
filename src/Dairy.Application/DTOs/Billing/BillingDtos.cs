namespace Dairy.Application.DTOs.Billing;

public class InvoiceListDto
{
    public long Id { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public long CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerCode { get; set; } = string.Empty;
    public string? CustomerMobile { get; set; }
    public string? CustomerGstNumber { get; set; }

    /// <summary>Order | Consolidated.</summary>
    public string InvoiceType { get; set; } = string.Empty;
    public DateTime InvoiceDate { get; set; }
    public DateTime DueDate { get; set; }
    public DateTime? PeriodFrom { get; set; }
    public DateTime? PeriodTo { get; set; }
    public decimal SubTotal { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal BalanceAmount { get; set; }

    /// <summary>Draft | Issued | PartiallyPaid | Paid | Cancelled.</summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>Open (Issued/PartiallyPaid) invoice past its due date (derived).</summary>
    public bool IsOverdue =>
        (Status == "Issued" || Status == "PartiallyPaid") && BalanceAmount > 0 && DueDate.Date < DateTime.Today;
    public int OrderCount { get; set; }
    public DateTime? IssuedDate { get; set; }
    public DateTime? CancelledDate { get; set; }
    public string? CancelReason { get; set; }
    public string? Remarks { get; set; }
    public DateTime CreatedDate { get; set; }
}

public class InvoiceDetailDto : InvoiceListDto
{
    public List<InvoiceLineDto> Lines { get; set; } = new();
    public List<InvoiceAllocationDto> Payments { get; set; } = new();
}

public class InvoiceLineDto
{
    public long Id { get; set; }
    public long? SalesOrderId { get; set; }
    public string? OrderNumber { get; set; }
    public long? DeliveryId { get; set; }
    public long? ProductId { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TaxPercent { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal LineTotal { get; set; }
}

/// <summary>A payment allocation against an invoice.</summary>
public class InvoiceAllocationDto
{
    public long Id { get; set; }
    public long CustomerPaymentId { get; set; }
    public string PaymentNumber { get; set; } = string.Empty;
    public DateTime PaymentDate { get; set; }
    public string? PaymentModeName { get; set; }
    public string? ReferenceNo { get; set; }
    public decimal Amount { get; set; }
    public DateTime AllocationDate { get; set; }
}

public class CustomerPaymentListDto
{
    public long Id { get; set; }
    public string PaymentNumber { get; set; } = string.Empty;
    public long CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerCode { get; set; } = string.Empty;
    public long? InvoiceId { get; set; }
    public string? InvoiceNumber { get; set; }
    public DateTime PaymentDate { get; set; }
    public decimal Amount { get; set; }
    public decimal AllocatedAmount { get; set; }
    public decimal UnallocatedAmount { get; set; }
    public long PaymentModeId { get; set; }
    public string PaymentModeName { get; set; } = string.Empty;
    public string? ReferenceNo { get; set; }

    /// <summary>Received | Cancelled.</summary>
    public string Status { get; set; } = string.Empty;
    public long? ReceivedBy { get; set; }
    public string? ReceivedByName { get; set; }
    public string? Remarks { get; set; }
    public DateTime CreatedDate { get; set; }
}

public class CustomerPaymentDetailDto : CustomerPaymentListDto
{
    public List<PaymentAllocationDto> Allocations { get; set; } = new();
}

public class PaymentAllocationDto
{
    public long Id { get; set; }
    public long InvoiceId { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public DateTime InvoiceDate { get; set; }
    public decimal InvoiceTotal { get; set; }
    public decimal Amount { get; set; }
    public DateTime AllocationDate { get; set; }
}

/// <summary>Row of sp_GetCustomerOutstanding (customers with open invoices).</summary>
public class CustomerOutstandingDto
{
    public long Id { get; set; }
    public string CustomerCode { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerType { get; set; } = string.Empty;
    public string? Mobile { get; set; }
    public decimal CreditLimit { get; set; }
    public int OpenInvoices { get; set; }
    public DateTime? OldestDueDate { get; set; }
    public decimal CurrentAmount { get; set; }
    public decimal Days1To30 { get; set; }
    public decimal Days31To60 { get; set; }
    public decimal Days61To90 { get; set; }
    public decimal Over90Days { get; set; }
    public decimal TotalOutstanding { get; set; }
    public decimal UnallocatedCredit { get; set; }
}

public class BillingSummaryDto
{
    public decimal InvoicedThisMonth { get; set; }
    public decimal CollectedThisMonth { get; set; }
    public decimal CollectedToday { get; set; }
    public decimal TotalOutstanding { get; set; }
    public decimal OverdueAmount { get; set; }
    public int OpenInvoices { get; set; }
    public int OverdueInvoices { get; set; }
    public decimal UnallocatedCredit { get; set; }

    /// <summary>Delivered orders not yet invoiced.</summary>
    public int UninvoicedOrders { get; set; }
    public decimal UninvoicedAmount { get; set; }
    public decimal CurrentAmount { get; set; }
    public decimal Days1To30 { get; set; }
    public decimal Days31To60 { get; set; }
    public decimal Days61To90 { get; set; }
    public decimal Over90Days { get; set; }
}
