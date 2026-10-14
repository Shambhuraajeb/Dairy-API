using Dairy.Application.Common;
using Dairy.Application.DTOs.Admin;
using Dairy.Application.Interfaces;
using Dairy.Application.Interfaces.Admin;
using Dairy.Application.Requests.Admin;
using Dairy.Domain.Constants;

namespace Dairy.Application.Services.Admin;

public class OrganizationAdminService : IOrganizationAdminService
{
    private readonly IOrganizationAdminRepository _repo;
    private readonly IAuditService _audit;
    private readonly ICurrentUser _user;

    public OrganizationAdminService(IOrganizationAdminRepository repo, IAuditService audit, ICurrentUser user)
    {
        _repo = repo;
        _audit = audit;
        _user = user;
    }

    // ---------- dairy ----------
    public Task<PagedResult<DairyDto>> GetDairiesAsync(OrgQuery query, CancellationToken ct = default) =>
        _repo.GetDairiesAsync(query, ct);

    public async Task<DairyDto> GetDairyAsync(long id, CancellationToken ct = default) =>
        await _repo.GetDairyAsync(id, ct) ?? throw new NotFoundException("Dairy", id);

    public async Task<DairyDto> CreateDairyAsync(SaveDairyRequest request, CancellationToken ct = default)
    {
        Normalize(request);
        if (await _repo.DairyCodeExistsAsync(request.Code, null, ct))
            throw new ConflictException($"A dairy with code '{request.Code}' already exists.");

        var id = await _repo.CreateDairyAsync(request, _user.UserId, ct);
        var created = await GetDairyAsync(id, ct);
        await _audit.LogAsync(AuditActions.Create, "Dairy", id, null, created, ct);
        return created;
    }

    public async Task<DairyDto> UpdateDairyAsync(long id, SaveDairyRequest request, CancellationToken ct = default)
    {
        var existing = await GetDairyAsync(id, ct);
        Normalize(request);
        if (await _repo.DairyCodeExistsAsync(request.Code, id, ct))
            throw new ConflictException($"A dairy with code '{request.Code}' already exists.");

        await _repo.UpdateDairyAsync(id, request, request.IsActive ?? existing.IsActive, _user.UserId, ct);
        var updated = await GetDairyAsync(id, ct);
        await _audit.LogAsync(ActionFor(existing.IsActive, updated.IsActive), "Dairy", id, existing, updated, ct);
        return updated;
    }

    // ---------- branches ----------
    public Task<PagedResult<BranchDto>> GetBranchesAsync(BranchAdminQuery query, CancellationToken ct = default) =>
        _repo.GetBranchesAsync(query, ct);

    public async Task<BranchDto> GetBranchAsync(long id, CancellationToken ct = default) =>
        await _repo.GetBranchAsync(id, ct) ?? throw new NotFoundException("Branch", id);

    public async Task<BranchDto> CreateBranchAsync(SaveBranchRequest request, CancellationToken ct = default)
    {
        Normalize(request);
        if (!await _repo.DairyExistsAsync(request.DairyId, ct))
            throw new RequestValidationException("dairyId", "Dairy not found.");
        if (await _repo.BranchCodeExistsAsync(request.Code, null, ct))
            throw new ConflictException($"A branch with code '{request.Code}' already exists.");

        var id = await _repo.CreateBranchAsync(request, _user.UserId, ct);
        var created = await GetBranchAsync(id, ct);
        await _audit.LogAsync(AuditActions.Create, "Branch", id, null, created, ct);
        return created;
    }

    public async Task<BranchDto> UpdateBranchAsync(long id, SaveBranchRequest request, CancellationToken ct = default)
    {
        var existing = await GetBranchAsync(id, ct);
        Normalize(request);
        if (!await _repo.DairyExistsAsync(request.DairyId, ct))
            throw new RequestValidationException("dairyId", "Dairy not found.");
        if (await _repo.BranchCodeExistsAsync(request.Code, id, ct))
            throw new ConflictException($"A branch with code '{request.Code}' already exists.");

        var isActive = request.IsActive ?? existing.IsActive;
        if (existing.IsActive && !isActive)
        {
            var centers = await _repo.CountActiveCentersAsync(id, ct);
            if (centers > 0)
                throw new BusinessRuleException($"Cannot deactivate a branch that still has {centers} active collection center(s).");
        }

        await _repo.UpdateBranchAsync(id, request, isActive, _user.UserId, ct);
        var updated = await GetBranchAsync(id, ct);
        await _audit.LogAsync(ActionFor(existing.IsActive, updated.IsActive), "Branch", id, existing, updated, ct);
        return updated;
    }

