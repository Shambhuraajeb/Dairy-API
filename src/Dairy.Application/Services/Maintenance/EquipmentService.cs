using Dairy.Application.Common;
using Dairy.Application.DTOs.Maintenance;
using Dairy.Application.Interfaces;
using Dairy.Application.Interfaces.Maintenance;
using Dairy.Application.Requests.Maintenance;
using Dairy.Domain.Constants;

namespace Dairy.Application.Services.Maintenance;

public class EquipmentService : IEquipmentService
{
    private readonly IEquipmentRepository _repo;
    private readonly IAuditService _audit;
    private readonly ICurrentUser _user;

    public EquipmentService(IEquipmentRepository repo, IAuditService audit, ICurrentUser user)
    {
        _repo = repo;
        _audit = audit;
        _user = user;
    }

    public Task<PagedResult<EquipmentDto>> GetPagedAsync(EquipmentQuery query, CancellationToken ct = default) =>
        _repo.GetPagedAsync(query, ct);

    public async Task<EquipmentDto> GetByIdAsync(long id, CancellationToken ct = default) =>
        await _repo.GetByIdAsync(id, ct) ?? throw new NotFoundException("Equipment", id);

    public async Task<EquipmentDto> CreateAsync(CreateEquipmentRequest request, CancellationToken ct = default)
    {
        Normalize(request);
        await EnsureValidAsync(request, null, ct);

        var id = await _repo.CreateAsync(request, _user.UserId, ct);
        var created = await GetByIdAsync(id, ct);
        await _audit.LogAsync(AuditActions.Create, "Equipment", id, null, created, ct);
        return created;
    }

    public async Task<EquipmentDto> UpdateAsync(long id, UpdateEquipmentRequest request, CancellationToken ct = default)
    {
        var existing = await GetByIdAsync(id, ct);

        Normalize(request);
        var status = string.IsNullOrWhiteSpace(request.Status) ? existing.Status : request.Status!;
        var isActive = request.IsActive ?? existing.IsActive;
        await EnsureValidAsync(request, id, ct);

        await _repo.UpdateAsync(id, request, status, isActive, _user.UserId, ct);
        var updated = await GetByIdAsync(id, ct);

        var action = existing.IsActive && !isActive ? AuditActions.Deactivate : AuditActions.Update;
        await _audit.LogAsync(action, "Equipment", id, existing, updated, ct);
        return updated;
    }

    private static void Normalize(CreateEquipmentRequest r)
    {
        r.EquipmentCode = TextClean.Req(r.EquipmentCode);
        r.Name = TextClean.Req(r.Name);
        r.Category = TextClean.Opt(r.Category);
        r.Make = TextClean.Opt(r.Make);
        r.Model = TextClean.Opt(r.Model);
        r.SerialNumber = TextClean.Opt(r.SerialNumber);
        r.Location = TextClean.Opt(r.Location);
        r.VendorName = TextClean.Opt(r.VendorName);
        r.Remarks = TextClean.Opt(r.Remarks);
    }

    private async Task EnsureValidAsync(CreateEquipmentRequest r, long? excludeId, CancellationToken ct)
    {
        if (await _repo.CodeExistsAsync(r.EquipmentCode, excludeId, ct))
            throw new ConflictException($"Equipment with code '{r.EquipmentCode}' already exists.");
        if (r.BranchId.HasValue && !await _repo.BranchExistsAsync(r.BranchId.Value, ct))
            throw new RequestValidationException("branchId", "Branch not found.");
        if (r.CollectionCenterId.HasValue && !await _repo.CollectionCenterExistsAsync(r.CollectionCenterId.Value, ct))
            throw new RequestValidationException("collectionCenterId", "Collection center not found.");
    }
}
