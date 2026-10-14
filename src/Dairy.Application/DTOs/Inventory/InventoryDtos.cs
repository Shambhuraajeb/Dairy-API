namespace Dairy.Application.DTOs.Inventory;

public class ItemCategoryDto
{
    public long Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public int ItemCount { get; set; }
}

public class InventoryItemDto
{
    public long Id { get; set; }
    public string ItemCode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public long ItemCategoryId { get; set; }
    public string? CategoryName { get; set; }
    public long UnitId { get; set; }
    public string? UnitName { get; set; }
    public string? Description { get; set; }
    public decimal ReorderLevel { get; set; }
    public decimal? MaxStockLevel { get; set; }
    public decimal CurrentStock { get; set; }
    public decimal UnitCost { get; set; }
    public decimal StockValue { get; set; }
    public decimal TaxPercent { get; set; }
    public bool IsPerishable { get; set; }
    public bool HasExpiry { get; set; }
    public int? ShelfLifeDays { get; set; }
    public long? PreferredSupplierId { get; set; }
    public string? PreferredSupplierName { get; set; }
    public string? StorageLocation { get; set; }
    public string? Barcode { get; set; }
    public bool IsActive { get; set; }

    /// <summary>OutOfStock | Low | OK</summary>
    public string StockStatus { get; set; } = "OK";
}

public class InventoryItemDetailDto : InventoryItemDto
{
    public int TotalBatches { get; set; }
    public int AvailableBatches { get; set; }
    public int ExpiredBatches { get; set; }
    public DateTime? NearestExpiryDate { get; set; }
    public List<InventoryBatchDto> Batches { get; set; } = new();
}

public class InventoryBatchDto
{
    public long Id { get; set; }
    public long InventoryItemId { get; set; }
    public string? ItemCode { get; set; }
    public string? ItemName { get; set; }
    public string? UnitName { get; set; }
    public string BatchNumber { get; set; } = string.Empty;
    public long? SupplierId { get; set; }
    public string? SupplierName { get; set; }
    public long? PurchaseDetailId { get; set; }
    public string? PurchaseNumber { get; set; }
    public DateTime? ManufactureDate { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public DateTime ReceivedDate { get; set; }
    public decimal QuantityReceived { get; set; }
    public decimal QuantityAvailable { get; set; }
    public decimal UnitCost { get; set; }
    public decimal StockValue { get; set; }
    public string? StorageLocation { get; set; }

    /// <summary>Available | Depleted | Expired | Quarantined</summary>
    public string Status { get; set; } = string.Empty;

    public int? DaysToExpiry { get; set; }
    public bool IsExpired { get; set; }
}

public class StockTransactionDto
{
    public long Id { get; set; }
    public long InventoryItemId { get; set; }
    public string? ItemCode { get; set; }
    public string? ItemName { get; set; }
    public string? UnitName { get; set; }
    public long? InventoryBatchId { get; set; }
    public string? BatchNumber { get; set; }

    /// <summary>Purchase | Consumption | Adjustment | Return | Sale | Opening</summary>
    public string TransactionType { get; set; } = string.Empty;

    /// <summary>Signed: positive = stock in, negative = stock out.</summary>
    public decimal Quantity { get; set; }
    public decimal UnitCost { get; set; }
    public decimal BalanceAfter { get; set; }
    public string? ReferenceType { get; set; }
    public long? ReferenceId { get; set; }
    public string? Remarks { get; set; }
    public DateTime TransactionDate { get; set; }
    public long? CreatedBy { get; set; }
    public string? CreatedByName { get; set; }
}

public class InventoryConsumptionDto
{
    public long Id { get; set; }
    public long InventoryItemId { get; set; }
    public string? ItemCode { get; set; }
    public string? ItemName { get; set; }
    public string? UnitName { get; set; }
    public long? InventoryBatchId { get; set; }
    public string? BatchNumber { get; set; }
    public long? StockTransactionId { get; set; }
    public decimal Quantity { get; set; }
    public decimal? UnitCost { get; set; }
    public decimal CostValue { get; set; }
    public DateTime ConsumptionDate { get; set; }
    public string? Purpose { get; set; }
    public long? BranchId { get; set; }
    public long? ConsumedBy { get; set; }
    public string? ConsumedByName { get; set; }
    public bool ExpiredOverride { get; set; }
    public string? OverrideReason { get; set; }
    public DateTime CreatedDate { get; set; }
}

/// <summary>Result row of sp_RecordInventoryConsumption.</summary>
public class ConsumptionResultDto
{
    public long? Id { get; set; }
    public decimal Quantity { get; set; }
    public int BatchesUsed { get; set; }
    public decimal CurrentStock { get; set; }
}

/// <summary>Result row of sp_AdjustStock.</summary>
public class StockAdjustmentResultDto
{
    public long? Id { get; set; }
    public decimal PreviousStock { get; set; }
    public decimal CurrentStock { get; set; }
}

public class LowStockAlertDto
{
    public long Id { get; set; }
    public string? ItemCode { get; set; }
    public string? ItemName { get; set; }
    public string? CategoryName { get; set; }
    public string? UnitName { get; set; }
    public decimal CurrentStock { get; set; }
    public decimal ReorderLevel { get; set; }
    public decimal? MaxStockLevel { get; set; }
    public decimal Shortfall { get; set; }
    public decimal UnitCost { get; set; }

