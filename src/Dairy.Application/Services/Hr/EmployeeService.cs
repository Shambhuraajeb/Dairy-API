using Dairy.Application.Common;
using Dairy.Application.DTOs.Hr;
using Dairy.Application.Interfaces;
using Dairy.Application.Interfaces.Hr;
using Dairy.Application.Requests.Hr;
using Dairy.Domain.Constants;

namespace Dairy.Application.Services.Hr;

public class EmployeeService : IEmployeeService
{
    private readonly IEmployeeRepository _repo;
    private readonly IAuditService _audit;
    private readonly ICurrentUser _user;

    public EmployeeService(IEmployeeRepository repo, IAuditService audit, ICurrentUser user)
    {
        _repo = repo;
        _audit = audit;
        _user = user;
    }

    public Task<PagedResult<EmployeeDto>> GetPagedAsync(EmployeeQuery query, CancellationToken ct = default) =>
        _repo.GetPagedAsync(query, ct);

    public async Task<EmployeeDto> GetByIdAsync(long id, CancellationToken ct = default) =>
        await _repo.GetByIdAsync(id, ct) ?? throw new NotFoundException("Employee", id);

    public async Task<EmployeeDto> CreateAsync(CreateEmployeeRequest request, CancellationToken ct = default)
    {
        Normalize(request);
        await EnsureReferencesAsync(request, ct);

        string code;
        var requested = B5Text.Clean(request.EmployeeCode);
        if (requested != null)
        {
            code = requested.ToUpperInvariant();
            if (await _repo.CodeExistsAsync(code, null, ct))
                throw new ConflictException($"An employee with code '{code}' already exists.");
        }
        else
        {
            code = await _repo.GenerateCodeAsync(ct);
        }

        var joinDate = (request.JoinDate ?? DateTime.Today).Date;
        var id = await _repo.CreateAsync(request, code, joinDate, _user.UserId, ct);
        var created = await GetByIdAsync(id, ct);
        await _audit.LogAsync(AuditActions.Create, "Employee", id, null, created, ct);
        return created;
    }

    public async Task<EmployeeDto> UpdateAsync(long id, UpdateEmployeeRequest request, CancellationToken ct = default)
    {
        var existing = await GetByIdAsync(id, ct);
        Normalize(request);
        request.ExitDate = request.ExitDate?.Date;
        await EnsureReferencesAsync(request, ct);

        var joinDate = (request.JoinDate ?? existing.JoinDate).Date;
        if (request.ExitDate.HasValue && request.ExitDate.Value < joinDate)
            throw new RequestValidationException("exitDate", "Exit date cannot be before the join date.");

        await _repo.UpdateAsync(id, request, joinDate, _user.UserId, ct);
        var updated = await GetByIdAsync(id, ct);
        await _audit.LogAsync(AuditActions.Update, "Employee", id, existing, updated, ct);
        return updated;
    }

    public async Task<EmployeeDto> DeactivateAsync(long id, DeactivateEmployeeRequest? request, CancellationToken ct = default)
    {
        var existing = await GetByIdAsync(id, ct);
        var status = B5Text.Clean(request?.Status) ?? "Inactive";
        if (status != "Inactive" && status != "Terminated")
            throw new RequestValidationException("status", "Status must be Inactive or Terminated.");

        var exitDate = (request?.ExitDate ?? existing.ExitDate ?? DateTime.Today).Date;
        await _repo.SetStatusAsync(id, status, false, exitDate, _user.UserId, ct);
        var updated = await GetByIdAsync(id, ct);
        await _audit.LogAsync(AuditActions.Deactivate, "Employee", id, existing, updated, ct);
        return updated;
    }

    public async Task<EmployeeDto> ActivateAsync(long id, CancellationToken ct = default)
    {
        var existing = await GetByIdAsync(id, ct);
        if (!await _repo.DesignationActiveAsync(existing.DesignationId, ct))
            throw new BusinessRuleException("The employee's designation is inactive. Assign an active designation first.");

        await _repo.SetStatusAsync(id, "Active", true, null, _user.UserId, ct);
        var updated = await GetByIdAsync(id, ct);
        await _audit.LogAsync(AuditActions.Activate, "Employee", id, existing, updated, ct);
        return updated;
    }

    private async Task EnsureReferencesAsync(EmployeeRequestBase r, CancellationToken ct)
    {
        if (!await _repo.DesignationActiveAsync(r.DesignationId, ct))
            throw new RequestValidationException("designationId", "Designation not found or inactive.");
        if (r.BranchId.HasValue && !await _repo.BranchExistsAsync(r.BranchId.Value, ct))
            throw new RequestValidationException("branchId", "Branch not found.");
    }

    private static void Normalize(EmployeeRequestBase r)
    {
        r.FullName = r.FullName.Trim();
        r.Gender = B5Text.Clean(r.Gender);
        r.DateOfBirth = r.DateOfBirth?.Date;
        r.JoinDate = r.JoinDate?.Date;
        r.Mobile = B5Text.Clean(r.Mobile);
        r.Email = B5Text.Clean(r.Email);
        r.AddressLine = B5Text.Clean(r.AddressLine);
        r.City = B5Text.Clean(r.City);
        r.BankName = B5Text.Clean(r.BankName);
        r.BankAccountNo = B5Text.Clean(r.BankAccountNo);
        r.IfscCode = B5Text.Clean(r.IfscCode)?.ToUpperInvariant();
        r.AadhaarNo = B5Text.Clean(r.AadhaarNo);
        r.PanNo = B5Text.Clean(r.PanNo)?.ToUpperInvariant();
        r.EmergencyContact = B5Text.Clean(r.EmergencyContact);
    }
}