    // ---------- collection centers ----------
    public Task<PagedResult<CollectionCenterDto>> GetCentersAsync(CollectionCenterAdminQuery query, CancellationToken ct = default) =>
        _repo.GetCentersAsync(query, ct);

    public async Task<CollectionCenterDto> GetCenterAsync(long id, CancellationToken ct = default) =>
        await _repo.GetCenterAsync(id, ct) ?? throw new NotFoundException("Collection center", id);

    public async Task<CollectionCenterDto> CreateCenterAsync(SaveCollectionCenterRequest request, CancellationToken ct = default)
    {
        Normalize(request);
        await EnsureCenterReferencesAsync(request, ct);
        if (await _repo.CenterCodeExistsAsync(request.Code, null, ct))
            throw new ConflictException($"A collection center with code '{request.Code}' already exists.");

        var id = await _repo.CreateCenterAsync(request, _user.UserId, ct);
        var created = await GetCenterAsync(id, ct);
        await _audit.LogAsync(AuditActions.Create, "CollectionCenter", id, null, created, ct);
        return created;
    }

    public async Task<CollectionCenterDto> UpdateCenterAsync(long id, SaveCollectionCenterRequest request, CancellationToken ct = default)
    {
        var existing = await GetCenterAsync(id, ct);
        Normalize(request);
        await EnsureCenterReferencesAsync(request, ct);
        if (await _repo.CenterCodeExistsAsync(request.Code, id, ct))
            throw new ConflictException($"A collection center with code '{request.Code}' already exists.");

        await _repo.UpdateCenterAsync(id, request, request.IsActive ?? existing.IsActive, _user.UserId, ct);
        var updated = await GetCenterAsync(id, ct);
        await _audit.LogAsync(ActionFor(existing.IsActive, updated.IsActive), "CollectionCenter", id, existing, updated, ct);
        return updated;
    }

    // ---------- helpers ----------
    private async Task EnsureCenterReferencesAsync(SaveCollectionCenterRequest r, CancellationToken ct)
    {
        if (!await _repo.BranchExistsAsync(r.BranchId, ct))
            throw new RequestValidationException("branchId", "Branch not found.");
        if (r.SupervisorEmployeeId.HasValue && !await _repo.EmployeeExistsAsync(r.SupervisorEmployeeId.Value, ct))
            throw new RequestValidationException("supervisorEmployeeId", "Employee not found.");
    }

    private static string ActionFor(bool wasActive, bool isActive) =>
        wasActive && !isActive ? AuditActions.Deactivate
        : !wasActive && isActive ? AuditActions.Activate
        : AuditActions.Update;

    private static void Normalize(SaveDairyRequest r)
    {
        r.Code = r.Code.Trim().ToUpperInvariant();
        r.Name = r.Name.Trim();
        r.OwnerName = B5Text.Clean(r.OwnerName);
        r.Mobile = B5Text.Clean(r.Mobile);
        r.Email = B5Text.Clean(r.Email);
        r.AddressLine = B5Text.Clean(r.AddressLine);
        r.City = B5Text.Clean(r.City);
        r.State = B5Text.Clean(r.State);
        r.Pincode = B5Text.Clean(r.Pincode);
        r.GstNumber = B5Text.Clean(r.GstNumber)?.ToUpperInvariant();
        r.LicenseNo = B5Text.Clean(r.LicenseNo);
        r.RegistrationNo = B5Text.Clean(r.RegistrationNo);
    }

    private static void Normalize(SaveBranchRequest r)
    {
        r.Code = r.Code.Trim().ToUpperInvariant();
        r.Name = r.Name.Trim();
        r.AddressLine = B5Text.Clean(r.AddressLine);
        r.City = B5Text.Clean(r.City);
        r.State = B5Text.Clean(r.State);
        r.Pincode = B5Text.Clean(r.Pincode);
        r.Mobile = B5Text.Clean(r.Mobile);
        r.Email = B5Text.Clean(r.Email);
        r.ManagerName = B5Text.Clean(r.ManagerName);
    }

    private static void Normalize(SaveCollectionCenterRequest r)
    {
        r.Code = r.Code.Trim().ToUpperInvariant();
        r.Name = r.Name.Trim();
        r.AddressLine = B5Text.Clean(r.AddressLine);
        r.City = B5Text.Clean(r.City);
        r.Pincode = B5Text.Clean(r.Pincode);
        r.Mobile = B5Text.Clean(r.Mobile);
        r.ContactPerson = B5Text.Clean(r.ContactPerson);
    }
}