    /// <summary>OutOfStock | Low</summary>
    public string StockStatus { get; set; } = string.Empty;
}

public class ExpiringStockAlertDto
{
    /// <summary>Batch id.</summary>
    public long Id { get; set; }
    public long InventoryItemId { get; set; }
    public string? ItemCode { get; set; }
    public string? ItemName { get; set; }
    public string? BatchNumber { get; set; }
    public DateTime ExpiryDate { get; set; }
    public decimal QuantityAvailable { get; set; }
    public string? UnitName { get; set; }
    public decimal UnitCost { get; set; }
    public decimal StockValue { get; set; }
    public int DaysToExpiry { get; set; }
    public bool IsExpired { get; set; }
    public string? Status { get; set; }
}

public class InventorySummaryDto
{
    public int TotalItems { get; set; }
    public int ActiveItems { get; set; }
    public int CategoryCount { get; set; }
    public int SupplierCount { get; set; }
    public decimal StockValue { get; set; }
    public int LowStockCount { get; set; }
    public int OutOfStockCount { get; set; }
    public int ExpiringBatchCount { get; set; }
    public int ExpiredBatchCount { get; set; }
    public decimal ExpiredStockValue { get; set; }
    public int OpenPurchaseCount { get; set; }
    public int PendingApprovalCount { get; set; }
}

public class PurchaseDto
{
    public long Id { get; set; }
    public string PurchaseNumber { get; set; } = string.Empty;
    public long SupplierId { get; set; }
    public string? SupplierName { get; set; }
    public long? BranchId { get; set; }
    public DateTime PurchaseDate { get; set; }
    public DateTime? ExpectedDate { get; set; }
    public string? SupplierInvoiceNo { get; set; }
    public decimal SubTotal { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal PaidAmount { get; set; }

    /// <summary>Unpaid | Partial | Paid</summary>
    public string PaymentStatus { get; set; } = string.Empty;

    /// <summary>Draft | Approved | Received | Cancelled</summary>
    public string Status { get; set; } = string.Empty;

    public long? ApprovedBy { get; set; }
    public string? ApprovedByName { get; set; }
    public DateTime? ApprovedDate { get; set; }
    public long? ReceivedBy { get; set; }
    public string? ReceivedByName { get; set; }
    public DateTime? ReceivedDate { get; set; }
    public DateTime? CancelledDate { get; set; }
    public string? CancelReason { get; set; }
    public string? Remarks { get; set; }
    public long? CreatedBy { get; set; }
    public string? CreatedByName { get; set; }
    public DateTime CreatedDate { get; set; }
    public int LineCount { get; set; }
}

public class PurchaseDetailDto : PurchaseDto
{
    public List<PurchaseLineDto> Lines { get; set; } = new();
}

public class PurchaseLineDto
{
    public long Id { get; set; }
    public long PurchaseId { get; set; }
    public long InventoryItemId { get; set; }
    public string? ItemCode { get; set; }
    public string? ItemName { get; set; }
    public string? UnitName { get; set; }
    public decimal Quantity { get; set; }
    public decimal ReceivedQuantity { get; set; }
    public decimal UnitCost { get; set; }
    public decimal TaxPercent { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal LineTotal { get; set; }
    public string? BatchNumber { get; set; }
    public DateTime? ManufactureDate { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public string? Remarks { get; set; }
}

/// <summary>Result row of sp_CreatePurchase.</summary>
public class PurchaseCreatedDto
{
    public long Id { get; set; }
    public string? Number { get; set; }
    public decimal TotalAmount { get; set; }
}

/// <summary>Result row of sp_ReceivePurchaseAndUpdateStock.</summary>
public class PurchaseReceivedDto
{
    public long Id { get; set; }
    public string? Number { get; set; }
    public int LinesReceived { get; set; }
    public string? Status { get; set; }
}
