using Dairy.Application.Common;

namespace Dairy.Application.Requests.Customers;

public class CustomerQuery : PagedQuery
{
    /// <summary>Retail | Distributor (query key: type).</summary>
    public string? Type { get; set; }

    /// <summary>Active | Inactive | Blocked.</summary>
    public string? Status { get; set; }
    public bool? IsActive { get; set; }

    /// <summary>Forced to the signed-in customer for Customer/Distributor roles.</summary>
    public long? CustomerId { get; set; }
}

public class CreateCustomerRequest
{
    /// <summary>Optional; auto-generated (CUS0001 ...) when blank.</summary>
    public string? CustomerCode { get; set; }

    /// <summary>Retail | Distributor.</summary>
    public string CustomerType { get; set; } = "Retail";
    public string Name { get; set; } = string.Empty;
    public string? ContactPerson { get; set; }
    public string Mobile { get; set; } = string.Empty;
    public string? AlternateMobile { get; set; }
    public string? Email { get; set; }
    public string? GstNumber { get; set; }
    public long? BranchId { get; set; }
    public decimal CreditLimit { get; set; }
    public int CreditDays { get; set; }
    public decimal DiscountPercent { get; set; }
    public DateTime? JoinDate { get; set; }
    public string? Notes { get; set; }
}

public class UpdateCustomerRequest
{
    public string CustomerType { get; set; } = "Retail";
    public string Name { get; set; } = string.Empty;
    public string? ContactPerson { get; set; }
    public string Mobile { get; set; } = string.Empty;
    public string? AlternateMobile { get; set; }
    public string? Email { get; set; }
    public string? GstNumber { get; set; }
    public long? BranchId { get; set; }
    public decimal CreditLimit { get; set; }
    public int CreditDays { get; set; }
    public decimal DiscountPercent { get; set; }
    public DateTime? JoinDate { get; set; }
    public string? Notes { get; set; }
}

public class CustomerAddressRequest
{
    /// <summary>Home | Office | Shop | Billing | Delivery.</summary>
    public string AddressType { get; set; } = "Home";
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
    public string? DeliveryInstructions { get; set; }
    public bool IsDefault { get; set; }
}

public class ComplaintQuery : PagedQuery
{
    public long? CustomerId { get; set; }
    public string? Status { get; set; }
    public string? Category { get; set; }
    public string? Priority { get; set; }
    public long? AssignedTo { get; set; }
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }
}

public class CreateComplaintRequest
{
    /// <summary>Required for staff; ignored for Customer/Distributor roles (own customer is used).</summary>
    public long? CustomerId { get; set; }
    public long? SalesOrderId { get; set; }
    public long? DeliveryId { get; set; }

    /// <summary>Quality | Delivery | Billing | Service | Other.</summary>
    public string Category { get; set; } = "Other";
    public string Subject { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    /// <summary>Low | Medium | High | Urgent.</summary>
    public string Priority { get; set; } = "Medium";
}

public class AssignComplaintRequest
{
    /// <summary>User id of the staff member.</summary>
    public long AssignedTo { get; set; }
}

public class ComplaintStatusRequest
{
    /// <summary>Open | Assigned | InProgress (use /resolve and /close for the final states).</summary>
    public string Status { get; set; } = string.Empty;
    public long? AssignedTo { get; set; }
}

public class ResolveComplaintRequest
{
    public string Resolution { get; set; } = string.Empty;
}

public class ComplaintFeedbackRequest
{
    /// <summary>1..5.</summary>
    public int Rating { get; set; }
    public string? Comments { get; set; }
}
