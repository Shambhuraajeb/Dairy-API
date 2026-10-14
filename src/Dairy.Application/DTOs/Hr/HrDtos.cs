namespace Dairy.Application.DTOs.Hr;

public class DesignationDto
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Department { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public int EmployeeCount { get; set; }
}

public class EmployeeDto
{
    public long Id { get; set; }
    public string EmployeeCode { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? Gender { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public string? Mobile { get; set; }
    public string? Email { get; set; }
    public string? AddressLine { get; set; }
    public string? City { get; set; }
    public long DesignationId { get; set; }
    public string DesignationName { get; set; } = string.Empty;
    public long? BranchId { get; set; }
    public string? BranchName { get; set; }
    public DateTime JoinDate { get; set; }
    public DateTime? ExitDate { get; set; }
    public decimal BasicSalary { get; set; }
    public decimal DefaultAllowances { get; set; }
    public decimal DefaultDeductions { get; set; }
    public string? BankName { get; set; }
    public string? BankAccountNo { get; set; }
    public string? IfscCode { get; set; }
    public string? AadhaarNo { get; set; }
    public string? PanNo { get; set; }
    public string? EmergencyContact { get; set; }
    public string Status { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreatedDate { get; set; }
}

public class AttendanceDto
{
    public long Id { get; set; }
    public long EmployeeId { get; set; }
    public string EmployeeCode { get; set; } = string.Empty;
    public string EmployeeName { get; set; } = string.Empty;
    public DateTime AttendanceDate { get; set; }
    public DateTime? CheckIn { get; set; }
    public DateTime? CheckOut { get; set; }
    public decimal WorkedHours { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? Remarks { get; set; }
}

/// <summary>One row of the daily attendance sheet: every active employee with the day's record (if any).</summary>
public class AttendanceSheetRowDto
{
    public long EmployeeId { get; set; }
    public string EmployeeCode { get; set; } = string.Empty;
    public string EmployeeName { get; set; } = string.Empty;
    public string DesignationName { get; set; } = string.Empty;
    public long? AttendanceId { get; set; }
    public string? Status { get; set; }
    public DateTime? CheckIn { get; set; }
    public DateTime? CheckOut { get; set; }
    public decimal? WorkedHours { get; set; }
    public string? Remarks { get; set; }
}

public class AttendanceBulkResultDto
{
    public DateTime Date { get; set; }
    public int Saved { get; set; }
}

/// <summary>Normalized attendance values handed to the repository.</summary>
public class AttendanceUpsertModel
{
    public long EmployeeId { get; set; }
    public DateTime Date { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime? CheckIn { get; set; }
    public DateTime? CheckOut { get; set; }
    public decimal WorkedHours { get; set; }
    public string? Remarks { get; set; }
}

public class LeaveRequestDto
{
    public long Id { get; set; }
    public long EmployeeId { get; set; }
    public string EmployeeCode { get; set; } = string.Empty;
    public string EmployeeName { get; set; } = string.Empty;
    public string LeaveType { get; set; } = string.Empty;
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
    public decimal Days { get; set; }
    public string? Reason { get; set; }
    public string Status { get; set; } = string.Empty;
    public long? ApprovedBy { get; set; }
    public string? ApprovedByName { get; set; }
    public DateTime? ApprovedDate { get; set; }
    public string? RejectionReason { get; set; }
    public DateTime CreatedDate { get; set; }
}

public class SalaryDto
{
    public long Id { get; set; }
    public long EmployeeId { get; set; }
    public string EmployeeCode { get; set; } = string.Empty;
    public string EmployeeName { get; set; } = string.Empty;
    public string? DesignationName { get; set; }
    public int SalaryMonth { get; set; }
    public int SalaryYear { get; set; }
    public decimal WorkingDays { get; set; }
    public decimal PresentDays { get; set; }
    public decimal LeaveDays { get; set; }
    public decimal BasicAmount { get; set; }
    public decimal Allowances { get; set; }
    public decimal Deductions { get; set; }
    public decimal NetAmount { get; set; }
    public string Status { get; set; } = string.Empty;
    public long? PaymentModeId { get; set; }
    public string? PaymentModeName { get; set; }
    public string? ReferenceNo { get; set; }
    public DateTime? PaidDate { get; set; }
    public string? Remarks { get; set; }
}

public class SalaryGenerateResultDto
{
    public int Generated { get; set; }
    public int AlreadyExisting { get; set; }
    public int SalaryMonth { get; set; }
    public int SalaryYear { get; set; }
}
