using Dairy.Application.Common;
using Dairy.Application.Requests.Hr;
using Dairy.Domain.Constants;
using FluentValidation;

namespace Dairy.Application.Validators.Hr;

public class CreateDesignationRequestValidator : AbstractValidator<CreateDesignationRequest>
{
    public CreateDesignationRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().WithMessage("Name is required.").MaximumLength(100);
        RuleFor(x => x.Department).MaximumLength(100);
        RuleFor(x => x.Description).MaximumLength(255);
    }
}

public class UpdateDesignationRequestValidator : AbstractValidator<UpdateDesignationRequest>
{
    public UpdateDesignationRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().WithMessage("Name is required.").MaximumLength(100);
        RuleFor(x => x.Department).MaximumLength(100);
        RuleFor(x => x.Description).MaximumLength(255);
    }
}

public abstract class EmployeeRequestValidatorBase<T> : AbstractValidator<T> where T : EmployeeRequestBase
{
    protected EmployeeRequestValidatorBase()
    {
        RuleFor(x => x.FullName).NotEmpty().WithMessage("Full name is required.").MaximumLength(150);
        RuleFor(x => x.Gender).MustBeOneOf("Male", "Female", "Other");
        RuleFor(x => x.Mobile).MustBeValidMobile();
        RuleFor(x => x.Email).EmailAddress().WithMessage("Email is not valid.").MaximumLength(150)
            .When(x => !string.IsNullOrWhiteSpace(x.Email));
        RuleFor(x => x.AddressLine).MaximumLength(255);
        RuleFor(x => x.City).MaximumLength(100);
        RuleFor(x => x.DesignationId).GreaterThan(0).WithMessage("Designation is required.");
        RuleFor(x => x.BasicSalary).GreaterThanOrEqualTo(0).WithMessage("Basic salary cannot be negative.");
        RuleFor(x => x.DefaultAllowances).GreaterThanOrEqualTo(0).WithMessage("Allowances cannot be negative.");
        RuleFor(x => x.DefaultDeductions).GreaterThanOrEqualTo(0).WithMessage("Deductions cannot be negative.");
        RuleFor(x => x.BankName).MaximumLength(100);
        RuleFor(x => x.BankAccountNo).MaximumLength(30);
        RuleFor(x => x.IfscCode).MustBeValidIfsc();
        RuleFor(x => x.AadhaarNo).Matches("^[0-9]{12}$").WithMessage("Aadhaar number must be 12 digits.")
            .When(x => !string.IsNullOrWhiteSpace(x.AadhaarNo));
        RuleFor(x => x.PanNo).MaximumLength(15);
        RuleFor(x => x.EmergencyContact).MaximumLength(150);
    }
}

public class CreateEmployeeRequestValidator : EmployeeRequestValidatorBase<CreateEmployeeRequest>
{
    public CreateEmployeeRequestValidator()
    {
        RuleFor(x => x.EmployeeCode).MaximumLength(30);
    }
}

public class UpdateEmployeeRequestValidator : EmployeeRequestValidatorBase<UpdateEmployeeRequest>
{
}

public class DeactivateEmployeeRequestValidator : AbstractValidator<DeactivateEmployeeRequest>
{
    public DeactivateEmployeeRequestValidator()
    {
        RuleFor(x => x.Status).MustBeOneOf("Inactive", "Terminated");
    }
}

public class UpsertAttendanceRequestValidator : AbstractValidator<UpsertAttendanceRequest>
{
    public UpsertAttendanceRequestValidator()
    {
        RuleFor(x => x.EmployeeId).GreaterThan(0).WithMessage("Employee is required.");
        RuleFor(x => x.Date).NotEmpty().WithMessage("Date is required.");
        RuleFor(x => x.Status).Must(AttendanceRules.IsValidStatus).WithMessage(AttendanceRules.StatusMessage);
        RuleFor(x => x.CheckIn).Must((r, v) => B5Text.TryParseDayTime(v, r.Date, out _))
            .WithMessage("Check-in must be a valid time (HH:mm) or date-time.");
        RuleFor(x => x.CheckOut).Must((r, v) => B5Text.TryParseDayTime(v, r.Date, out _))
            .WithMessage("Check-out must be a valid time (HH:mm) or date-time.");
        RuleFor(x => x.Remarks).MaximumLength(255);
    }
}

