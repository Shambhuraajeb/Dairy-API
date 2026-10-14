using Dairy.Application.Common;

namespace Dairy.Application.Requests.Orders;

public class OrderQuery : PagedQuery
{
    /// <summary>Forced to the signed-in customer for Customer/Distributor roles.</summary>
    public long? CustomerId { get; set; }

    /// <summary>Pending | Confirmed | Scheduled | Dispatched | Delivered | Cancelled | Failed.</summary>
    public string? Status { get; set; }

    /// <summary>Delivery date range (inclusive).</summary>
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }

    /// <summary>Morning | Evening.</summary>
    public string? Slot { get; set; }
    public long? RouteId { get; set; }

    /// <summary>Manual | Subscription.</summary>
    public string? Source { get; set; }
}

public class OrderItemRequest
{
    public long ProductId { get; set; }
    public decimal Quantity { get; set; }

    /// <summary>Optional price override (staff only; ignored for customers). Null = price list.</summary>
    public decimal? UnitPrice { get; set; }
}

public class CreateOrderRequest
{
    /// <summary>Required for staff; ignored for Customer/Distributor roles (own customer is used).</summary>
    public long? CustomerId { get; set; }

    /// <summary>Null = the customer's default address.</summary>
    public long? CustomerAddressId { get; set; }
    public DateTime DeliveryDate { get; set; }

    /// <summary>Morning | Evening.</summary>
    public string DeliverySlot { get; set; } = "Morning";
    public string? Remarks { get; set; }
    public List<OrderItemRequest> Items { get; set; } = new();
}

/// <summary>Allowed only while the order is Pending/Confirmed and not yet scheduled. Null = unchanged (remarks always applied).</summary>
public class UpdateOrderRequest
{
    public long? CustomerAddressId { get; set; }
    public DateTime? DeliveryDate { get; set; }
    public string? DeliverySlot { get; set; }
    public string? Remarks { get; set; }
}

public class CancelOrderRequest
{
    public string? Reason { get; set; }
}
