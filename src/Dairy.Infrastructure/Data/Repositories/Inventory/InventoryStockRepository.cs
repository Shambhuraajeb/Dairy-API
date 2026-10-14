using Dairy.Application.Common;
using Dairy.Application.DTOs.Inventory;
using Dairy.Application.Interfaces;
using Dairy.Application.Interfaces.Inventory;
using Dairy.Application.Requests.Inventory;

namespace Dairy.Infrastructure.Data.Repositories.Inventory;

public class InventoryStockRepository : IInventoryStockRepository
{
    // ----- batches -----
    private static readonly Dictionary<string, string> BatchSort = new(StringComparer.OrdinalIgnoreCase)
    {
        ["id"] = "b.InventoryBatchId",
        ["itemName"] = "i.Name",
        ["batchNumber"] = "b.BatchNumber",
        ["expiryDate"] = "b.ExpiryDate",
        ["receivedDate"] = "b.ReceivedDate",
        ["quantityAvailable"] = "b.QuantityAvailable",
        ["status"] = "b.Status"
    };

    private const string ExpiryDaysExpr =
        "(SELECT CAST(ss.SettingValue AS UNSIGNED) FROM `SystemSetting` ss WHERE ss.SettingKey = 'inventory.expiryAlertDays' LIMIT 1)";

    private const string BatchColumns =
        "SELECT b.InventoryBatchId AS Id, b.InventoryItemId, i.ItemCode, i.Name AS ItemName, u.Name AS UnitName," +
        " b.BatchNumber, b.SupplierId, s.Name AS SupplierName, b.PurchaseDetailId, p.PurchaseNumber," +
        " b.ManufactureDate, b.ExpiryDate, b.ReceivedDate, b.QuantityReceived, b.QuantityAvailable, b.UnitCost," +
        " ROUND(b.QuantityAvailable * b.UnitCost, 2) AS StockValue, b.StorageLocation, b.Status," +
        " CASE WHEN b.ExpiryDate IS NULL THEN NULL ELSE DATEDIFF(b.ExpiryDate, CURDATE()) END AS DaysToExpiry," +
        " IF(b.ExpiryDate IS NOT NULL AND b.ExpiryDate < CURDATE(), 1, 0) AS IsExpired";

    private const string BatchFrom =
        " FROM `InventoryBatch` b" +
        " JOIN `InventoryItem` i ON i.InventoryItemId = b.InventoryItemId" +
        " JOIN `Unit` u ON u.UnitId = i.UnitId" +
        " LEFT JOIN `Supplier` s ON s.SupplierId = b.SupplierId" +
        " LEFT JOIN `PurchaseDetail` pd ON pd.PurchaseDetailId = b.PurchaseDetailId" +
        " LEFT JOIN `Purchase` p ON p.PurchaseId = pd.PurchaseId";

    private const string BatchWhere =
        " WHERE b.IsActive = 1" +
        " AND (@ItemId IS NULL OR b.InventoryItemId = @ItemId)" +
        " AND (@Search IS NULL OR b.BatchNumber LIKE @Search OR i.Name LIKE @Search OR i.ItemCode LIKE @Search)" +
        " AND (@Status IS NULL OR b.Status = @Status)" +
        " AND (@Available IS NULL OR @Available = 0 OR (b.QuantityAvailable > 0 AND b.Status = 'Available'" +
        "      AND (b.ExpiryDate IS NULL OR b.ExpiryDate >= CURDATE())))" +
        " AND (@Expired IS NULL OR @Expired = 0 OR (b.QuantityAvailable > 0 AND b.ExpiryDate IS NOT NULL AND b.ExpiryDate < CURDATE()))" +
        " AND (@Expiring IS NULL OR @Expiring = 0 OR (b.QuantityAvailable > 0 AND b.Status <> 'Depleted'" +
        "      AND b.ExpiryDate IS NOT NULL AND b.ExpiryDate >= CURDATE()" +
        "      AND b.ExpiryDate <= DATE_ADD(CURDATE(), INTERVAL COALESCE(@Days, " + ExpiryDaysExpr + ", 30) DAY)))";

