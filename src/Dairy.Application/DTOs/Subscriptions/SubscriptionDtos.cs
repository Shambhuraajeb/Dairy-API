namespace Dairy.Application.DTOs.Subscriptions;

public class SubscriptionPlanDto
{
    public long Id { get; set; }
    public string PlanCode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Frequency { get; set; } = string.Empty;
    public string? DaysOfWeek { get; set; }
    public long? ProductId { get; set; }
    public string? ProductName { get; set; }
    public decimal DefaultQuantity { get; set; }
    public decimal? PricePerUnit { get; set; }
    public decimal DiscountPercent { get; set; }
    public string BillingCycle { get; set; } = string.Empty;
    public int MinimumDays { get; set; }
    public bool IsActive { get; set; }
    public int ActiveSubscriptions { get; set; }
}

public class SubscriptionDto
{
    public long Id { get; set; }
    public string SubscriptionNumber { get; set; } = string.Empty;
    public long CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerCode { get; set; } = string.Empty;
    public long SubscriptionPlanId { get; set; }
    public string PlanName { get; set; } = string.Empty;
    public long CustomerAddressId { get; set; }
    public string? AddressText { get; set; }
    public long ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public string Frequency { get; set; } = string.Empty;
    public string? DaysOfWeek { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string DeliverySlot { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime? NextOrderDate { get; set; }
    public DateTime? LastOrderDate { get; set; }
    public DateTime? CancelledDate { get; set; }
    public string? CancelReason { get; set; }
    public string? Remarks { get; set; }
    public DateTime CreatedDate { get; set; }
}

public class SubscriptionDetailDto : SubscriptionDto
{
    public List<SubscriptionPauseDto> Pauses { get; set; } = new();
}

public class SubscriptionPauseDto
{
    public long Id { get; set; }
    public long CustomerSubscriptionId { get; set; }
    public DateTime PauseFrom { get; set; }
    public DateTime? PauseTo { get; set; }
    public string? Reason { get; set; }
    public DateTime? ResumedDate { get; set; }
    public long? ResumedBy { get; set; }

    /// <summary>Active | Resumed | Completed | Cancelled.</summary>
    public string Status { get; set; } = string.Empty;
}

public class SubscriptionOrdersGeneratedDto
{
    public int OrdersCreated { get; set; }
    public DateTime ForDate { get; set; }
}
