namespace Dairy.Application.DTOs.Orders;

public class OrderListDto
{
    public long Id { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public long CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerCode { get; set; } = string.Empty;
    public long? CustomerAddressId { get; set; }
    public string? AddressText { get; set; }
    public long? CustomerSubscriptionId { get; set; }
    public long? RouteId { get; set; }
    public string? RouteName { get; set; }
    public DateTime OrderDate { get; set; }
    public DateTime DeliveryDate { get; set; }

    /// <summary>Morning | Evening.</summary>
    public string DeliverySlot { get; set; } = string.Empty;

    /// <summary>Manual | Subscription.</summary>
    public string Source { get; set; } = string.Empty;
    public decimal SubTotal { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal DeliveryCharge { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal PaidAmount { get; set; }

    /// <summary>Pending | Confirmed | Scheduled | Dispatched | Delivered | Cancelled | Failed.</summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>Unpaid | PartiallyPaid | Paid.</summary>
    public string PaymentStatus { get; set; } = string.Empty;
    public long? InvoiceId { get; set; }
    public string? InvoiceNumber { get; set; }
    public decimal TotalQuantity { get; set; }
    public string? Remarks { get; set; }
    public DateTime CreatedDate { get; set; }
}

public class OrderDetailDto : OrderListDto
{
    public DateTime? ConfirmedDate { get; set; }
    public DateTime? CancelledDate { get; set; }
    public string? CancelReason { get; set; }
    public List<OrderLineDto> Lines { get; set; } = new();

    /// <summary>Latest non-cancelled delivery of the order (null until scheduled).</summary>
    public OrderDeliveryDto? Delivery { get; set; }
}

public class OrderLineDto
{
    public long Id { get; set; }
    public long ProductId { get; set; }
    public string ProductCode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string? UnitCode { get; set; }
    public decimal Quantity { get; set; }
    public decimal DeliveredQuantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TaxPercent { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal LineTotal { get; set; }
    public string? Remarks { get; set; }
}

public class OrderDeliveryDto
{
    public long Id { get; set; }
    public string DeliveryNumber { get; set; } = string.Empty;

    /// <summary>Pending | Dispatched | Delivered | Failed.</summary>
    public string Status { get; set; } = string.Empty;
    public long? DeliveryScheduleId { get; set; }
    public long? RouteId { get; set; }
    public string? RouteName { get; set; }
    public long? DriverEmployeeId { get; set; }
    public string? DriverName { get; set; }
    public long? AssignedUserId { get; set; }
    public DateTime? DispatchedDate { get; set; }
    public DateTime? DeliveredDate { get; set; }
    public string? FailureReason { get; set; }
}
