using Dairy.Application.Common;
using Dairy.Application.DTOs.Inventory;
using Dairy.Application.Interfaces;
using Dairy.Application.Interfaces.Inventory;
using Dairy.Application.Requests.Inventory;
using Dairy.Domain.Constants;

namespace Dairy.Application.Services.Inventory;

public class InventoryStockService : IInventoryStockService
{
    private readonly IInventoryStockRepository _repo;
    private readonly IAuditService _audit;
    private readonly ICurrentUser _user;

    public InventoryStockService(IInventoryStockRepository repo, IAuditService audit, ICurrentUser user)
    {
        _repo = repo;
        _audit = audit;
        _user = user;
    }

    public Task<PagedResult<InventoryBatchDto>> GetBatchesPagedAsync(BatchQuery query, CancellationToken ct = default) =>
        _repo.GetBatchesPagedAsync(query, ct);

    public Task<PagedResult<StockTransactionDto>> GetLedgerPagedAsync(StockTransactionQuery query, CancellationToken ct = default) =>
        _repo.GetLedgerPagedAsync(query, ct);

    public Task<PagedResult<StockTransactionDto>> GetAdjustmentsPagedAsync(StockTransactionQuery query, CancellationToken ct = default)
    {
        query.Type = "Adjustment";
        return _repo.GetLedgerPagedAsync(query, ct);
    }

    public Task<PagedResult<InventoryConsumptionDto>> GetConsumptionsPagedAsync(ConsumptionQuery query, CancellationToken ct = default) =>
        _repo.GetConsumptionsPagedAsync(query, ct);

    public async Task<ConsumptionResultDto> RecordConsumptionAsync(RecordConsumptionRequest request, CancellationToken ct = default)
    {
        request.Purpose = TextClean.Opt(request.Purpose);

        if (request.ExpiredOverride && !_user.HasPermission(Permissions.InventoryAdjust))
            throw new BusinessRuleException("Only users with the inventory.adjust permission can consume expired stock with an override.");
        if (!await _repo.ItemExistsAsync(request.InventoryItemId, ct))
            throw new NotFoundException("Inventory item", request.InventoryItemId);

        var result = await _repo.RecordConsumptionAsync(request, request.ExpiredOverride, _user.UserId, ct);

        await _audit.LogAsync(AuditActions.Create, "InventoryConsumption", result.Id,
            null,
            new
            {
                request.InventoryItemId,
                request.InventoryBatchId,
                request.Quantity,
                request.ConsumptionDate,
                request.Purpose,
                ExpiredOverride = request.ExpiredOverride,
                result.BatchesUsed,
                result.CurrentStock
            }, ct);
        return result;
    }

    /// <summary>sp_AdjustStock writes its own AuditLog row (StockAdjustment); not audited again here.</summary>
    public Task<StockAdjustmentResultDto> AdjustStockAsync(AdjustStockRequest request, CancellationToken ct = default)
    {
        request.Reason = TextClean.Req(request.Reason);
        if (request.Reason.Length == 0)
            throw new RequestValidationException("reason", "A reason is required for stock adjustments.");
        return _repo.AdjustStockAsync(request, _user.UserId, ct);
    }

    public Task<List<LowStockAlertDto>> GetLowStockAsync(CancellationToken ct = default) => _repo.GetLowStockAsync(ct);

    public Task<List<ExpiringStockAlertDto>> GetExpiringAsync(int? days, CancellationToken ct = default)
    {
        if (days is < 0)
            throw new RequestValidationException("days", "Days cannot be negative.");
        return _repo.GetExpiringAsync(days, ct);
    }

    public Task<InventorySummaryDto> GetSummaryAsync(CancellationToken ct = default) => _repo.GetSummaryAsync(ct);
}