    // ----- ledger -----
    private static readonly Dictionary<string, string> LedgerSort = new(StringComparer.OrdinalIgnoreCase)
    {
        ["id"] = "t.StockTransactionId",
        ["transactionDate"] = "t.TransactionDate",
        ["itemName"] = "i.Name",
        ["transactionType"] = "t.TransactionType",
        ["quantity"] = "t.Quantity"
    };

    private const string LedgerColumns =
        "SELECT t.StockTransactionId AS Id, t.InventoryItemId, i.ItemCode, i.Name AS ItemName, u.Name AS UnitName," +
        " t.InventoryBatchId, b.BatchNumber, t.TransactionType, t.Quantity, t.UnitCost, t.BalanceAfter," +
        " t.ReferenceType, t.ReferenceId, t.Remarks, t.TransactionDate, t.CreatedBy, cu.DisplayName AS CreatedByName";

    private const string LedgerFrom =
        " FROM `StockTransaction` t" +
        " JOIN `InventoryItem` i ON i.InventoryItemId = t.InventoryItemId" +
        " JOIN `Unit` u ON u.UnitId = i.UnitId" +
        " LEFT JOIN `InventoryBatch` b ON b.InventoryBatchId = t.InventoryBatchId" +
        " LEFT JOIN `User` cu ON cu.UserId = t.CreatedBy";

    private const string LedgerWhere =
        " WHERE t.IsActive = 1" +
        " AND (@ItemId IS NULL OR t.InventoryItemId = @ItemId)" +
        " AND (@BatchId IS NULL OR t.InventoryBatchId = @BatchId)" +
        " AND (@Type IS NULL OR t.TransactionType = @Type)" +
        " AND (@From IS NULL OR t.TransactionDate >= @From)" +
        " AND (@To IS NULL OR t.TransactionDate < DATE_ADD(DATE(@To), INTERVAL 1 DAY))" +
        " AND (@Search IS NULL OR i.Name LIKE @Search OR i.ItemCode LIKE @Search OR b.BatchNumber LIKE @Search OR t.Remarks LIKE @Search)";

    // ----- consumption -----
    private static readonly Dictionary<string, string> ConsumptionSort = new(StringComparer.OrdinalIgnoreCase)
    {
        ["id"] = "k.InventoryConsumptionId",
        ["consumptionDate"] = "k.ConsumptionDate",
        ["itemName"] = "i.Name",
        ["quantity"] = "k.Quantity"
    };

    private const string ConsumptionColumns =
        "SELECT k.InventoryConsumptionId AS Id, k.InventoryItemId, i.ItemCode, i.Name AS ItemName, u.Name AS UnitName," +
        " k.InventoryBatchId, b.BatchNumber, k.StockTransactionId, k.Quantity, st.UnitCost," +
        " ROUND(k.Quantity * COALESCE(st.UnitCost, 0), 2) AS CostValue, k.ConsumptionDate, k.Purpose, k.BranchId," +
        " k.ConsumedBy, cu.DisplayName AS ConsumedByName, k.ExpiredOverride, k.OverrideReason, k.CreatedDate";

    private const string ConsumptionFrom =
        " FROM `InventoryConsumption` k" +
        " JOIN `InventoryItem` i ON i.InventoryItemId = k.InventoryItemId" +
        " JOIN `Unit` u ON u.UnitId = i.UnitId" +
        " LEFT JOIN `InventoryBatch` b ON b.InventoryBatchId = k.InventoryBatchId" +
        " LEFT JOIN `StockTransaction` st ON st.StockTransactionId = k.StockTransactionId" +
        " LEFT JOIN `User` cu ON cu.UserId = k.ConsumedBy";

