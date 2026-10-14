using Dairy.Application.Common;
using Dairy.Application.DTOs.Hr;
using Dairy.Application.Requests.Hr;

namespace Dairy.Application.Interfaces.Hr;

// ---------- designations ----------
public interface IDesignationRepository
{
    Task<PagedResult<DesignationDto>> GetPagedAsync(DesignationQuery query, CancellationToken ct = default);
    Task<DesignationDto?> GetByIdAsync(long id, CancellationToken ct = default);
    Task<bool> NameExistsAsync(string name, long? excludeId, CancellationToken ct = default);
    Task<long> CreateAsync(string name, string? department, string? description, long userId, CancellationToken ct = default);
    Task UpdateAsync(long id, string name, string? department, string? description, bool isActive, long userId, CancellationToken ct = default);
}

public interface IDesignationService
{
    Task<PagedResult<DesignationDto>> GetPagedAsync(DesignationQuery query, CancellationToken ct = default);
    Task<DesignationDto> GetByIdAsync(long id, CancellationToken ct = default);
    Task<DesignationDto> CreateAsync(CreateDesignationRequest request, CancellationToken ct = default);
    Task<DesignationDto> UpdateAsync(long id, UpdateDesignationRequest request, CancellationToken ct = default);
    Task DeactivateAsync(long id, CancellationToken ct = default);
}

// ---------- employees ----------
public interface IEmployeeRepository
{
    Task<PagedResult<EmployeeDto>> GetPagedAsync(EmployeeQuery query, CancellationToken ct = default);
    Task<EmployeeDto?> GetByIdAsync(long id, CancellationToken ct = default);
    Task<bool> CodeExistsAsync(string code, long? excludeId, CancellationToken ct = default);
    Task<string> GenerateCodeAsync(CancellationToken ct = default);
    Task<bool> DesignationActiveAsync(long designationId, CancellationToken ct = default);
    Task<bool> BranchExistsAsync(long branchId, CancellationToken ct = default);
    Task<long> CreateAsync(CreateEmployeeRequest request, string employeeCode, DateTime joinDate, long userId, CancellationToken ct = default);
    Task UpdateAsync(long id, UpdateEmployeeRequest request, DateTime joinDate, long userId, CancellationToken ct = default);
    Task SetStatusAsync(long id, string status, bool isActive, DateTime? exitDate, long userId, CancellationToken ct = default);
}

public interface IEmployeeService
{
    Task<PagedResult<EmployeeDto>> GetPagedAsync(EmployeeQuery query, CancellationToken ct = default);
    Task<EmployeeDto> GetByIdAsync(long id, CancellationToken ct = default);
    Task<EmployeeDto> CreateAsync(CreateEmployeeRequest request, CancellationToken ct = default);
    Task<EmployeeDto> UpdateAsync(long id, UpdateEmployeeRequest request, CancellationToken ct = default);
    Task<EmployeeDto> DeactivateAsync(long id, DeactivateEmployeeRequest? request, CancellationToken ct = default);
    Task<EmployeeDto> ActivateAsync(long id, CancellationToken ct = default);
}

// ---------- attendance ----------
public interface IAttendanceRepository
{
    Task<PagedResult<AttendanceDto>> GetPagedAsync(AttendanceQuery query, CancellationToken ct = default);
    Task<List<AttendanceSheetRowDto>> GetSheetAsync(DateTime date, long? designationId, CancellationToken ct = default);
    Task<AttendanceDto?> GetByEmployeeDateAsync(long employeeId, DateTime date, CancellationToken ct = default);
    Task<HashSet<long>> GetActiveEmployeeIdsAsync(IEnumerable<long> employeeIds, CancellationToken ct = default);
    Task UpsertAsync(IReadOnlyCollection<AttendanceUpsertModel> entries, long userId, CancellationToken ct = default);
}

public interface IAttendanceService
{
    Task<PagedResult<AttendanceDto>> GetPagedAsync(AttendanceQuery query, CancellationToken ct = default);
    Task<List<AttendanceSheetRowDto>> GetSheetAsync(AttendanceSheetQuery query, CancellationToken ct = default);
    Task<AttendanceDto> UpsertAsync(UpsertAttendanceRequest request, CancellationToken ct = default);
    Task<AttendanceBulkResultDto> BulkUpsertAsync(BulkAttendanceRequest request, CancellationToken ct = default);
}

// ---------- leave ----------
public interface ILeaveRequestRepository
{
    Task<PagedResult<LeaveRequestDto>> GetPagedAsync(LeaveRequestQuery query, CancellationToken ct = default);
    Task<LeaveRequestDto?> GetByIdAsync(long id, CancellationToken ct = default);
    Task<bool> HasOverlapAsync(long employeeId, DateTime from, DateTime to, CancellationToken ct = default);
    Task<long> CreateAsync(long employeeId, string leaveType, DateTime from, DateTime to, decimal days, string? reason, long userId, CancellationToken ct = default);

    /// <summary>Approves a Pending request and marks the attendance days; false when the request was not Pending.</summary>
    Task<bool> ApproveAsync(long id, long userId, CancellationToken ct = default);

    /// <summary>Rejects a Pending request; false when the request was not Pending.</summary>
    Task<bool> RejectAsync(long id, long userId, string? reason, CancellationToken ct = default);
}

public interface ILeaveRequestService
{
    Task<PagedResult<LeaveRequestDto>> GetPagedAsync(LeaveRequestQuery query, CancellationToken ct = default);
    Task<LeaveRequestDto> GetByIdAsync(long id, CancellationToken ct = default);
    Task<LeaveRequestDto> CreateAsync(CreateLeaveRequest request, CancellationToken ct = default);
    Task<LeaveRequestDto> ApproveAsync(long id, CancellationToken ct = default);
    Task<LeaveRequestDto> RejectAsync(long id, RejectLeaveRequest? request, CancellationToken ct = default);
}

// ---------- salaries ----------
public interface ISalaryRepository
{
    Task<PagedResult<SalaryDto>> GetPagedAsync(SalaryQuery query, CancellationToken ct = default);
    Task<SalaryDto?> GetByIdAsync(long id, CancellationToken ct = default);
    Task<SalaryGenerateResultDto> GenerateAsync(int month, int year, long userId, CancellationToken ct = default);
    Task PayAsync(long id, long paymentModeId, long userId, CancellationToken ct = default);
}

public interface ISalaryService
{
    Task<PagedResult<SalaryDto>> GetPagedAsync(SalaryQuery query, CancellationToken ct = default);
    Task<SalaryDto> GetByIdAsync(long id, CancellationToken ct = default);
    Task<SalaryGenerateResultDto> GenerateAsync(GenerateSalariesRequest request, CancellationToken ct = default);
    Task<SalaryDto> PayAsync(long id, PaySalaryRequest request, CancellationToken ct = default);
}
