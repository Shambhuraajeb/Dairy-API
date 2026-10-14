using Dairy.Application.Common;

namespace Dairy.Application.Requests.Hr;

public class DesignationQuery : PagedQuery
{
    public bool? IsActive { get; set; }
}

public class CreateDesignationRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Department { get; set; }
    public string? Description { get; set; }
}

public class UpdateDesignationRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Department { get; set; }
    public string? Description { get; set; }

    /// <summary>Null = unchanged; false = deactivate.</summary>
    public bool? IsActive { get; set; }
}

public class EmployeeQuery : PagedQuery
{
    public long? DesignationId { get; set; }
    public string? Status { get; set; }
    public long? BranchId { get; set; }
}

public abstract class EmployeeRequestBase
{
    public string FullName { get; set; } = string.Empty;
    public string? Gender { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public string? Mobile { get; set; }
    public string? Email { get; set; }
    public string? AddressLine { get; set; }
    public string? City { get; set; }
    public long DesignationId { get; set; }
    public long? BranchId { get; set; }
    public DateTime? JoinDate { get; set; }
    public decimal BasicSalary { get; set; }
    public decimal DefaultAllowances { get; set; }
    public decimal DefaultDeductions { get; set; }
    public string? BankName { get; set; }
    public string? BankAccountNo { get; set; }
    public string? IfscCode { get; set; }
    public string? AadhaarNo { get; set; }
    public string? PanNo { get; set; }
    public string? EmergencyContact { get; set; }
}

public class CreateEmployeeRequest : EmployeeRequestBase
{
    /// <summary>Optional; blank = auto-generated (EMP001, EMP002, ...).</summary>
    public string? EmployeeCode { get; set; }
}

public class UpdateEmployeeRequest : EmployeeRequestBase
{
    public DateTime? ExitDate { get; set; }
}

public class DeactivateEmployeeRequest
{
    /// <summary>Inactive (default) or Terminated.</summary>
    public string? Status { get; set; }
    public DateTime? ExitDate { get; set; }
}

public class AttendanceQuery : PagedQuery
{
    /// <summary>Single day (overrides From/To).</summary>
    public DateTime? Date { get; set; }
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }
    public long? EmployeeId { get; set; }
    public string? Status { get; set; }
}

public class AttendanceSheetQuery
{
    public DateTime? Date { get; set; }
    public long? DesignationId { get; set; }
}

public class UpsertAttendanceRequest
{
    public long EmployeeId { get; set; }
    public DateTime Date { get; set; }
    public string Status { get; set; } = string.Empty;

    /// <summary>ISO date-time or "HH:mm" on <see cref="Date"/>.</summary>
    public string? CheckIn { get; set; }
    public string? CheckOut { get; set; }
    public string? Remarks { get; set; }
}

public class AttendanceEntryRequest
{
    public long EmployeeId { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? CheckIn { get; set; }
    public string? CheckOut { get; set; }
    public string? Remarks { get; set; }
}

public class BulkAttendanceRequest
{
    public DateTime Date { get; set; }
    public List<AttendanceEntryRequest> Entries { get; set; } = new();
}

public class LeaveRequestQuery : PagedQuery
{
    public long? EmployeeId { get; set; }
    public string? Status { get; set; }
    public string? LeaveType { get; set; }
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }
}

public class CreateLeaveRequest
{
    /// <summary>Blank = the caller's own employee record.</summary>
    public long? EmployeeId { get; set; }
    public string LeaveType { get; set; } = string.Empty;
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }

    /// <summary>Optional (multiples of 0.5); default = calendar days between FromDate and ToDate inclusive.</summary>
    public decimal? Days { get; set; }
    public string? Reason { get; set; }
}

public class RejectLeaveRequest
{
    public string? Reason { get; set; }
}

public class SalaryQuery : PagedQuery
{
    public int? Month { get; set; }
    public int? Year { get; set; }
    public long? EmployeeId { get; set; }
    public string? Status { get; set; }
}

public class GenerateSalariesRequest
{
    public int Month { get; set; }
    public int Year { get; set; }
}

public class PaySalaryRequest
{
    public long PaymentModeId { get; set; }
}