    private const string ConsumptionWhere =
        " WHERE k.IsActive = 1" +
        " AND (@ItemId IS NULL OR k.InventoryItemId = @ItemId)" +
        " AND (@BatchId IS NULL OR k.InventoryBatchId = @BatchId)" +
        " AND (@From IS NULL OR k.ConsumptionDate >= DATE(@From))" +
        " AND (@To IS NULL OR k.ConsumptionDate <= DATE(@To))" +
        " AND (@ExpiredOverride IS NULL OR k.ExpiredOverride = @ExpiredOverride)" +
        " AND (@Search IS NULL OR i.Name LIKE @Search OR i.ItemCode LIKE @Search OR k.Purpose LIKE @Search OR b.BatchNumber LIKE @Search)";

    private readonly IDbConnectionFactory _db;

    public InventoryStockRepository(IDbConnectionFactory db) => _db = db;

    public async Task<PagedResult<InventoryBatchDto>> GetBatchesPagedAsync(BatchQuery query, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        var param = new
        {
            Search = query.SearchPattern,
            query.ItemId,
            query.Status,
            query.Available,
            query.Expired,
            query.Expiring,
            query.Days
        };
        return await conn.QueryPagedAsync<InventoryBatchDto>(
            BatchColumns + BatchFrom + BatchWhere,
            "SELECT COUNT(*)" + BatchFrom + BatchWhere,
            param, query, BatchSort, "(b.ExpiryDate IS NULL) ASC, b.ExpiryDate ASC, b.InventoryBatchId DESC", ct);
    }

    public async Task<List<InventoryBatchDto>> GetBatchesByItemAsync(long itemId, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        return await conn.QueryListAsync<InventoryBatchDto>(
            BatchColumns + BatchFrom +
            " WHERE b.IsActive = 1 AND b.InventoryItemId = @ItemId" +
            " ORDER BY b.ReceivedDate DESC, b.InventoryBatchId DESC LIMIT 200",
            new { ItemId = itemId }, ct);
    }

    public async Task<PagedResult<StockTransactionDto>> GetLedgerPagedAsync(StockTransactionQuery query, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        var param = new
        {
            Search = query.SearchPattern,
            query.ItemId,
            query.BatchId,
            query.Type,
            query.From,
            query.To
        };
        return await conn.QueryPagedAsync<StockTransactionDto>(
            LedgerColumns + LedgerFrom + LedgerWhere,
            "SELECT COUNT(*)" + LedgerFrom + LedgerWhere,
            param, query, LedgerSort, "t.TransactionDate DESC, t.StockTransactionId DESC", ct);
    }

    public async Task<PagedResult<InventoryConsumptionDto>> GetConsumptionsPagedAsync(ConsumptionQuery query, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        var param = new
        {
            Search = query.SearchPattern,
            query.ItemId,
            query.BatchId,
            query.From,
            query.To,
            query.ExpiredOverride
        };
        return await conn.QueryPagedAsync<InventoryConsumptionDto>(
            ConsumptionColumns + ConsumptionFrom + ConsumptionWhere,
            "SELECT COUNT(*)" + ConsumptionFrom + ConsumptionWhere,
            param, query, ConsumptionSort, "k.ConsumptionDate DESC, k.InventoryConsumptionId DESC", ct);
    }

    public async Task<bool> ItemExistsAsync(long itemId, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        return await conn.ScalarAsync<int>("SELECT COUNT(1) FROM `InventoryItem` WHERE InventoryItemId = @Id", new { Id = itemId }, ct) > 0;
    }

    public async Task<ConsumptionResultDto> RecordConsumptionAsync(RecordConsumptionRequest request, bool expiredOverride, long userId, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        var row = await conn.QuerySpFirstOrDefaultAsync<ConsumptionResultDto>("sp_RecordInventoryConsumption", new
        {
            p_InventoryItemId = request.InventoryItemId,
            p_InventoryBatchId = request.InventoryBatchId,
            p_Quantity = request.Quantity,
            p_ConsumptionDate = request.ConsumptionDate,
            p_Purpose = request.Purpose,
            p_ExpiredOverride = expiredOverride ? 1 : 0,
            p_UserId = userId
        }, ct);
        return row ?? throw new InvalidOperationException("sp_RecordInventoryConsumption did not return a row.");
    }

