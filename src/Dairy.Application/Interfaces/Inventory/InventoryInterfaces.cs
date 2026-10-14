using Dairy.Application.Common;
using Dairy.Application.DTOs.Inventory;
using Dairy.Application.Requests.Inventory;

namespace Dairy.Application.Interfaces.Inventory;

// ---------- item categories ----------

public interface IItemCategoryRepository
{
    Task<PagedResult<ItemCategoryDto>> GetPagedAsync(ItemCategoryQuery query, CancellationToken ct = default);
    Task<ItemCategoryDto?> GetByIdAsync(long id, CancellationToken ct = default);
    Task<bool> ExistsAsync(string code, string name, long? excludeId, CancellationToken ct = default);
    Task<long> CreateAsync(CreateItemCategoryRequest request, long userId, CancellationToken ct = default);
    Task UpdateAsync(long id, UpdateItemCategoryRequest request, bool isActive, long userId, CancellationToken ct = default);
}

public interface IItemCategoryService
{
    Task<PagedResult<ItemCategoryDto>> GetPagedAsync(ItemCategoryQuery query, CancellationToken ct = default);
    Task<ItemCategoryDto> GetByIdAsync(long id, CancellationToken ct = default);
    Task<ItemCategoryDto> CreateAsync(CreateItemCategoryRequest request, CancellationToken ct = default);
    Task<ItemCategoryDto> UpdateAsync(long id, UpdateItemCategoryRequest request, CancellationToken ct = default);
}

// ---------- items ----------

public interface IInventoryItemRepository
{
    Task<PagedResult<InventoryItemDto>> GetPagedAsync(InventoryItemQuery query, CancellationToken ct = default);
    Task<InventoryItemDetailDto?> GetByIdAsync(long id, CancellationToken ct = default);
    Task<bool> ItemCodeExistsAsync(string code, long? excludeId, CancellationToken ct = default);
    Task<bool> CategoryExistsAsync(long id, CancellationToken ct = default);
    Task<bool> UnitExistsAsync(long id, CancellationToken ct = default);
    Task<bool> SupplierExistsAsync(long id, CancellationToken ct = default);
    Task<long> CreateAsync(CreateInventoryItemRequest request, long userId, CancellationToken ct = default);
    Task UpdateAsync(long id, UpdateInventoryItemRequest request, bool isActive, long userId, CancellationToken ct = default);
}

public interface IInventoryItemService
{
    Task<PagedResult<InventoryItemDto>> GetPagedAsync(InventoryItemQuery query, CancellationToken ct = default);
    Task<InventoryItemDetailDto> GetByIdAsync(long id, CancellationToken ct = default);
    Task<InventoryItemDetailDto> CreateAsync(CreateInventoryItemRequest request, CancellationToken ct = default);
    Task<InventoryItemDetailDto> UpdateAsync(long id, UpdateInventoryItemRequest request, CancellationToken ct = default);
}

// ---------- batches, ledger, consumption, adjustments, alerts, summary ----------

public interface IInventoryStockRepository
{
    Task<PagedResult<InventoryBatchDto>> GetBatchesPagedAsync(BatchQuery query, CancellationToken ct = default);
    Task<List<InventoryBatchDto>> GetBatchesByItemAsync(long itemId, CancellationToken ct = default);
    Task<PagedResult<StockTransactionDto>> GetLedgerPagedAsync(StockTransactionQuery query, CancellationToken ct = default);
    Task<PagedResult<InventoryConsumptionDto>> GetConsumptionsPagedAsync(ConsumptionQuery query, CancellationToken ct = default);
    Task<bool> ItemExistsAsync(long itemId, CancellationToken ct = default);
    Task<ConsumptionResultDto> RecordConsumptionAsync(RecordConsumptionRequest request, bool expiredOverride, long userId, CancellationToken ct = default);
    Task<StockAdjustmentResultDto> AdjustStockAsync(AdjustStockRequest request, long userId, CancellationToken ct = default);
    Task<List<LowStockAlertDto>> GetLowStockAsync(CancellationToken ct = default);
    Task<List<ExpiringStockAlertDto>> GetExpiringAsync(int? days, CancellationToken ct = default);
    Task<InventorySummaryDto> GetSummaryAsync(CancellationToken ct = default);
}

public interface IInventoryStockService
{
    Task<PagedResult<InventoryBatchDto>> GetBatchesPagedAsync(BatchQuery query, CancellationToken ct = default);
    Task<PagedResult<StockTransactionDto>> GetLedgerPagedAsync(StockTransactionQuery query, CancellationToken ct = default);
    Task<PagedResult<StockTransactionDto>> GetAdjustmentsPagedAsync(StockTransactionQuery query, CancellationToken ct = default);
    Task<PagedResult<InventoryConsumptionDto>> GetConsumptionsPagedAsync(ConsumptionQuery query, CancellationToken ct = default);
    Task<ConsumptionResultDto> RecordConsumptionAsync(RecordConsumptionRequest request, CancellationToken ct = default);
    Task<StockAdjustmentResultDto> AdjustStockAsync(AdjustStockRequest request, CancellationToken ct = default);
    Task<List<LowStockAlertDto>> GetLowStockAsync(CancellationToken ct = default);
    Task<List<ExpiringStockAlertDto>> GetExpiringAsync(int? days, CancellationToken ct = default);
    Task<InventorySummaryDto> GetSummaryAsync(CancellationToken ct = default);
}

// ---------- purchases ----------

public interface IPurchaseRepository
{
    Task<PagedResult<PurchaseDto>> GetPagedAsync(PurchaseQuery query, CancellationToken ct = default);
    Task<PurchaseDetailDto?> GetByIdAsync(long id, CancellationToken ct = default);
    Task<PurchaseCreatedDto> CreateAsync(long supplierId, DateTime? purchaseDate, string? remarks, string itemsJson, long userId, CancellationToken ct = default);
    Task ApproveAsync(long id, long userId, CancellationToken ct = default);
    Task<PurchaseReceivedDto> ReceiveAsync(long id, DateTime? receivedDate, long userId, CancellationToken ct = default);

    /// <summary>Cancels a Draft/Approved purchase with nothing received. Returns affected rows (0 = not cancellable).</summary>
    Task<int> CancelAsync(long id, string reason, long userId, CancellationToken ct = default);
}

public interface IPurchaseService
{
    Task<PagedResult<PurchaseDto>> GetPagedAsync(PurchaseQuery query, CancellationToken ct = default);
    Task<PurchaseDetailDto> GetByIdAsync(long id, CancellationToken ct = default);
    Task<PurchaseCreatedDto> CreateAsync(CreatePurchaseRequest request, CancellationToken ct = default);
    Task<PurchaseDetailDto> ApproveAsync(long id, CancellationToken ct = default);
    Task<PurchaseDetailDto> ReceiveAsync(long id, ReceivePurchaseRequest? request, CancellationToken ct = default);
    Task<PurchaseDetailDto> CancelAsync(long id, CancelPurchaseRequest request, CancellationToken ct = default);
}
