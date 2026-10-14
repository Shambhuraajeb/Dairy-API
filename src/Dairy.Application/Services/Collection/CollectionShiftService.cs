using Dairy.Application.Common;
using Dairy.Application.DTOs.Collection;
using Dairy.Application.Interfaces;
using Dairy.Application.Interfaces.Collection;
using Dairy.Application.Requests.Collection;
using Dairy.Domain.Constants;

namespace Dairy.Application.Services.Collection;

public class CollectionShiftService : ICollectionShiftService
{
    private readonly ICollectionShiftRepository _repo;
    private readonly ICurrentUser _user;

    public CollectionShiftService(ICollectionShiftRepository repo, ICurrentUser user)
    {
        _repo = repo;
        _user = user;
    }

    public Task<List<CollectionShiftDto>> GetShiftsAsync(ShiftListQuery query, CancellationToken ct = default) =>
        _repo.GetShiftsAsync(query.CenterId, query.IsActive, ct);

    public async Task<CollectionDashboardDto> GetDashboardAsync(ShiftDashboardQuery query, CancellationToken ct = default)
    {
        if (_user.IsInRole(Roles.Farmer))
            throw new ForbiddenException();

        var date = (query.Date ?? DateTime.Today).Date;
        var allShifts = await _repo.GetShiftsAsync(query.CenterId, null, ct);

        CollectionShiftDto? shift;
        var isCurrent = false;
        if (query.ShiftId.HasValue)
        {
            shift = allShifts.FirstOrDefault(s => s.Id == query.ShiftId.Value)
                    ?? throw new NotFoundException("Collection shift", query.ShiftId.Value);
            isCurrent = IsRunningNow(shift, DateTime.Now.TimeOfDay);
        }
        else
        {
            shift = ResolveShift(allShifts.Where(s => s.IsActive).ToList(), DateTime.Now.TimeOfDay, out isCurrent);
        }
        isCurrent = isCurrent && date == DateTime.Today;

        var summary = await _repo.GetSummaryAsync(date, query.CenterId, shift?.ShiftName, ct);
        summary.ShiftId = shift?.Id;
        summary.ShiftName = shift?.ShiftName;
        summary.StartTime = shift?.StartTime;
        summary.EndTime = shift?.EndTime;
        summary.CollectionCenterId = query.CenterId;

        return new CollectionDashboardDto
        {
            Date = date,
            CollectionCenterId = query.CenterId,
            ShiftId = shift?.Id,
            ShiftName = shift?.ShiftName,
            StartTime = shift?.StartTime,
            EndTime = shift?.EndTime,
            IsCurrentShift = isCurrent,
            Summary = summary,
            Shifts = await _repo.GetBreakdownAsync(date, query.CenterId, ct),
            TotalPendingApprovals = await _repo.GetPendingApprovalCountAsync(query.CenterId, ct)
        };
    }

    private static bool IsRunningNow(CollectionShiftDto s, TimeSpan now) =>
        s.StartTime <= s.EndTime
            ? now >= s.StartTime && now <= s.EndTime
            : now >= s.StartTime || now <= s.EndTime;

    /// <summary>Shift whose window contains now; else the most recently started one.</summary>
    private static CollectionShiftDto? ResolveShift(List<CollectionShiftDto> active, TimeSpan now, out bool isCurrent)
    {
        var ordered = active.OrderBy(s => s.CollectionCenterId == null).ThenBy(s => s.StartTime).ToList();
        var current = ordered.FirstOrDefault(s => IsRunningNow(s, now));
        if (current is not null)
        {
            isCurrent = true;
            return current;
        }

        isCurrent = false;
        return ordered.Where(s => s.StartTime <= now).OrderByDescending(s => s.StartTime).FirstOrDefault()
               ?? ordered.OrderByDescending(s => s.StartTime).FirstOrDefault();
    }
}
