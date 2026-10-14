using Dairy.Application.Common;

namespace Dairy.Application.Requests.Subscriptions;

public class SubscriptionPlanQuery : PagedQuery
{
    /// <summary>Daily | Alternate | Weekly.</summary>
    public string? Frequency { get; set; }
    public long? ProductId { get; set; }
    public bool? IsActive { get; set; }
}

public class CreateSubscriptionPlanRequest
{
    /// <summary>Optional; auto-generated (PLN0001 ...) when blank.</summary>
    public string? PlanCode { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }

    /// <summary>Daily | Alternate | Weekly.</summary>
    public string Frequency { get; set; } = "Daily";

    /// <summary>Weekly plans: comma list, e.g. "Mon,Thu".</summary>
    public string? DaysOfWeek { get; set; }
    public long? ProductId { get; set; }
    public decimal DefaultQuantity { get; set; }

    /// <summary>Null = use the product price list.</summary>
    public decimal? PricePerUnit { get; set; }
    public decimal DiscountPercent { get; set; }

    /// <summary>Daily | Weekly | Monthly.</summary>
    public string BillingCycle { get; set; } = "Monthly";
    public int MinimumDays { get; set; }
}

public class UpdateSubscriptionPlanRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }

    /// <summary>Daily | Alternate | Weekly.</summary>
    public string Frequency { get; set; } = "Daily";

    /// <summary>Weekly plans: comma list, e.g. "Mon,Thu".</summary>
    public string? DaysOfWeek { get; set; }
    public long? ProductId { get; set; }
    public decimal DefaultQuantity { get; set; }
    public decimal? PricePerUnit { get; set; }
    public decimal DiscountPercent { get; set; }

    /// <summary>Daily | Weekly | Monthly.</summary>
    public string BillingCycle { get; set; } = "Monthly";
    public int MinimumDays { get; set; }

    /// <summary>Null = unchanged; false = deactivate.</summary>
    public bool? IsActive { get; set; }
}

public class SubscriptionQuery : PagedQuery
{
    public long? CustomerId { get; set; }

    /// <summary>Active | Paused | Cancelled | Expired.</summary>
    public string? Status { get; set; }
    public long? ProductId { get; set; }

    /// <summary>Morning | Evening.</summary>
    public string? Slot { get; set; }
}

public class CreateSubscriptionRequest
{
    public long CustomerId { get; set; }
    public long SubscriptionPlanId { get; set; }
    public long CustomerAddressId { get; set; }

    /// <summary>Null = the plan's product.</summary>
    public long? ProductId { get; set; }

    /// <summary>Null/0 = the plan's default quantity.</summary>
    public decimal? Quantity { get; set; }

    /// <summary>Null = today.</summary>
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }

    /// <summary>Morning | Evening.</summary>
    public string DeliverySlot { get; set; } = "Morning";
}

/// <summary>Null quantity/slot/address = unchanged. EndDate is always applied (null = open ended).</summary>
public class UpdateSubscriptionRequest
{
    public decimal? Quantity { get; set; }
    public DateTime? EndDate { get; set; }
    public string? DeliverySlot { get; set; }
    public long? CustomerAddressId { get; set; }
    public string? Remarks { get; set; }
}

public class PauseSubscriptionRequest
{
    /// <summary>Null = today.</summary>
    public DateTime? PauseFrom { get; set; }

    /// <summary>Null = until resumed.</summary>
    public DateTime? PauseTo { get; set; }
    public string? Reason { get; set; }
}

public class CancelSubscriptionRequest
{
    public string? Reason { get; set; }
}

public class GenerateSubscriptionOrdersRequest
{
    /// <summary>Delivery date to generate orders for; null = today.</summary>
    public DateTime? ForDate { get; set; }
}
