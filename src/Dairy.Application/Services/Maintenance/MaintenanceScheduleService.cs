using Dairy.Application.Common;
using Dairy.Application.DTOs.Maintenance;
using Dairy.Application.Interfaces;
using Dairy.Application.Interfaces.Maintenance;
using Dairy.Application.Requests.Maintenance;
using Dairy.Domain.Constants;

namespace Dairy.Application.Services.Maintenance;

public class MaintenanceScheduleService : IMaintenanceScheduleService
{
    private readonly IMaintenanceScheduleRepository _repo;
    private readonly IAuditService _audit;
    private readonly ICurrentUser _user;

    public MaintenanceScheduleService(IMaintenanceScheduleRepository repo, IAuditService audit, ICurrentUser user)
    {
        _repo = repo;
        _audit = audit;
        _user = user;
    }

    public Task<PagedResult<MaintenanceScheduleDto>> GetPagedAsync(MaintenanceScheduleQuery query, CancellationToken ct = default) =>
        _repo.GetPagedAsync(query, ct);

    public async Task<MaintenanceScheduleDto> GetByIdAsync(long id, CancellationToken ct = default) =>
        await _repo.GetByIdAsync(id, ct) ?? throw new NotFoundException("Maintenance schedule", id);

    public async Task<MaintenanceScheduleDto> CreateAsync(CreateMaintenanceScheduleRequest request, CancellationToken ct = default)
    {
        request.TaskName = TextClean.Req(request.TaskName);
        request.Description = TextClean.Opt(request.Description);

        if (!await _repo.EquipmentExistsAsync(request.EquipmentId, ct))
            throw new RequestValidationException("equipmentId", "Equipment not found.");
        if (request.AssignedEmployeeId.HasValue && !await _repo.EmployeeExistsAsync(request.AssignedEmployeeId.Value, ct))
            throw new RequestValidationException("assignedEmployeeId", "Employee not found.");

        var nextDue = (request.NextDueDate ?? (request.LastServiceDate ?? DateTime.Today).AddDays(request.FrequencyDays)).Date;

        var id = await _repo.CreateAsync(request, nextDue, _user.UserId, ct);
        var created = await GetByIdAsync(id, ct);
        await _audit.LogAsync(AuditActions.Create, "MaintenanceSchedule", id, null, created, ct);
        return created;
    }

    public async Task<MaintenanceScheduleDto> UpdateAsync(long id, UpdateMaintenanceScheduleRequest request, CancellationToken ct = default)
    {
        var existing = await GetByIdAsync(id, ct);

        request.TaskName = TextClean.Req(request.TaskName);
        request.Description = TextClean.Opt(request.Description);
        if (request.AssignedEmployeeId.HasValue && !await _repo.EmployeeExistsAsync(request.AssignedEmployeeId.Value, ct))
            throw new RequestValidationException("assignedEmployeeId", "Employee not found.");

        var status = string.IsNullOrWhiteSpace(request.Status) ? existing.Status : request.Status!;
        var isActive = request.IsActive ?? existing.IsActive;
        var nextDue = (request.NextDueDate ?? existing.NextDueDate).Date;

        await _repo.UpdateAsync(id, request, nextDue, status, isActive, _user.UserId, ct);
        var updated = await GetByIdAsync(id, ct);

        var action = existing.IsActive && !isActive ? AuditActions.Deactivate : AuditActions.Update;
        await _audit.LogAsync(action, "MaintenanceSchedule", id, existing, updated, ct);
        return updated;
    }
}
