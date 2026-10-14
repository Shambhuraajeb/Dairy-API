using Dairy.Application.Common;
using Dairy.Application.DTOs.Hr;
using Dairy.Application.Interfaces;
using Dairy.Application.Interfaces.Hr;
using Dairy.Application.Requests.Hr;
using Dairy.Domain.Constants;

namespace Dairy.Application.Services.Hr;

public class AttendanceService : IAttendanceService
{
    private readonly IAttendanceRepository _repo;
    private readonly IAuditService _audit;
    private readonly ICurrentUser _user;

    public AttendanceService(IAttendanceRepository repo, IAuditService audit, ICurrentUser user)
    {
        _repo = repo;
        _audit = audit;
        _user = user;
    }

    public Task<PagedResult<AttendanceDto>> GetPagedAsync(AttendanceQuery query, CancellationToken ct = default)
    {
        // Staff without HR rights only ever see their own attendance.
        if (!CanSeeAll())
            query.EmployeeId = _user.EmployeeId ?? throw new ForbiddenException("Your user account is not linked to an employee record.");

        if (query.Date.HasValue)
        {
            query.From = query.Date.Value.Date;
            query.To = query.Date.Value.Date;
        }
        else
        {
            query.From = query.From?.Date;
            query.To = query.To?.Date;
        }
        return _repo.GetPagedAsync(query, ct);
    }

    public Task<List<AttendanceSheetRowDto>> GetSheetAsync(AttendanceSheetQuery query, CancellationToken ct = default)
    {
        if (!CanSeeAll()) throw new ForbiddenException();
        return _repo.GetSheetAsync((query.Date ?? DateTime.Today).Date, query.DesignationId, ct);
    }

    public async Task<AttendanceDto> UpsertAsync(UpsertAttendanceRequest request, CancellationToken ct = default)
    {
        var date = request.Date.Date;
        EnsureNotFuture(date);

        var active = await _repo.GetActiveEmployeeIdsAsync(new[] { request.EmployeeId }, ct);
        if (!active.Contains(request.EmployeeId))
            throw new RequestValidationException("employeeId", "Employee not found or not active.");

        var model = ToModel(request.EmployeeId, date, request.Status, request.CheckIn, request.CheckOut, request.Remarks, "employeeId");
        var before = await _repo.GetByEmployeeDateAsync(request.EmployeeId, date, ct);
        await _repo.UpsertAsync(new[] { model }, _user.UserId, ct);
        var after = await _repo.GetByEmployeeDateAsync(request.EmployeeId, date, ct)
                    ?? throw new NotFoundException("Attendance", request.EmployeeId);

        await _audit.LogAsync(before == null ? AuditActions.Create : AuditActions.Update, "Attendance", after.Id, before, after, ct);
        return after;
    }

    public async Task<AttendanceBulkResultDto> BulkUpsertAsync(BulkAttendanceRequest request, CancellationToken ct = default)
    {
        var date = request.Date.Date;
        EnsureNotFuture(date);

        var duplicate = request.Entries.GroupBy(e => e.EmployeeId).FirstOrDefault(g => g.Count() > 1);
        if (duplicate != null)
            throw new RequestValidationException("entries", $"Employee {duplicate.Key} appears more than once.");

        var ids = request.Entries.Select(e => e.EmployeeId).ToList();
        var active = await _repo.GetActiveEmployeeIdsAsync(ids, ct);
        var missing = ids.Where(i => !active.Contains(i)).ToList();
        if (missing.Count > 0)
            throw new RequestValidationException("entries", "Employee(s) not found or not active: " + string.Join(", ", missing) + ".");

        var models = request.Entries
            .Select(e => ToModel(e.EmployeeId, date, e.Status, e.CheckIn, e.CheckOut, e.Remarks, "entries"))
            .ToList();
        await _repo.UpsertAsync(models, _user.UserId, ct);

        var summary = new
        {
            Date = date.ToString("yyyy-MM-dd"),
            Count = models.Count,
            Present = models.Count(m => m.Status == AttendanceStatus.Present),
            Absent = models.Count(m => m.Status == AttendanceStatus.Absent),
            HalfDay = models.Count(m => m.Status == AttendanceStatus.HalfDay),
            Leave = models.Count(m => m.Status == AttendanceStatus.Leave)
        };
        await _audit.LogAsync(AuditActions.Update, "Attendance", date.ToString("yyyy-MM-dd"), null, summary, ct);
        return new AttendanceBulkResultDto { Date = date, Saved = models.Count };
    }

    private bool CanSeeAll() =>
        _user.HasPermission(Permissions.AttendanceManage) || _user.HasPermission(Permissions.EmployeeView);

    private static void EnsureNotFuture(DateTime date)
    {
        if (date > DateTime.Today)
            throw new RequestValidationException("date", "Attendance cannot be recorded for a future date.");
    }

    private static AttendanceUpsertModel ToModel(long employeeId, DateTime date, string status, string? checkIn, string? checkOut, string? remarks, string field)
    {
        B5Text.TryParseDayTime(checkIn, date, out var inTime);
        B5Text.TryParseDayTime(checkOut, date, out var outTime);

        decimal worked = 0;
        if (inTime.HasValue && outTime.HasValue)
        {
            if (outTime.Value <= inTime.Value)
                throw new RequestValidationException(field, "Check-out must be after check-in.");
            worked = Math.Round((decimal)(outTime.Value - inTime.Value).TotalHours, 2);
            if (worked > 24) throw new RequestValidationException(field, "Worked hours cannot exceed 24.");
        }

        return new AttendanceUpsertModel
        {
            EmployeeId = employeeId,
            Date = date,
            Status = status,
            CheckIn = inTime,
            CheckOut = outTime,
            WorkedHours = worked,
            Remarks = B5Text.Clean(remarks)
        };
    }
}
