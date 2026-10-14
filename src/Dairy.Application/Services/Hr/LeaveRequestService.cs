using Dairy.Application.Common;
using Dairy.Application.DTOs.Hr;
using Dairy.Application.Interfaces;
using Dairy.Application.Interfaces.Hr;
using Dairy.Application.Requests.Hr;
using Dairy.Domain.Constants;

namespace Dairy.Application.Services.Hr;

public class LeaveRequestService : ILeaveRequestService
{
    private const int MaxLeaveSpanDays = 366;

    private readonly ILeaveRequestRepository _repo;
    private readonly IEmployeeRepository _employees;
    private readonly IAuditService _audit;
    private readonly ICurrentUser _user;

    public LeaveRequestService(ILeaveRequestRepository repo, IEmployeeRepository employees, IAuditService audit, ICurrentUser user)
    {
        _repo = repo;
        _employees = employees;
        _audit = audit;
        _user = user;
    }

    public Task<PagedResult<LeaveRequestDto>> GetPagedAsync(LeaveRequestQuery query, CancellationToken ct = default)
    {
        if (!CanSeeAll())
            query.EmployeeId = _user.EmployeeId ?? throw new ForbiddenException("Your user account is not linked to an employee record.");
        query.From = query.From?.Date;
        query.To = query.To?.Date;
        return _repo.GetPagedAsync(query, ct);
    }

    public async Task<LeaveRequestDto> GetByIdAsync(long id, CancellationToken ct = default)
    {
        var dto = await _repo.GetByIdAsync(id, ct) ?? throw new NotFoundException("Leave request", id);
        if (!CanSeeAll() && dto.EmployeeId != _user.EmployeeId)
            throw new NotFoundException("Leave request", id);
        return dto;
    }

    public async Task<LeaveRequestDto> CreateAsync(CreateLeaveRequest request, CancellationToken ct = default)
    {
        var employeeId = request.EmployeeId ?? _user.EmployeeId
                         ?? throw new BusinessRuleException("Your user account is not linked to an employee record.");

        // Only approvers may file leave on behalf of someone else.
        if (employeeId != _user.EmployeeId && !_user.HasPermission(Permissions.LeaveApprove))
            throw new ForbiddenException("You can only apply for leave for yourself.");

        var employee = await _employees.GetByIdAsync(employeeId, ct)
                       ?? throw new RequestValidationException("employeeId", "Employee not found.");
        if (employee.Status != "Active")
            throw new BusinessRuleException("Leave can only be requested for an active employee.");

        var from = request.FromDate.Date;
        var to = request.ToDate.Date;
        var span = (to - from).Days + 1;
        if (span > MaxLeaveSpanDays)
            throw new RequestValidationException("toDate", $"A leave request cannot span more than {MaxLeaveSpanDays} days.");

        var days = request.Days ?? span;
        if (days > span)
            throw new RequestValidationException("days", "Days cannot exceed the number of calendar days in the range.");

        if (await _repo.HasOverlapAsync(employeeId, from, to, ct))
            throw new BusinessRuleException("This employee already has a pending or approved leave overlapping these dates.");

        var id = await _repo.CreateAsync(employeeId, request.LeaveType, from, to, days, B5Text.Clean(request.Reason), _user.UserId, ct);
        var created = await _repo.GetByIdAsync(id, ct) ?? throw new NotFoundException("Leave request", id);
        await _audit.LogAsync(AuditActions.Create, "LeaveRequest", id, null, created, ct);
        return created;
    }

    public async Task<LeaveRequestDto> ApproveAsync(long id, CancellationToken ct = default)
    {
        var existing = await _repo.GetByIdAsync(id, ct) ?? throw new NotFoundException("Leave request", id);
        EnsureNotOwn(existing);
        if (existing.Status != LeaveStatus.Pending)
            throw new BusinessRuleException($"Only pending requests can be approved (current status: {existing.Status}).");

        if (!await _repo.ApproveAsync(id, _user.UserId, ct))
            throw new BusinessRuleException("The request is no longer pending.");

        var updated = await _repo.GetByIdAsync(id, ct) ?? throw new NotFoundException("Leave request", id);
        await _audit.LogAsync(AuditActions.Approve, "LeaveRequest", id, existing, updated, ct);
        return updated;
    }

    public async Task<LeaveRequestDto> RejectAsync(long id, RejectLeaveRequest? request, CancellationToken ct = default)
    {
        var existing = await _repo.GetByIdAsync(id, ct) ?? throw new NotFoundException("Leave request", id);
        EnsureNotOwn(existing);
        if (existing.Status != LeaveStatus.Pending)
            throw new BusinessRuleException($"Only pending requests can be rejected (current status: {existing.Status}).");

        if (!await _repo.RejectAsync(id, _user.UserId, B5Text.Clean(request?.Reason), ct))
            throw new BusinessRuleException("The request is no longer pending.");

        var updated = await _repo.GetByIdAsync(id, ct) ?? throw new NotFoundException("Leave request", id);
        await _audit.LogAsync(AuditActions.Reject, "LeaveRequest", id, existing, updated, ct);
        return updated;
    }

    private bool CanSeeAll() =>
        _user.HasPermission(Permissions.LeaveApprove) || _user.HasPermission(Permissions.EmployeeView);

    private void EnsureNotOwn(LeaveRequestDto dto)
    {
        if (dto.EmployeeId == _user.EmployeeId && !_user.IsInRole(Roles.SuperAdmin))
            throw new ForbiddenException("You cannot approve or reject your own leave request.");
    }
}
