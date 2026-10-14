namespace Dairy.Application.DTOs.Customers;

public class CustomerDto
{
    public long Id { get; set; }
    public string CustomerCode { get; set; } = string.Empty;
    public string CustomerType { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? ContactPerson { get; set; }
    public string Mobile { get; set; } = string.Empty;
    public string? AlternateMobile { get; set; }
    public string? Email { get; set; }
    public string? GstNumber { get; set; }
    public long? BranchId { get; set; }
    public string? BranchName { get; set; }
    public decimal CreditLimit { get; set; }
    public int CreditDays { get; set; }
    public decimal DiscountPercent { get; set; }
    public decimal OutstandingBalance { get; set; }
    public DateTime JoinDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedDate { get; set; }
    public int ActiveSubscriptions { get; set; }
}

public class CustomerAddressDto
{
    public long Id { get; set; }
    public long CustomerId { get; set; }
    public string AddressType { get; set; } = string.Empty;
    public string? ContactName { get; set; }
    public string? ContactMobile { get; set; }
    public string Line1 { get; set; } = string.Empty;
    public string? Line2 { get; set; }
    public string? Landmark { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? Pincode { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public long? RouteId { get; set; }
    public string? RouteName { get; set; }
    public string? DeliveryInstructions { get; set; }
    public bool IsDefault { get; set; }
    public bool IsActive { get; set; }
}

public class CustomerSummaryDto
{
    public long Id { get; set; }
    public CustomerDto? Customer { get; set; }
    public int ActiveSubscriptions { get; set; }
    public int PausedSubscriptions { get; set; }
    public int TotalOrders { get; set; }
    public int PendingOrders { get; set; }
    public int DeliveredOrders { get; set; }
    public DateTime? LastOrderDate { get; set; }
    public decimal TotalInvoiced { get; set; }
    public int OpenInvoices { get; set; }
    public decimal OutstandingAmount { get; set; }
    public decimal OverdueAmount { get; set; }
    public decimal UnallocatedCredit { get; set; }
    public DateTime? LastPaymentDate { get; set; }
    public decimal? LastPaymentAmount { get; set; }
    public int OpenComplaints { get; set; }
}

public class ComplaintDto
{
    public long Id { get; set; }
    public string ComplaintNumber { get; set; } = string.Empty;
    public long CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerCode { get; set; } = string.Empty;
    public long? SalesOrderId { get; set; }
    public string? OrderNumber { get; set; }
    public long? DeliveryId { get; set; }
    public string? DeliveryNumber { get; set; }
    public string Category { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Priority { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public long? AssignedTo { get; set; }
    public string? AssignedToName { get; set; }
    public DateTime? AssignedDate { get; set; }
    public string? Resolution { get; set; }
    public long? ResolvedBy { get; set; }
    public DateTime? ResolvedDate { get; set; }
    public long? ClosedBy { get; set; }
    public DateTime? ClosedDate { get; set; }
    public int? FeedbackRating { get; set; }
    public string? FeedbackComments { get; set; }
    public DateTime? FeedbackDate { get; set; }
    public DateTime CreatedDate { get; set; }
}
