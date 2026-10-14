using Dairy.Application.Requests.Maintenance;
using FluentValidation;

namespace Dairy.Application.Validators.Maintenance;

public abstract class EquipmentRequestRules<T> : AbstractValidator<T> where T : CreateEquipmentRequest
{
    protected EquipmentRequestRules()
    {
        RuleFor(x => x.EquipmentCode).NotEmpty().WithMessage("Equipment code is required.").MaximumLength(30);
        RuleFor(x => x.Name).NotEmpty().WithMessage("Name is required.").MaximumLength(150);
        RuleFor(x => x.Category).MaximumLength(50);
        RuleFor(x => x.Make).MaximumLength(100);
        RuleFor(x => x.Model).MaximumLength(100);
        RuleFor(x => x.SerialNumber).MaximumLength(100);
        RuleFor(x => x.PurchaseCost).GreaterThanOrEqualTo(0).WithMessage("Purchase cost cannot be negative.");
        RuleFor(x => x.Location).MaximumLength(100);
        RuleFor(x => x.BranchId).GreaterThan(0).When(x => x.BranchId.HasValue);
        RuleFor(x => x.CollectionCenterId).GreaterThan(0).When(x => x.CollectionCenterId.HasValue);
        RuleFor(x => x.VendorName).MaximumLength(150);
        RuleFor(x => x.Remarks).MaximumLength(500);
    }
}

public class CreateEquipmentRequestValidator : EquipmentRequestRules<CreateEquipmentRequest> { }

public class UpdateEquipmentRequestValidator : EquipmentRequestRules<UpdateEquipmentRequest>
{
    public UpdateEquipmentRequestValidator()
    {
        RuleFor(x => x.Status).MustBeOneOf("Operational", "UnderMaintenance", "Retired");
    }
}

public class CreateMaintenanceScheduleRequestValidator : AbstractValidator<CreateMaintenanceScheduleRequest>
{
    public CreateMaintenanceScheduleRequestValidator()
    {
        RuleFor(x => x.EquipmentId).GreaterThan(0).WithMessage("Equipment is required.");
        RuleFor(x => x.TaskName).NotEmpty().WithMessage("Task name is required.").MaximumLength(150);
        RuleFor(x => x.Description).MaximumLength(500);
        RuleFor(x => x.FrequencyDays).InclusiveBetween(1, 3650).WithMessage("Frequency must be between 1 and 3650 days.");
        RuleFor(x => x.AlertBeforeDays).InclusiveBetween(0, 365).WithMessage("Alert days must be between 0 and 365.");
        RuleFor(x => x.AssignedEmployeeId).GreaterThan(0).When(x => x.AssignedEmployeeId.HasValue);
    }
}

public class UpdateMaintenanceScheduleRequestValidator : AbstractValidator<UpdateMaintenanceScheduleRequest>
{
    public UpdateMaintenanceScheduleRequestValidator()
    {
        RuleFor(x => x.TaskName).NotEmpty().WithMessage("Task name is required.").MaximumLength(150);
        RuleFor(x => x.Description).MaximumLength(500);
        RuleFor(x => x.FrequencyDays).InclusiveBetween(1, 3650).WithMessage("Frequency must be between 1 and 3650 days.");
        RuleFor(x => x.AlertBeforeDays).InclusiveBetween(0, 365).WithMessage("Alert days must be between 0 and 365.");
        RuleFor(x => x.AssignedEmployeeId).GreaterThan(0).When(x => x.AssignedEmployeeId.HasValue);
        RuleFor(x => x.Status).MustBeOneOf("Active", "Paused");
    }
}

public class CreateMaintenanceRecordRequestValidator : AbstractValidator<CreateMaintenanceRecordRequest>
{
    public CreateMaintenanceRecordRequestValidator()
    {
        RuleFor(x => x.EquipmentId).GreaterThan(0).WithMessage("Equipment is required.");
        RuleFor(x => x.MaintenanceScheduleId).GreaterThan(0).When(x => x.MaintenanceScheduleId.HasValue);
        RuleFor(x => x.MaintenanceDate).LessThan(DateTime.Today.AddDays(1)).When(x => x.MaintenanceDate.HasValue)
            .WithMessage("Maintenance date cannot be in the future.");
        RuleFor(x => x.Description).NotEmpty().WithMessage("A maintenance description is required.").MaximumLength(500);
        RuleFor(x => x.VendorName).MaximumLength(150);
        RuleFor(x => x.Cost).GreaterThanOrEqualTo(0).WithMessage("Cost cannot be negative.");
        RuleFor(x => x.MaintenanceType).MustBeOneOf("Preventive", "Corrective", "Breakdown");
        RuleFor(x => x.PerformedBy).MaximumLength(100);
        RuleFor(x => x.DowntimeHours).InclusiveBetween(0m, 9999m).When(x => x.DowntimeHours.HasValue);
        RuleFor(x => x.InvoiceNo).MaximumLength(50);
        RuleFor(x => x.Remarks).MaximumLength(255);
    }
}
