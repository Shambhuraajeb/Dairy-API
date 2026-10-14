using System.Text.Json;
using Dairy.Application.Common;
using Dairy.Application.DTOs.Inventory;
using Dairy.Application.Interfaces;
using Dairy.Application.Interfaces.Inventory;
using Dairy.Application.Requests.Inventory;
using Dairy.Domain.Constants;

namespace Dairy.Application.Services.Inventory;

/// <summary>Purchase workflow: Draft -> Approved -> Received (or Cancelled). Procedures do not audit, so this service does.</summary>
public class PurchaseService : IPurchaseService
{
    private readonly IPurchaseRepository _repo;
    private readonly IAuditService _audit;
    private readonly ICurrentUser _user;

    public PurchaseService(IPurchaseRepository repo, IAuditService audit, ICurrentUser user)
    {
        _repo = repo;
        _audit = audit;
        _user = user;
    }

    public Task<PagedResult<PurchaseDto>> GetPagedAsync(PurchaseQuery query, CancellationToken ct = default) =>
        _repo.GetPagedAsync(query, ct);

    public async Task<PurchaseDetailDto> GetByIdAsync(long id, CancellationToken ct = default) =>
        await _repo.GetByIdAsync(id, ct) ?? throw new NotFoundException("Purchase", id);

    public async Task<PurchaseCreatedDto> CreateAsync(CreatePurchaseRequest request, CancellationToken ct = default)
    {
        var lines = request.Items.Select(i => new
        {
            inventoryItemId = i.InventoryItemId,
            quantity = i.Quantity,
            unitCost = i.UnitCost,
            batchNumber = TextClean.Opt(i.BatchNumber),
            manufactureDate = i.ManufactureDate?.ToString("yyyy-MM-dd"),
            expiryDate = i.ExpiryDate?.ToString("yyyy-MM-dd")
        });
        var json = JsonSerializer.Serialize(lines);

        var created = await _repo.CreateAsync(request.SupplierId, request.PurchaseDate, TextClean.Opt(request.Remarks), json, _user.UserId, ct);

        await _audit.LogAsync(AuditActions.Create, "Purchase", created.Id, null,
            new { created.Number, request.SupplierId, request.PurchaseDate, created.TotalAmount, LineCount = request.Items.Count }, ct);
        return created;
    }

    public async Task<PurchaseDetailDto> ApproveAsync(long id, CancellationToken ct = default)
    {
        var before = await GetByIdAsync(id, ct);
        await _repo.ApproveAsync(id, _user.UserId, ct);
        var after = await GetByIdAsync(id, ct);

        await _audit.LogAsync(AuditActions.Approve, "Purchase", id,
            new { before.Status }, new { after.Status, after.PurchaseNumber, after.TotalAmount }, ct);
        return after;
    }

    public async Task<PurchaseDetailDto> ReceiveAsync(long id, ReceivePurchaseRequest? request, CancellationToken ct = default)
    {
        var before = await GetByIdAsync(id, ct);
        var result = await _repo.ReceiveAsync(id, request?.ReceivedDate, _user.UserId, ct);
        var after = await GetByIdAsync(id, ct);

        await _audit.LogAsync("Receive", "Purchase", id,
            new { before.Status },
            new { after.Status, after.PurchaseNumber, result.LinesReceived, ReceivedDate = request?.ReceivedDate }, ct);
        return after;
    }

    public async Task<PurchaseDetailDto> CancelAsync(long id, CancelPurchaseRequest request, CancellationToken ct = default)
    {
        var before = await GetByIdAsync(id, ct);
        if (before.Status != PurchaseStatus.Draft && before.Status != PurchaseStatus.Approved)
            throw new BusinessRuleException("Only a Draft or Approved purchase can be cancelled.");

        var reason = TextClean.Req(request.Reason);
        var rows = await _repo.CancelAsync(id, reason, _user.UserId, ct);
        if (rows == 0)
            throw new BusinessRuleException("This purchase can no longer be cancelled (it has been received or changed).");

        var after = await GetByIdAsync(id, ct);
        await _audit.LogAsync("Cancel", "Purchase", id,
            new { before.Status }, new { after.Status, Reason = reason, after.PurchaseNumber }, ct);
        return after;
    }
}