public class AttendanceEntryRequestValidator : AbstractValidator<AttendanceEntryRequest>
{
    public AttendanceEntryRequestValidator()
    {
        RuleFor(x => x.EmployeeId).GreaterThan(0).WithMessage("Employee is required.");
        RuleFor(x => x.Status).Must(AttendanceRules.IsValidStatus).WithMessage(AttendanceRules.StatusMessage);
        RuleFor(x => x.Remarks).MaximumLength(255);
    }
}

public class BulkAttendanceRequestValidator : AbstractValidator<BulkAttendanceRequest>
{
    public BulkAttendanceRequestValidator()
    {
        RuleFor(x => x.Date).NotEmpty().WithMessage("Date is required.");
        RuleFor(x => x.Entries).NotEmpty().WithMessage("At least one entry is required.")
            .Must(e => e.Count <= 1000).WithMessage("At most 1000 entries per request.");
        RuleForEach(x => x.Entries).SetValidator(new AttendanceEntryRequestValidator());
        RuleForEach(x => x.Entries).Must((r, e) => B5Text.TryParseDayTime(e.CheckIn, r.Date, out _) && B5Text.TryParseDayTime(e.CheckOut, r.Date, out _))
            .WithMessage("Check-in/check-out must be a valid time (HH:mm) or date-time.");
    }
}

public static class AttendanceRules
{
    public static readonly string[] Statuses =
    {
        AttendanceStatus.Present, AttendanceStatus.Absent, AttendanceStatus.HalfDay, AttendanceStatus.Leave
    };

    public static string StatusMessage => "Status must be one of: " + string.Join(", ", Statuses) + ".";

    public static bool IsValidStatus(string? status) => status != null && Statuses.Contains(status);
}

public class CreateLeaveRequestValidator : AbstractValidator<CreateLeaveRequest>
{
    private static readonly string[] LeaveTypes = { "Casual", "Sick", "Earned", "Unpaid", "Other" };

    public CreateLeaveRequestValidator()
    {
        RuleFor(x => x.LeaveType).Must(t => t != null && LeaveTypes.Contains(t))
            .WithMessage("Leave type must be one of: " + string.Join(", ", LeaveTypes) + ".");
        RuleFor(x => x.FromDate).NotEmpty().WithMessage("From date is required.");
        RuleFor(x => x.ToDate).NotEmpty().WithMessage("To date is required.")
            .GreaterThanOrEqualTo(x => x.FromDate).WithMessage("To date cannot be before from date.");
        RuleFor(x => x.Days)
            .Must(d => d!.Value > 0 && d.Value % 0.5m == 0).WithMessage("Days must be a positive multiple of 0.5.")
            .When(x => x.Days.HasValue);
        RuleFor(x => x.Reason).MaximumLength(500);
    }
}

public class RejectLeaveRequestValidator : AbstractValidator<RejectLeaveRequest>
{
    public RejectLeaveRequestValidator()
    {
        RuleFor(x => x.Reason).MaximumLength(255);
    }
}

public class GenerateSalariesRequestValidator : AbstractValidator<GenerateSalariesRequest>
{
    public GenerateSalariesRequestValidator()
    {
        RuleFor(x => x.Month).InclusiveBetween(1, 12).WithMessage("Month must be between 1 and 12.");
        RuleFor(x => x.Year).InclusiveBetween(2000, 2100).WithMessage("Year must be between 2000 and 2100.");
    }
}

public class PaySalaryRequestValidator : AbstractValidator<PaySalaryRequest>
{
    public PaySalaryRequestValidator()
    {
        RuleFor(x => x.PaymentModeId).GreaterThan(0).WithMessage("Payment mode is required.");
    }
}
