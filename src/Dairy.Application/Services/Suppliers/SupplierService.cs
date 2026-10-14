using Dairy.Application.Common;
using Dairy.Application.DTOs.Suppliers;
using Dairy.Application.Interfaces;
using Dairy.Application.Interfaces.Suppliers;
using Dairy.Application.Requests.Suppliers;
using Dairy.Domain.Constants;

namespace Dairy.Application.Services.Suppliers;

public class SupplierService : ISupplierService
{
    private readonly ISupplierRepository _repo;
    private readonly IAuditService _audit;
    private readonly ICurrentUser _user;

    public SupplierService(ISupplierRepository repo, IAuditService audit, ICurrentUser user)
    {
        _repo = repo;
        _audit = audit;
        _user = user;
    }

    public Task<PagedResult<SupplierDto>> GetPagedAsync(SupplierQuery query, CancellationToken ct = default) =>
        _repo.GetPagedAsync(query, ct);

    public async Task<SupplierDto> GetByIdAsync(long id, CancellationToken ct = default) =>
        await _repo.GetByIdAsync(id, ct) ?? throw new NotFoundException("Supplier", id);

    public async Task<SupplierDto> CreateAsync(CreateSupplierRequest request, CancellationToken ct = default)
    {
        Normalize(request);
        if (await _repo.CodeExistsAsync(request.SupplierCode, null, ct))
            throw new ConflictException($"A supplier with code '{request.SupplierCode}' already exists.");

        var id = await _repo.CreateAsync(request, _user.UserId, ct);
        var created = await GetByIdAsync(id, ct);
        await _audit.LogAsync(AuditActions.Create, "Supplier", id, null, created, ct);
        return created;
    }

    public async Task<SupplierDto> UpdateAsync(long id, UpdateSupplierRequest request, CancellationToken ct = default)
    {
        var existing = await GetByIdAsync(id, ct);

        Normalize(request);
        var isActive = request.IsActive ?? existing.IsActive;
        if (await _repo.CodeExistsAsync(request.SupplierCode, id, ct))
            throw new ConflictException($"A supplier with code '{request.SupplierCode}' already exists.");

        await _repo.UpdateAsync(id, request, isActive, _user.UserId, ct);
        var updated = await GetByIdAsync(id, ct);

        var action = existing.IsActive && !isActive ? AuditActions.Deactivate : AuditActions.Update;
        await _audit.LogAsync(action, "Supplier", id, existing, updated, ct);
        return updated;
    }

    private static void Normalize(CreateSupplierRequest r)
    {
        r.SupplierCode = TextClean.Req(r.SupplierCode);
        r.Name = TextClean.Req(r.Name);
        r.ContactPerson = TextClean.Opt(r.ContactPerson);
        r.Mobile = TextClean.Opt(r.Mobile);
        r.Email = TextClean.Opt(r.Email);
        r.AddressLine = TextClean.Opt(r.AddressLine);
        r.City = TextClean.Opt(r.City);
        r.State = TextClean.Opt(r.State);
        r.Pincode = TextClean.Opt(r.Pincode);
        r.GstNumber = TextClean.Opt(r.GstNumber)?.ToUpperInvariant();
        r.PanNo = TextClean.Opt(r.PanNo)?.ToUpperInvariant();
    }
}
