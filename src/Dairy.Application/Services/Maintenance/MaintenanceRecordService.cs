using Dairy.Application.Common;
using Dairy.Application.DTOs.Maintenance;
using Dairy.Application.Interfaces;
using Dairy.Application.Interfaces.Maintenance;
using Dairy.Application.Requests.Maintenance;
using Dairy.Domain.Constants;

namespace Dairy.Application.Services.Maintenance;

/// <summary>sp_RecordMaintenance creates the record, advances the schedule and may raise a Pending expense; it does not audit.</summary>
public class MaintenanceRecordService : IMaintenanceRecordService
{
    private readonly IMaintenanceRecordRepository _repo;
    private readonly IAuditService _audit;
    private readonly ICurrentUser _user;

    public MaintenanceRecordService(IMaintenanceRecordRepository repo, IAuditService audit, ICurrentUser user)
    {
        _repo = repo;
        _audit = audit;
        _user = user;
    }

    public Task<PagedResult<MaintenanceRecordDto>> GetPagedAsync(MaintenanceRecordQuery query, CancellationToken ct = default) =>
        _repo.GetPagedAsync(query, ct);

    public async Task<MaintenanceRecordDto> GetByIdAsync(long id, CancellationToken ct = default) =>
        await _repo.GetByIdAsync(id, ct) ?? throw new NotFoundException("Maintenance record", id);

    public async Task<MaintenanceRecordedDto> CreateAsync(CreateMaintenanceRecordRequest request, CancellationToken ct = default)
    {
        request.Description = TextClean.Req(request.Description);
        request.VendorName = TextClean.Opt(request.VendorName);
        request.MaintenanceType = TextClean.Opt(request.MaintenanceType);
        request.PerformedBy = TextClean.Opt(request.PerformedBy);
        request.InvoiceNo = TextClean.Opt(request.InvoiceNo);
        request.Remarks = TextClean.Opt(request.Remarks);

        var recorded = await _repo.RecordAsync(request, _user.UserId, ct);

        if (request.MaintenanceType is not null || request.PerformedBy is not null || request.DowntimeHours.HasValue ||
            request.InvoiceNo is not null || request.Remarks is not null)
            await _repo.SetExtrasAsync(recorded.Id, request, ct);

        await _audit.LogAsync(AuditActions.Create, "MaintenanceRecord", recorded.Id, null,
            new
            {
                request.EquipmentId,
                request.MaintenanceScheduleId,
                request.MaintenanceDate,
                request.Description,
                request.Cost,
                recorded.NextDueDate,
                recorded.ExpenseId
            }, ct);
        return recorded;
    }

    public Task<List<MaintenanceDueDto>> GetDueAsync(int? days, CancellationToken ct = default)
    {
        if (days is < 0)
            throw new RequestValidationException("days", "Days cannot be negative.");
        return _repo.GetDueAsync(days, ct);
    }
}
