using Dairy.Application.Common;
using Dairy.Application.DTOs.Reports;

namespace Dairy.Application.BusinessRules;

public class ReportDefinition
{
    public string Code { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Group { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string[] Filters { get; init; } = Array.Empty<string>();
    public List<ReportColumnDto> Columns { get; init; } = new();

    /// <summary>Numeric column keys that are summed into <c>totals</c>.</summary>
    public string[] TotalKeys { get; init; } = Array.Empty<string>();
}

/// <summary>The 19 report-center reports: code, display metadata, supported filters and column hints.</summary>
public static class ReportCatalog
{
    private static readonly string[] CollectionFilters = { "from", "to", "branchId", "collectionCenterId", "farmerId", "shiftId", "status" };
    private static readonly string[] CollectionNoStatus = { "from", "to", "branchId", "collectionCenterId", "farmerId", "shiftId" };
    private static readonly string[] SalesFilters = { "from", "to", "customerId", "productId", "status" };
    private static readonly string[] InventoryFilters = { "categoryId", "itemId" };
    private static readonly string[] InventoryRangeFilters = { "from", "to", "categoryId", "itemId" };

    private static readonly string[] CollectionSummaryTotals =
        { "totalCollections", "totalQuantity", "acceptedQuantity", "rejectedQuantity", "totalAmount", "pendingCount", "approvedCount", "rejectedCount" };

    public static readonly IReadOnlyList<ReportDefinition> All = Build();

    public static ReportDefinition? Find(string? code) =>
        string.IsNullOrWhiteSpace(code)
            ? null
            : All.FirstOrDefault(r => string.Equals(r.Code, code.Trim(), StringComparison.OrdinalIgnoreCase));

    private static ReportColumnDto C(string key, string type = "text", string? label = null) =>
        new() { Key = key, Label = label ?? B5Text.Humanize(key), Type = type };

    private static List<ReportColumnDto> Cols(params ReportColumnDto[] columns) => columns.ToList();

    private static List<ReportDefinition> Build() => new()
    {
        // ---------------- Collection ----------------
        new ReportDefinition
        {
            Code = "daily-collection", Name = "Daily Collection", Group = "Collection",
            Description = "Milk collected per day with quality averages and value.",
            Filters = CollectionFilters, TotalKeys = CollectionSummaryTotals,
            Columns = Cols(C("collectionDate", "date", "Date"), C("totalCollections", "number", "Collections"), C("farmerCount", "number", "Farmers"),
                C("totalQuantity", "number", "Total (L)"), C("acceptedQuantity", "number", "Accepted (L)"), C("rejectedQuantity", "number", "Rejected (L)"),
                C("avgFat", "number", "Avg FAT"), C("avgSnf", "number", "Avg SNF"), C("totalAmount", "money", "Amount"),
                C("pendingCount", "number", "Pending"), C("approvedCount", "number", "Approved"), C("rejectedCount", "number", "Rejected"))
        },
        new ReportDefinition
        {
            Code = "shift-collection", Name = "Shift Collection", Group = "Collection",
            Description = "Milk collected per day, shift and collection center.",
            Filters = CollectionFilters, TotalKeys = CollectionSummaryTotals,
            Columns = Cols(C("collectionDate", "date", "Date"), C("shiftName", "text", "Shift"), C("collectionCenterName", "text", "Collection Center"),
                C("totalCollections", "number", "Collections"), C("farmerCount", "number", "Farmers"),
                C("totalQuantity", "number", "Total (L)"), C("acceptedQuantity", "number", "Accepted (L)"), C("rejectedQuantity", "number", "Rejected (L)"),
                C("avgFat", "number", "Avg FAT"), C("avgSnf", "number", "Avg SNF"), C("totalAmount", "money", "Amount"),
                C("pendingCount", "number", "Pending"), C("approvedCount", "number", "Approved"), C("rejectedCount", "number", "Rejected"))
        },
        new ReportDefinition
        {
            Code = "farmer-collection", Name = "Farmer Collection", Group = "Collection",
            Description = "Milk supplied per farmer with average quality and rate.",
            Filters = CollectionFilters,
            TotalKeys = new[] { "totalCollections", "totalQuantity", "acceptedQuantity", "rejectedQuantity", "totalAmount", "pendingCount", "approvedCount", "rejectedCount" },
            Columns = Cols(C("farmerCode", "text", "Farmer Code"), C("farmerName", "text", "Farmer"), C("totalCollections", "number", "Collections"),
                C("totalQuantity", "number", "Total (L)"), C("acceptedQuantity", "number", "Accepted (L)"), C("rejectedQuantity", "number", "Rejected (L)"),
                C("avgFat", "number", "Avg FAT"), C("avgSnf", "number", "Avg SNF"), C("avgRate", "money", "Avg Rate"), C("totalAmount", "money", "Amount"),
                C("pendingCount", "number", "Pending"), C("approvedCount", "number", "Approved"), C("rejectedCount", "number", "Rejected"))
        },
        new ReportDefinition
        {
            Code = "milk-quality", Name = "Milk Quality", Group = "Collection",
            Description = "Collection-level quality test results and adulteration flags.",
            Filters = CollectionFilters, TotalKeys = new[] { "quantityLiters" },
            Columns = Cols(C("collectionNumber", "text", "Collection No"), C("collectionDate", "date", "Date"), C("shiftName", "text", "Shift"),
                C("collectionCenterName", "text", "Collection Center"), C("farmerCode", "text", "Farmer Code"), C("farmerName", "text", "Farmer"),
                C("quantityLiters", "number", "Qty (L)"), C("fat", "number", "FAT"), C("snf", "number", "SNF"), C("clr", "number", "CLR"),
                C("temperature", "number", "Temp"), C("acidity", "number", "Acidity"),
                C("waterAdded", "boolean", "Water"), C("starchDetected", "boolean", "Starch"), C("ureaDetected", "boolean", "Urea"),
                C("neutralizerDetected", "boolean", "Neutralizer"), C("antibioticDetected", "boolean", "Antibiotic"),
                C("qualityResult", "text", "Result"), C("failureReason", "text", "Failure Reason"), C("status", "text", "Status"))
        },
        new ReportDefinition
        {
            Code = "rejected-milk", Name = "Rejected Milk", Group = "Collection",
            Description = "Rejected collections with reasons and disposal.",
            Filters = CollectionNoStatus, TotalKeys = new[] { "quantityLiters" },
            Columns = Cols(C("rejectionDate", "date", "Rejected On"), C("collectionNumber", "text", "Collection No"), C("collectionDate", "date", "Collected On"),
                C("shiftName", "text", "Shift"), C("collectionCenterName", "text", "Collection Center"), C("farmerCode", "text", "Farmer Code"),
                C("farmerName", "text", "Farmer"), C("reasonCode", "text", "Reason Code"), C("reason", "text", "Reason"),
                C("quantityLiters", "number", "Qty (L)"), C("disposalMethod", "text", "Disposal"), C("collectionStatus", "text", "Collection Status"))
        },

        // ---------------- Farmer payments ----------------
        new ReportDefinition
        {
            Code = "farmer-settlement", Name = "Farmer Settlement", Group = "Farmer Payments",
            Description = "Farmer settlements created in the period with gross, deductions and net payable.",
            Filters = new[] { "from", "to", "farmerId", "status" },
            TotalKeys = new[] { "totalCollections", "totalQuantity", "grossAmount", "bonusAmount", "penaltyAmount", "deductionAmount", "netPayable", "paidAmount", "balanceAmount" },
            Columns = Cols(C("paymentNumber", "text", "Payment No"), C("paymentDate", "date", "Date"), C("periodFrom", "date", "Period From"), C("periodTo", "date", "Period To"),
                C("farmerCode", "text", "Farmer Code"), C("farmerName", "text", "Farmer"), C("totalCollections", "number", "Collections"),
                C("totalQuantity", "number", "Qty (L)"), C("grossAmount", "money", "Gross"), C("bonusAmount", "money", "Bonus"), C("penaltyAmount", "money", "Penalty"),
                C("deductionAmount", "money", "Deductions"), C("netPayable", "money", "Net Payable"), C("paidAmount", "money", "Paid"),
                C("balanceAmount", "money", "Balance"), C("status", "text", "Status"))
        },
        new ReportDefinition
        {
            Code = "farmer-outstanding", Name = "Farmer Outstanding", Group = "Farmer Payments",
            Description = "Per-farmer payable position versus advances and loans recoverable.",
            Filters = new[] { "from", "to", "farmerId" },
            TotalKeys = new[] { "unsettledQuantity", "unsettledAmount", "pendingPaymentBalance", "payableToFarmer", "advanceBalance", "loanOutstanding", "recoverableFromFarmer", "netOutstanding" },
            Columns = Cols(C("farmerCode", "text", "Farmer Code"), C("farmerName", "text", "Farmer"), C("unsettledQuantity", "number", "Unsettled (L)"),
                C("unsettledAmount", "money", "Unsettled Amount"), C("pendingPaymentBalance", "money", "Pending Payments"), C("payableToFarmer", "money", "Payable"),
                C("advanceBalance", "money", "Advance Balance"), C("loanOutstanding", "money", "Loan Outstanding"),
                C("recoverableFromFarmer", "money", "Recoverable"), C("netOutstanding", "money", "Net Outstanding"))
        },

        // ---------------- Animals ----------------
        new ReportDefinition
        {
            Code = "animal-health", Name = "Animal Health", Group = "Animals",
            Description = "Animal health records with diagnosis, treatment and cost.",
            Filters = new[] { "from", "to", "branchId", "collectionCenterId", "farmerId", "status" },
            TotalKeys = new[] { "cost" },
            Columns = Cols(C("recordDate", "date", "Date"), C("tagNumber", "text", "Tag"), C("animalName", "text", "Animal"), C("animalType", "text", "Type"),
                C("farmerCode", "text", "Farmer Code"), C("farmerName", "text", "Farmer"), C("healthCondition", "text", "Condition"), C("severity", "text", "Severity"),
                C("diagnosis", "text", "Diagnosis"), C("treatment", "text", "Treatment"), C("vetName", "text", "Vet"), C("cost", "money", "Cost"),
                C("status", "text", "Status"), C("followUpDate", "date", "Follow-up"), C("recoveredDate", "date", "Recovered On"))
        },
        new ReportDefinition
        {
            Code = "vaccination-due", Name = "Vaccination Due", Group = "Animals",
            Description = "Animals whose next vaccination is due or overdue (default: due within 30 days). 'to' = due-until date; status = Due|Overdue.",
            Filters = new[] { "from", "to", "branchId", "collectionCenterId", "farmerId", "status" },
            Columns = Cols(C("tagNumber", "text", "Tag"), C("animalName", "text", "Animal"), C("animalType", "text", "Type"),
                C("farmerCode", "text", "Farmer Code"), C("farmerName", "text", "Farmer"), C("vaccineName", "text", "Vaccine"),
                C("lastVaccinationDate", "date", "Last Dose"), C("nextDueDate", "date", "Next Due"), C("daysToDue", "number", "Days To Due"),
                C("dueStatus", "text", "Due Status"))
        },

        // ---------------- Inventory ----------------
        new ReportDefinition
        {
            Code = "inventory-stock", Name = "Inventory Stock", Group = "Inventory",
            Description = "Current stock and stock value per item.",
            Filters = InventoryFilters, TotalKeys = new[] { "stockValue" },
            Columns = Cols(C("itemCode", "text", "Item Code"), C("itemName", "text", "Item"), C("categoryName", "text", "Category"), C("unitName", "text", "Unit"),
                C("currentStock", "number", "Stock"), C("reorderLevel", "number", "Reorder Level"), C("maxStockLevel", "number", "Max Level"),
                C("unitCost", "money", "Unit Cost"), C("stockValue", "money", "Stock Value"), C("stockStatus", "text", "Stock Status"))
        },
        new ReportDefinition
        {
            Code = "low-stock", Name = "Low Stock", Group = "Inventory",
            Description = "Items at or below their reorder level.",
            Filters = InventoryFilters, TotalKeys = new[] { "stockValue" },
            Columns = Cols(C("itemCode", "text", "Item Code"), C("itemName", "text", "Item"), C("categoryName", "text", "Category"), C("unitName", "text", "Unit"),
                C("currentStock", "number", "Stock"), C("reorderLevel", "number", "Reorder Level"), C("maxStockLevel", "number", "Max Level"),
                C("unitCost", "money", "Unit Cost"), C("stockValue", "money", "Stock Value"), C("stockStatus", "text", "Stock Status"))
        },
        new ReportDefinition
        {
            Code = "expiring-batches", Name = "Expiring Batches", Group = "Inventory",
            Description = "Batches that are expired or expire within the alert window (or up to the 'to' date).",
            Filters = InventoryRangeFilters, TotalKeys = new[] { "quantityAvailable", "stockValue" },
            Columns = Cols(C("itemCode", "text", "Item Code"), C("itemName", "text", "Item"), C("categoryName", "text", "Category"), C("batchNumber", "text", "Batch"),
                C("expiryDate", "date", "Expiry"), C("quantityAvailable", "number", "Available"), C("unitCost", "money", "Unit Cost"),
                C("stockValue", "money", "Stock Value"), C("daysToExpiry", "number", "Days To Expiry"), C("expiryStatus", "text", "Status"))
        },
        new ReportDefinition
        {
            Code = "purchase", Name = "Purchases", Group = "Inventory",
            Description = "Purchase order lines by supplier and item.",
            Filters = InventoryRangeFilters, TotalKeys = new[] { "quantity", "receivedQuantity", "taxAmount", "lineTotal" },
            Columns = Cols(C("purchaseNumber", "text", "Purchase No"), C("purchaseDate", "date", "Date"), C("supplierName", "text", "Supplier"),
                C("itemCode", "text", "Item Code"), C("itemName", "text", "Item"), C("categoryName", "text", "Category"),
                C("quantity", "number", "Qty"), C("receivedQuantity", "number", "Received"), C("unitCost", "money", "Unit Cost"),
                C("taxAmount", "money", "Tax"), C("lineTotal", "money", "Line Total"), C("batchNumber", "text", "Batch"),
                C("expiryDate", "date", "Expiry"), C("status", "text", "Status"))
        },

        // ---------------- Sales ----------------
        new ReportDefinition
        {
            Code = "sales", Name = "Sales", Group = "Sales",
            Description = "Sales order lines by delivery date, customer and product.",
            Filters = SalesFilters, TotalKeys = new[] { "quantity", "deliveredQuantity", "discountAmount", "taxAmount", "lineTotal" },
            Columns = Cols(C("orderNumber", "text", "Order No"), C("deliveryDate", "date", "Delivery Date"), C("status", "text", "Status"), C("source", "text", "Source"),
                C("customerCode", "text", "Customer Code"), C("customerName", "text", "Customer"), C("productCode", "text", "Product Code"),
                C("productName", "text", "Product"), C("quantity", "number", "Qty"), C("deliveredQuantity", "number", "Delivered"),
                C("unitPrice", "money", "Unit Price"), C("discountAmount", "money", "Discount"), C("taxAmount", "money", "Tax"),
                C("lineTotal", "money", "Line Total"), C("invoiceNumber", "text", "Invoice"))
        },
        new ReportDefinition
        {
            Code = "customer-outstanding", Name = "Customer Outstanding", Group = "Sales",
            Description = "Open invoices with balance and days overdue.",
            Filters = SalesFilters, TotalKeys = new[] { "totalAmount", "paidAmount", "balanceAmount" },
            Columns = Cols(C("invoiceNumber", "text", "Invoice"), C("customerCode", "text", "Customer Code"), C("customerName", "text", "Customer"),
                C("invoiceDate", "date", "Invoice Date"), C("dueDate", "date", "Due Date"), C("totalAmount", "money", "Total"),
                C("paidAmount", "money", "Paid"), C("balanceAmount", "money", "Balance"), C("daysOverdue", "number", "Days Overdue"), C("status", "text", "Status"))
        },
        new ReportDefinition
        {
            Code = "delivery-performance", Name = "Delivery Performance", Group = "Sales",
            Description = "Deliveries per driver with success rate.",
            Filters = SalesFilters, TotalKeys = new[] { "totalDeliveries", "delivered", "failed", "pending" },
            Columns = Cols(C("driverName", "text", "Driver"), C("totalDeliveries", "number", "Total"), C("delivered", "number", "Delivered"),
                C("failed", "number", "Failed"), C("pending", "number", "Pending"), C("successRate", "number", "Success %"))
        },

        // ---------------- Finance ----------------
        new ReportDefinition
        {
            Code = "expenses", Name = "Expenses", Group = "Finance",
            Description = "Expense register by category and status ('categoryId' = expense category).",
            Filters = new[] { "from", "to", "categoryId", "status" }, TotalKeys = new[] { "amount" },
            Columns = Cols(C("expenseNumber", "text", "Expense No"), C("expenseDate", "date", "Date"), C("categoryName", "text", "Category"),
                C("amount", "money", "Amount"), C("paymentModeName", "text", "Payment Mode"), C("description", "text", "Description"),
                C("payee", "text", "Payee"), C("branchName", "text", "Branch"), C("status", "text", "Status"),
                C("approvedDate", "date", "Approved On"), C("rejectionReason", "text", "Rejection Reason"))
        },
        new ReportDefinition
        {
            Code = "revenue", Name = "Revenue", Group = "Finance",
            Description = "Invoiced revenue versus customer collections per day.",
            Filters = SalesFilters, TotalKeys = new[] { "invoiced", "collected", "difference" },
            Columns = Cols(C("reportDate", "date", "Date"), C("invoiced", "money", "Invoiced"), C("collected", "money", "Collected"), C("difference", "money", "Difference"))
        },
        new ReportDefinition
        {
            Code = "profit-loss", Name = "Profit & Loss", Group = "Finance",
            Description = "Revenue versus milk, purchase, expense and salary cost. rows = income/cost lines; totals = summary; extra.expenseByCategory.",
            Filters = new[] { "from", "to", "branchId" },
            Columns = Cols(C("label", "text", "Line"), C("amount", "money", "Amount"))
        }
    };
}
