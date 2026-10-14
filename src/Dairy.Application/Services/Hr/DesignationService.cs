using Dairy.Application.Common;
using Dairy.Application.DTOs.Hr;
using Dairy.Application.Interfaces;
using Dairy.Application.Interfaces.Hr;
using Dairy.Application.Requests.Hr;
using Dairy.Domain.Constants;

namespace Dairy.Application.Services.Hr;

public class DesignationService : IDesignationService
{
    private readonly IDesignationRepository _repo;
    private readonly IAuditService _audit;
    private readonly ICurrentUser _user;

    public DesignationService(IDesignationRepository repo, IAuditService audit, ICurrentUser user)
    {
        _repo = repo;
        _audit = audit;
        _user = user;
    }

    public Task<PagedResult<DesignationDto>> GetPagedAsync(DesignationQuery query, CancellationToken ct = default) =>
        _repo.GetPagedAsync(query, ct);

    public async Task<DesignationDto> GetByIdAsync(long id, CancellationToken ct = default) =>
        await _repo.GetByIdAsync(id, ct) ?? throw new NotFoundException("Designation", id);

    public async Task<DesignationDto> CreateAsync(CreateDesignationRequest request, CancellationToken ct = default)
    {
        var name = request.Name.Trim();
        if (await _repo.NameExistsAsync(name, null, ct))
            throw new ConflictException($"A designation named '{name}' already exists.");

        var id = await _repo.CreateAsync(name, B5Text.Clean(request.Department), B5Text.Clean(request.Description), _user.UserId, ct);
        var created = await GetByIdAsync(id, ct);
        await _audit.LogAsync(AuditActions.Create, "Designation", id, null, created, ct);
        return created;
    }

    public async Task<DesignationDto> UpdateAsync(long id, UpdateDesignationRequest request, CancellationToken ct = default)
    {
        var existing = await GetByIdAsync(id, ct);
        var name = request.Name.Trim();
        var isActive = request.IsActive ?? existing.IsActive;

        if (await _repo.NameExistsAsync(name, id, ct))
            throw new ConflictException($"A designation named '{name}' already exists.");
        if (existing.IsActive && !isActive && existing.EmployeeCount > 0)
            throw new BusinessRuleException($"Cannot deactivate a designation assigned to {existing.EmployeeCount} active employee(s).");

        await _repo.UpdateAsync(id, name, B5Text.Clean(request.Department), B5Text.Clean(request.Description), isActive, _user.UserId, ct);
        var updated = await GetByIdAsync(id, ct);

        var action = existing.IsActive && !isActive ? AuditActions.Deactivate : AuditActions.Update;
        await _audit.LogAsync(action, "Designation", id, existing, updated, ct);
        return updated;
    }

    public async Task DeactivateAsync(long id, CancellationToken ct = default)
    {
        var existing = await GetByIdAsync(id, ct);
        if (!existing.IsActive) return;
        if (existing.EmployeeCount > 0)
            throw new BusinessRuleException($"Cannot deactivate a designation assigned to {existing.EmployeeCount} active employee(s).");

        await _repo.UpdateAsync(id, existing.Name, existing.Department, existing.Description, false, _user.UserId, ct);
        var updated = await GetByIdAsync(id, ct);
        await _audit.LogAsync(AuditActions.Deactivate, "Designation", id, existing, updated, ct);
    }
}
