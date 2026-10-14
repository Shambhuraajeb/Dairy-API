using Dairy.Application.Common;
using Dairy.Application.DTOs.Inventory;
using Dairy.Application.Interfaces;
using Dairy.Application.Interfaces.Inventory;
using Dairy.Application.Requests.Inventory;
using Dairy.Domain.Constants;

namespace Dairy.Application.Services.Inventory;

public class UnitService : IUnitService
{
    private readonly IUnitRepository _repo;
    private readonly IAuditService _audit;
    private readonly ICurrentUser _user;

    public UnitService(IUnitRepository repo, IAuditService audit, ICurrentUser user)
    {
        _repo = repo;
        _audit = audit;
        _user = user;
    }

    public Task<PagedResult<UnitDto>> GetPagedAsync(UnitQuery query, CancellationToken ct = default) =>
        _repo.GetPagedAsync(query, ct);

    public async Task<UnitDto> GetByIdAsync(long id, CancellationToken ct = default) =>
        await _repo.GetByIdAsync(id, ct) ?? throw new NotFoundException("Unit", id);

    public async Task<UnitDto> CreateAsync(CreateUnitRequest request, CancellationToken ct = default)
    {
        var code = request.Code.Trim();
        var name = request.Name.Trim();

        if (await _repo.CodeExistsAsync(code, null, ct))
            throw new ConflictException($"A unit with code '{code}' already exists.");

        var id = await _repo.CreateAsync(code, name, _user.UserId, ct);
        var created = new UnitDto { Id = id, Code = code, Name = name, IsActive = true };

        await _audit.LogAsync(AuditActions.Create, "Unit", id, null, created, ct);
        return created;
    }

    public async Task<UnitDto> UpdateAsync(long id, UpdateUnitRequest request, CancellationToken ct = default)
    {
        var existing = await _repo.GetByIdAsync(id, ct) ?? throw new NotFoundException("Unit", id);

        var code = request.Code.Trim();
        var name = request.Name.Trim();
        var isActive = request.IsActive ?? existing.IsActive;

        if (await _repo.CodeExistsAsync(code, id, ct))
            throw new ConflictException($"A unit with code '{code}' already exists.");

        await _repo.UpdateAsync(id, code, name, isActive, _user.UserId, ct);
        var updated = new UnitDto { Id = id, Code = code, Name = name, IsActive = isActive };

        var action = existing.IsActive && !isActive ? AuditActions.Deactivate : AuditActions.Update;
        await _audit.LogAsync(action, "Unit", id, existing, updated, ct);
        return updated;
    }
}
