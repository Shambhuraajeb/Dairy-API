using Dairy.Application.Common;
using Dairy.Application.DTOs.Delivery;
using Dairy.Application.Interfaces;
using Dairy.Application.Interfaces.Delivery;
using Dairy.Application.Requests.Delivery;
using Dairy.Domain.Constants;

namespace Dairy.Application.Services.Delivery;

public class VehicleService : IVehicleService
{
    private readonly IVehicleRepository _repo;
    private readonly IAuditService _audit;
    private readonly ICurrentUser _user;

    public VehicleService(IVehicleRepository repo, IAuditService audit, ICurrentUser user)
    {
        _repo = repo;
        _audit = audit;
        _user = user;
    }

    public Task<PagedResult<VehicleDto>> GetPagedAsync(VehicleQuery query, CancellationToken ct = default) =>
        _repo.GetPagedAsync(query, ct);

    public async Task<VehicleDto> GetByIdAsync(long id, CancellationToken ct = default) =>
        await _repo.GetByIdAsync(id, ct) ?? throw new NotFoundException("Vehicle", id);

    public async Task<VehicleDto> CreateAsync(CreateVehicleRequest request, CancellationToken ct = default)
    {
        var reg = Normalize(request.RegistrationNo);
        if (await _repo.RegistrationExistsAsync(reg, null, ct))
            throw new ConflictException($"A vehicle with registration number '{reg}' already exists.");

        var id = await _repo.CreateAsync(reg, request, _user.UserId, ct);
        var dto = await _repo.GetByIdAsync(id, ct) ?? throw new NotFoundException("Vehicle", id);

        await _audit.LogAsync(AuditActions.Create, "Vehicle", id, null, dto, ct);
        return dto;
    }

    public async Task<VehicleDto> UpdateAsync(long id, UpdateVehicleRequest request, CancellationToken ct = default)
    {
        var existing = await _repo.GetByIdAsync(id, ct) ?? throw new NotFoundException("Vehicle", id);
        var reg = Normalize(request.RegistrationNo);
        if (await _repo.RegistrationExistsAsync(reg, id, ct))
            throw new ConflictException($"A vehicle with registration number '{reg}' already exists.");

        var isActive = request.IsActive ?? existing.IsActive;
        if (existing.IsActive && !isActive) await EnsureNoOpenSchedulesAsync(id, ct);

        await _repo.UpdateAsync(id, reg, request, isActive, _user.UserId, ct);
        var dto = await _repo.GetByIdAsync(id, ct) ?? throw new NotFoundException("Vehicle", id);

        var action = existing.IsActive && !isActive ? AuditActions.Deactivate : AuditActions.Update;
        await _audit.LogAsync(action, "Vehicle", id, existing, dto, ct);
        return dto;
    }

    /// <summary>Soft delete: marks the vehicle inactive (blocked while upcoming schedules use it).</summary>
    public async Task DeleteAsync(long id, CancellationToken ct = default)
    {
        var existing = await _repo.GetByIdAsync(id, ct) ?? throw new NotFoundException("Vehicle", id);
        await EnsureNoOpenSchedulesAsync(id, ct);

        await _repo.SetInactiveAsync(id, _user.UserId, ct);
        await _audit.LogAsync(AuditActions.Deactivate, "Vehicle", id, existing, null, ct);
    }

    private static string Normalize(string registrationNo) => registrationNo.Trim().ToUpperInvariant();

    private async Task EnsureNoOpenSchedulesAsync(long id, CancellationToken ct)
    {
        var open = await _repo.CountOpenSchedulesAsync(id, ct);
        if (open > 0)
            throw new BusinessRuleException($"This vehicle is used by {open} upcoming delivery schedule(s). Reassign them first.");
    }
}
