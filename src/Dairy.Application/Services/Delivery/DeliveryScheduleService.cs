using Dairy.Application.Common;
using Dairy.Application.DTOs.Delivery;
using Dairy.Application.Interfaces;
using Dairy.Application.Interfaces.Delivery;
using Dairy.Application.Requests.Delivery;
using Dairy.Domain.Constants;

namespace Dairy.Application.Services.Delivery;

public class DeliveryScheduleService : IDeliveryScheduleService
{
    private readonly IDeliveryScheduleRepository _repo;
    private readonly IAuditService _audit;
    private readonly ICurrentUser _user;

    public DeliveryScheduleService(IDeliveryScheduleRepository repo, IAuditService audit, ICurrentUser user)
    {
        _repo = repo;
        _audit = audit;
        _user = user;
    }

    private bool IsDriver => _user.IsInRole(Roles.Driver);

    public Task<PagedResult<DeliveryScheduleDto>> GetPagedAsync(DeliveryScheduleQuery query, CancellationToken ct = default)
    {
        // drivers only see the schedules they drive (an unmatched id yields an empty list)
        if (IsDriver) query.DriverEmployeeId = _user.EmployeeId ?? -1;
        return _repo.GetPagedAsync(query, ct);
    }

    public async Task<DeliveryScheduleDto> GetByIdAsync(long id, CancellationToken ct = default)
    {
        var dto = await _repo.GetByIdAsync(id, ct) ?? throw new NotFoundException("Delivery schedule", id);
        if (IsDriver && dto.DriverEmployeeId != _user.EmployeeId) throw new NotFoundException("Delivery schedule", id);
        return dto;
    }

    public async Task<DeliveryScheduleDto> CreateAsync(CreateDeliveryScheduleRequest request, CancellationToken ct = default)
    {
        if (!await _repo.RouteActiveAsync(request.RouteId, ct))
            throw new BusinessRuleException("The selected route does not exist or is inactive.");
        if (await _repo.SlotTakenAsync(request.RouteId, request.ScheduleDate, request.Slot, null, ct))
            throw new ConflictException("A schedule already exists for this route, date and slot.");

        var id = await _repo.CreateAsync(request, _user.UserId, ct);
        var dto = await _repo.GetByIdAsync(id, ct) ?? throw new NotFoundException("Delivery schedule", id);

        await _audit.LogAsync(AuditActions.Create, "DeliverySchedule", id, null, dto, ct);
        return dto;
    }

    public async Task<DeliveryScheduleDto> UpdateAsync(long id, UpdateDeliveryScheduleRequest request, CancellationToken ct = default)
    {
        var existing = await _repo.GetByIdAsync(id, ct) ?? throw new NotFoundException("Delivery schedule", id);
        if (existing.Status == "Completed")
            throw new BusinessRuleException("A completed schedule cannot be changed.");

        var dateOrSlotChanged = existing.ScheduleDate.Date != request.ScheduleDate.Date || existing.Slot != request.Slot;
        if (dateOrSlotChanged)
        {
            if (existing.Status != "Planned")
                throw new BusinessRuleException("The date and slot can only be changed while the schedule is Planned.");
            if (await _repo.CountLiveDeliveriesAsync(id, ct) > 0)
                throw new BusinessRuleException("Deliveries are already attached to this schedule, so its date and slot cannot change.");
            if (await _repo.SlotTakenAsync(existing.RouteId, request.ScheduleDate, request.Slot, id, ct))
                throw new ConflictException("A schedule already exists for this route, date and slot.");
        }

        var driverChanged = existing.DriverEmployeeId != request.DriverEmployeeId;
        await _repo.UpdateAsync(id, request, driverChanged, _user.UserId, ct);
        var dto = await _repo.GetByIdAsync(id, ct) ?? throw new NotFoundException("Delivery schedule", id);

        await _audit.LogAsync(AuditActions.Update, "DeliverySchedule", id, existing, dto, ct);
        return dto;
    }

    /// <summary>Soft delete: only a schedule without live deliveries can be removed.</summary>
    public async Task DeleteAsync(long id, CancellationToken ct = default)
    {
        var existing = await _repo.GetByIdAsync(id, ct) ?? throw new NotFoundException("Delivery schedule", id);
        if (await _repo.CountLiveDeliveriesAsync(id, ct) > 0)
            throw new BusinessRuleException("Deliveries are attached to this schedule and it cannot be removed.");

        await _repo.DeactivateAsync(id, _user.UserId, ct);
        await _audit.LogAsync(AuditActions.Deactivate, "DeliverySchedule", id, existing, null, ct);
    }
}
