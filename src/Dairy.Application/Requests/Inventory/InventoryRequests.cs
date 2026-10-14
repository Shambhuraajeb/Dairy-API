using Dairy.Application.Common;

namespace Dairy.Application.Requests.Inventory;

// ---------- item categories ----------

public class ItemCategoryQuery : PagedQuery
{
    public bool? IsActive { get; set; }
}

public class CreateItemCategoryRequest
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
}

public class UpdateItemCategoryRequest : CreateItemCategoryRequest
{
    /// <summary>Null = leave unchanged; false = deactivate.</summary>
    public bool? IsActive { get; set; }
}

// ---------- items ----------

public class InventoryItemQuery : PagedQuery
{
    public long? CategoryId { get; set; }
    public bool? LowStockOnly { get; set; }
    public bool? IsActive { get; set; }
}

public class CreateInventoryItemRequest
{
    public string ItemCode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public long ItemCategoryId { get; set; }
    public long UnitId { get; set; }
    public string? Description { get; set; }
    public decimal ReorderLevel { get; set; }
    public decimal? MaxStockLevel { get; set; }
    public decimal UnitCost { get; set; }
    public decimal TaxPercent { get; set; }
    public bool IsPerishable { get; set; }
    public bool HasExpiry { get; set; }
    public int? ShelfLifeDays { get; set; }
    public long? PreferredSupplierId { get; set; }
    public string? StorageLocation { get; set; }
    public string? Barcode { get; set; }
}

public class UpdateInventoryItemRequest : CreateInventoryItemRequest
{
    /// <summary>Null = leave unchanged; false = deactivate.</summary>
    public bool? IsActive { get; set; }
}

// ---------- batches / ledger / consumption / adjustments ----------

public class BatchQuery : PagedQuery
{
    public long? ItemId { get; set; }

    /// <summary>true = not yet expired but expiring within <see cref="Days"/> (default inventory.expiryAlertDays), stock remaining.</summary>
    public bool? Expiring { get; set; }

    /// <summary>true = already expired with stock remaining.</summary>
    public bool? Expired { get; set; }

    /// <summary>true = stock remaining, status Available and not expired.</summary>
    public bool? Available { get; set; }

    public int? Days { get; set; }

    /// <summary>Available | Depleted | Expired | Quarantined</summary>
    public string? Status { get; set; }
}

public class StockTransactionQuery : PagedQuery
{
    public long? ItemId { get; set; }
    public long? BatchId { get; set; }

    /// <summary>Purchase | Consumption | Adjustment | Return | Sale | Opening</summary>
    public string? Type { get; set; }

    public DateTime? From { get; set; }
    public DateTime? To { get; set; }
}

public class ConsumptionQuery : PagedQuery
{
    public long? ItemId { get; set; }
    public long? BatchId { get; set; }
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }
    public bool? ExpiredOverride { get; set; }
}

public class RecordConsumptionRequest
{
    public long InventoryItemId { get; set; }

    /// <summary>Null = consume FEFO across batches.</summary>
    public long? InventoryBatchId { get; set; }

    public decimal Quantity { get; set; }
    public DateTime? ConsumptionDate { get; set; }
    public string? Purpose { get; set; }

    /// <summary>Consume expired stock. Requires permission inventory.adjust (and system setting inventory.allowExpiredOverride).</summary>
    public bool ExpiredOverride { get; set; }
}

public class AdjustStockRequest
{
    public long InventoryItemId { get; set; }

    /// <summary>Null = positive: unbatched receipt; negative: FEFO across batches.</summary>
    public long? InventoryBatchId { get; set; }

    /// <summary>Signed, non-zero: positive adds stock, negative removes stock.</summary>
    public decimal QuantityDelta { get; set; }

    public string Reason { get; set; } = string.Empty;
}

// ---------- purchases ----------

public class PurchaseQuery : PagedQuery
{
    public long? SupplierId { get; set; }

    /// <summary>Draft | Approved | Received | Cancelled</summary>
    public string? Status { get; set; }

    /// <summary>Unpaid | Partial | Paid</summary>
    public string? PaymentStatus { get; set; }

    public DateTime? From { get; set; }
    public DateTime? To { get; set; }
}

public class PurchaseLineRequest
{
    public long InventoryItemId { get; set; }
    public decimal Quantity { get; set; }

    /// <summary>Null = use the item's current unit cost.</summary>
    public decimal? UnitCost { get; set; }

    public string? BatchNumber { get; set; }
    public DateTime? ManufactureDate { get; set; }
    public DateTime? ExpiryDate { get; set; }
}

public class CreatePurchaseRequest
{
    public long SupplierId { get; set; }
    public DateTime? PurchaseDate { get; set; }
    public string? Remarks { get; set; }
    public List<PurchaseLineRequest> Items { get; set; } = new();
}

public class ReceivePurchaseRequest
{
    /// <summary>Null = today.</summary>
    public DateTime? ReceivedDate { get; set; }
}

public class CancelPurchaseRequest
{
    public string Reason { get; set; } = string.Empty;
}
