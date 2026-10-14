using Dairy.Application.Common;
using Dairy.Application.DTOs.Inventory;
using Dairy.Application.Interfaces;
using Dairy.Application.Interfaces.Inventory;
using Dairy.Application.Requests.Inventory;
using Dairy.Domain.Constants;

namespace Dairy.Application.Services.Inventory;

public class InventoryItemService : IInventoryItemService
{
    private readonly IInventoryItemRepository _repo;
    private readonly IInventoryStockRepository _stock;
    private readonly IAuditService _audit;
    private readonly ICurrentUser _user;

    public InventoryItemService(IInventoryItemRepository repo, IInventoryStockRepository stock, IAuditService audit, ICurrentUser user)
    {
        _repo = repo;
        _stock = stock;
        _audit = audit;
        _user = user;
    }

    public Task<PagedResult<InventoryItemDto>> GetPagedAsync(InventoryItemQuery query, CancellationToken ct = default) =>
        _repo.GetPagedAsync(query, ct);

    public async Task<InventoryItemDetailDto> GetByIdAsync(long id, CancellationToken ct = default)
    {
        var item = await _repo.GetByIdAsync(id, ct) ?? throw new NotFoundException("Inventory item", id);

        var batches = await _stock.GetBatchesByItemAsync(id, ct);
        item.Batches = batches;
        item.TotalBatches = batches.Count;
        item.AvailableBatches = batches.Count(b => b.QuantityAvailable > 0 && b.Status == "Available" && !b.IsExpired);
        item.ExpiredBatches = batches.Count(b => b.QuantityAvailable > 0 && b.IsExpired);
        item.NearestExpiryDate = batches
            .Where(b => b.QuantityAvailable > 0 && !b.IsExpired && b.ExpiryDate.HasValue)
            .Select(b => b.ExpiryDate)
            .OrderBy(d => d)
            .FirstOrDefault();
        return item;
    }

    public async Task<InventoryItemDetailDto> CreateAsync(CreateInventoryItemRequest request, CancellationToken ct = default)
    {
        Normalize(request);
        await EnsureValidAsync(request, null, ct);

        var id = await _repo.CreateAsync(request, _user.UserId, ct);
        var created = await GetByIdAsync(id, ct);
        await _audit.LogAsync(AuditActions.Create, "InventoryItem", id, null, created, ct);
        return created;
    }

    public async Task<InventoryItemDetailDto> UpdateAsync(long id, UpdateInventoryItemRequest request, CancellationToken ct = default)
    {
        var existing = await GetByIdAsync(id, ct);

        Normalize(request);
        var isActive = request.IsActive ?? existing.IsActive;
        await EnsureValidAsync(request, id, ct);

        await _repo.UpdateAsync(id, request, isActive, _user.UserId, ct);
        var updated = await GetByIdAsync(id, ct);

        var action = existing.IsActive && !isActive ? AuditActions.Deactivate : AuditActions.Update;
        await _audit.LogAsync(action, "InventoryItem", id, existing, updated, ct);
        return updated;
    }

    private static void Normalize(CreateInventoryItemRequest r)
    {
        r.ItemCode = TextClean.Req(r.ItemCode);
        r.Name = TextClean.Req(r.Name);
        r.Description = TextClean.Opt(r.Description);
        r.StorageLocation = TextClean.Opt(r.StorageLocation);
        r.Barcode = TextClean.Opt(r.Barcode);
    }

    private async Task EnsureValidAsync(CreateInventoryItemRequest r, long? excludeId, CancellationToken ct)
    {
        if (await _repo.ItemCodeExistsAsync(r.ItemCode, excludeId, ct))
            throw new ConflictException($"An item with code '{r.ItemCode}' already exists.");
        if (!await _repo.CategoryExistsAsync(r.ItemCategoryId, ct))
            throw new RequestValidationException("itemCategoryId", "Item category not found.");
        if (!await _repo.UnitExistsAsync(r.UnitId, ct))
            throw new RequestValidationException("unitId", "Unit not found.");
        if (r.PreferredSupplierId.HasValue && !await _repo.SupplierExistsAsync(r.PreferredSupplierId.Value, ct))
            throw new RequestValidationException("preferredSupplierId", "Supplier not found.");
        if (r.MaxStockLevel.HasValue && r.MaxStockLevel.Value < r.ReorderLevel)
            throw new RequestValidationException("maxStockLevel", "Max stock level cannot be lower than the reorder level.");
    }
}