    public async Task<StockAdjustmentResultDto> AdjustStockAsync(AdjustStockRequest request, long userId, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        var row = await conn.QuerySpFirstOrDefaultAsync<StockAdjustmentResultDto>("sp_AdjustStock", new
        {
            p_InventoryItemId = request.InventoryItemId,
            p_InventoryBatchId = request.InventoryBatchId,
            p_QuantityDelta = request.QuantityDelta,
            p_Reason = request.Reason,
            p_UserId = userId
        }, ct);
        return row ?? throw new InvalidOperationException("sp_AdjustStock did not return a row.");
    }

    public async Task<List<LowStockAlertDto>> GetLowStockAsync(CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        return await conn.QuerySpAsync<LowStockAlertDto>("sp_GetLowStockItems", null, ct);
    }

    public async Task<List<ExpiringStockAlertDto>> GetExpiringAsync(int? days, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        return await conn.QuerySpAsync<ExpiringStockAlertDto>("sp_GetExpiringInventory", new { p_Days = days }, ct);
    }

    public async Task<InventorySummaryDto> GetSummaryAsync(CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        const string expiringCond =
            " IsActive = 1 AND QuantityAvailable > 0 AND ExpiryDate IS NOT NULL AND Status <> 'Depleted'";
        var sql =
            "SELECT" +
            " (SELECT COUNT(*) FROM `InventoryItem`) AS TotalItems," +
            " (SELECT COUNT(*) FROM `InventoryItem` WHERE IsActive = 1) AS ActiveItems," +
            " (SELECT COUNT(*) FROM `ItemCategory` WHERE IsActive = 1) AS CategoryCount," +
            " (SELECT COUNT(*) FROM `Supplier` WHERE IsActive = 1) AS SupplierCount," +
            " (SELECT COALESCE(ROUND(SUM(GREATEST(CurrentStock, 0) * UnitCost), 2), 0) FROM `InventoryItem` WHERE IsActive = 1) AS StockValue," +
            " (SELECT COUNT(*) FROM `InventoryItem` WHERE IsActive = 1 AND ReorderLevel > 0 AND CurrentStock <= ReorderLevel) AS LowStockCount," +
            " (SELECT COUNT(*) FROM `InventoryItem` WHERE IsActive = 1 AND CurrentStock <= 0) AS OutOfStockCount," +
            " (SELECT COUNT(*) FROM `InventoryBatch` WHERE" + expiringCond +
            "   AND ExpiryDate >= CURDATE() AND ExpiryDate <= DATE_ADD(CURDATE(), INTERVAL COALESCE(" + ExpiryDaysExpr + ", 30) DAY)) AS ExpiringBatchCount," +
            " (SELECT COUNT(*) FROM `InventoryBatch` WHERE" + expiringCond + " AND ExpiryDate < CURDATE()) AS ExpiredBatchCount," +
            " (SELECT COALESCE(ROUND(SUM(QuantityAvailable * UnitCost), 2), 0) FROM `InventoryBatch` WHERE" + expiringCond +
            "   AND ExpiryDate < CURDATE()) AS ExpiredStockValue," +
            " (SELECT COUNT(*) FROM `Purchase` WHERE IsActive = 1 AND Status IN ('Draft', 'Approved')) AS OpenPurchaseCount," +
            " (SELECT COUNT(*) FROM `Purchase` WHERE IsActive = 1 AND Status = 'Draft') AS PendingApprovalCount";
        var row = await conn.QueryFirstOrNullAsync<InventorySummaryDto>(sql, null, ct);
        return row ?? new InventorySummaryDto();
    }
}
