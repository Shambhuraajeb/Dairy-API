namespace Dairy.Application.DTOs.Dashboard;

/// <summary>Flat KPI object; serialized as exactly these 20 lowerCamel fields.</summary>
public class DashboardSummaryDto
{
    public decimal MilkCollectedToday { get; set; }
    public decimal MorningCollection { get; set; }
    public decimal EveningCollection { get; set; }
    public decimal AvgFat { get; set; }
    public decimal AvgSnf { get; set; }
    public decimal TodayMilkValue { get; set; }
    public decimal SalesToday { get; set; }
    public decimal SalesThisMonth { get; set; }
    public decimal FarmerOutstanding { get; set; }
    public decimal CustomerOutstanding { get; set; }
    public long ActiveSubscriptions { get; set; }
    public long LowStockCount { get; set; }
    public long ExpiringInventoryCount { get; set; }
    public long SickAnimals { get; set; }
    public long VaccinationsDue { get; set; }
    public long MaintenanceDue { get; set; }
    public decimal DeliverySuccessRate { get; set; }
    public decimal Revenue { get; set; }
    public decimal Expenses { get; set; }
    public decimal Profit { get; set; }
}

public class ChartPointDto
{
    public string Label { get; set; } = string.Empty;
    public decimal Value { get; set; }
}

public class DashboardChartsDto
{
    public List<ChartPointDto> MilkCollectionTrend { get; set; } = new();
    public List<ChartPointDto> SalesTrend { get; set; } = new();
    public List<ChartPointDto> CollectionByCenter { get; set; } = new();
    public List<ChartPointDto> TopFarmers { get; set; } = new();
    public List<ChartPointDto> ProductSales { get; set; } = new();
    public List<ChartPointDto> ExpenseCategories { get; set; } = new();
}

public class CollectionDashboardDto
{
    public DateTime CollectionDate { get; set; }
    public long? ShiftId { get; set; }
    public string? ShiftName { get; set; }
    public TimeSpan? StartTime { get; set; }
    public TimeSpan? EndTime { get; set; }

    /// <summary>True when the shift window contains the current time; false when showing the most recent shift.</summary>
    public bool IsCurrentShift { get; set; }
    public long FarmersServed { get; set; }
    public long TotalCollections { get; set; }
    public decimal TotalLiters { get; set; }
    public decimal AcceptedLiters { get; set; }
    public decimal RejectedLiters { get; set; }
    public decimal AvgFat { get; set; }
    public decimal AvgSnf { get; set; }
    public long AcceptedCount { get; set; }
    public long RejectedCount { get; set; }
    public decimal TotalAmount { get; set; }
    public long PendingApprovals { get; set; }
}

public class InventoryDashboardDto
{
    public InventorySummaryDto Summary { get; set; } = new();
    public List<LowStockItemDto> LowStockItems { get; set; } = new();
    public List<ExpiringBatchDto> ExpiringBatches { get; set; } = new();
    public List<RecentPurchaseDto> RecentPurchases { get; set; } = new();

    /// <summary>Quantity consumed per day for the last 30 days (label = yyyy-MM-dd).</summary>
    public List<ChartPointDto> ConsumptionTrend { get; set; } = new();
}

public class InventorySummaryDto
{
    public decimal StockValue { get; set; }
    public long TotalItems { get; set; }
    public long LowStockCount { get; set; }
    public long OutOfStockCount { get; set; }
    public long ExpiringBatchCount { get; set; }
    public long ExpiredBatchCount { get; set; }
}

public class LowStockItemDto
{
    public long Id { get; set; }
    public string ItemCode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public decimal CurrentStock { get; set; }
    public decimal ReorderLevel { get; set; }
    public decimal UnitCost { get; set; }
    public string? UnitCode { get; set; }
    public string? UnitSymbol { get; set; }
}

public class ExpiringBatchDto
{
    public long Id { get; set; }
    public long InventoryItemId { get; set; }
    public string ItemCode { get; set; } = string.Empty;
    public string ItemName { get; set; } = string.Empty;
    public string BatchNumber { get; set; } = string.Empty;
    public DateTime ExpiryDate { get; set; }
    public decimal QuantityAvailable { get; set; }

    /// <summary>Negative when already expired.</summary>
    public int DaysToExpiry { get; set; }
}

public class RecentPurchaseDto
{
    public long Id { get; set; }
    public string PurchaseNumber { get; set; } = string.Empty;
    public string SupplierName { get; set; } = string.Empty;
    public DateTime PurchaseDate { get; set; }
    public decimal TotalAmount { get; set; }
    public string Status { get; set; } = string.Empty;
    public string PaymentStatus { get; set; } = string.Empty;
}

public class CustomerDashboardDto
{
    public long CustomerId { get; set; }
    public List<DashSubscriptionDto> Subscriptions { get; set; } = new();
    public List<DashTodayDeliveryDto> TodayDeliveries { get; set; } = new();

    /// <summary>Latest issued invoice, or null.</summary>
    public DashCurrentBillDto? CurrentBill { get; set; }
    public DashOutstandingDto? Outstanding { get; set; }
    public List<DashRecentDeliveryDto> RecentDeliveries { get; set; } = new();
    public List<DashComplaintDto> OpenComplaints { get; set; } = new();
}

public class DashSubscriptionDto
{
    public long Id { get; set; }
    public string SubscriptionNumber { get; set; } = string.Empty;
    public string PlanName { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public string Frequency { get; set; } = string.Empty;
    public string DeliverySlot { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public DateTime? NextOrderDate { get; set; }
    public string? AddressLine { get; set; }
}

public class DashTodayDeliveryDto
{
    public long Id { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public string DeliverySlot { get; set; } = string.Empty;
    public string OrderStatus { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public long? DeliveryId { get; set; }
    public string? DeliveryNumber { get; set; }
    public string? DeliveryStatus { get; set; }
    public DateTime? DeliveredDate { get; set; }
}

public class DashCurrentBillDto
{
    public long Id { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public DateTime InvoiceDate { get; set; }
    public DateTime DueDate { get; set; }
    public DateTime? PeriodFrom { get; set; }
    public DateTime? PeriodTo { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal BalanceAmount { get; set; }
    public string Status { get; set; } = string.Empty;
}

public class DashOutstandingDto
{
    public long Id { get; set; }
    public decimal OutstandingBalance { get; set; }
    public decimal CreditLimit { get; set; }
    public decimal OverdueAmount { get; set; }
    public decimal UnbilledAmount { get; set; }
    public decimal MonthToDateOrders { get; set; }
}

public class DashRecentDeliveryDto
{
    public long Id { get; set; }
    public string DeliveryNumber { get; set; } = string.Empty;
    public string OrderNumber { get; set; } = string.Empty;
    public DateTime DeliveryDate { get; set; }
    public string DeliverySlot { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime? DeliveredDate { get; set; }
    public string? FailureReason { get; set; }
    public decimal TotalAmount { get; set; }
}

public class DashComplaintDto
{
    public long Id { get; set; }
    public string ComplaintNumber { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string? Category { get; set; }
    public string? Priority { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedDate { get; set; }
}
