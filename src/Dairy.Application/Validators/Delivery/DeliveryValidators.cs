using Dairy.Application.BusinessRules;
using Dairy.Application.Requests.Delivery;
using FluentValidation;

namespace Dairy.Application.Validators.Delivery;

public class CreateRouteRequestValidator : AbstractValidator<CreateRouteRequest>
{
    public CreateRouteRequestValidator()
    {
        RuleFor(x => x.Code).MaximumLength(30);
        RuleFor(x => x.Name).NotEmpty().WithMessage("Name is required.").MaximumLength(150);
        RuleFor(x => x.Description).MaximumLength(255);
        RuleFor(x => x.StartLocation).MaximumLength(150);
        RuleFor(x => x.EndLocation).MaximumLength(150);
        RuleFor(x => x.DistanceKm).GreaterThanOrEqualTo(0);
        RuleFor(x => x.EstimatedMinutes).GreaterThanOrEqualTo(0);
    }
}

public class UpdateRouteRequestValidator : AbstractValidator<UpdateRouteRequest>
{
    public UpdateRouteRequestValidator()
    {
        RuleFor(x => x.Code).NotEmpty().WithMessage("Code is required.").MaximumLength(30);
        RuleFor(x => x.Name).NotEmpty().WithMessage("Name is required.").MaximumLength(150);
        RuleFor(x => x.Description).MaximumLength(255);
        RuleFor(x => x.StartLocation).MaximumLength(150);
        RuleFor(x => x.EndLocation).MaximumLength(150);
        RuleFor(x => x.DistanceKm).GreaterThanOrEqualTo(0);
        RuleFor(x => x.EstimatedMinutes).GreaterThanOrEqualTo(0);
    }
}

public class CreateVehicleRequestValidator : AbstractValidator<CreateVehicleRequest>
{
    public CreateVehicleRequestValidator()
    {
        RuleFor(x => x.RegistrationNo).NotEmpty().WithMessage("Registration number is required.").MaximumLength(30);
        RuleFor(x => x.VehicleType).NotEmpty().MustBeOneOf(SalesValues.VehicleTypes);
        RuleFor(x => x.Make).MaximumLength(100);
        RuleFor(x => x.Model).MaximumLength(100);
        RuleFor(x => x.Capacity).GreaterThanOrEqualTo(0);
        RuleFor(x => x.CapacityUnit).NotEmpty().MaximumLength(20);
        RuleFor(x => x.FuelType).MaximumLength(20);
        RuleFor(x => x.Status).NotEmpty().MustBeOneOf(SalesValues.VehicleStatuses);
    }
}

public class UpdateVehicleRequestValidator : AbstractValidator<UpdateVehicleRequest>
{
    public UpdateVehicleRequestValidator()
    {
        RuleFor(x => x.RegistrationNo).NotEmpty().WithMessage("Registration number is required.").MaximumLength(30);
        RuleFor(x => x.VehicleType).NotEmpty().MustBeOneOf(SalesValues.VehicleTypes);
        RuleFor(x => x.Make).MaximumLength(100);
        RuleFor(x => x.Model).MaximumLength(100);
        RuleFor(x => x.Capacity).GreaterThanOrEqualTo(0);
        RuleFor(x => x.CapacityUnit).NotEmpty().MaximumLength(20);
        RuleFor(x => x.FuelType).MaximumLength(20);
        RuleFor(x => x.Status).NotEmpty().MustBeOneOf(SalesValues.VehicleStatuses);
    }
}

public class CreateDeliveryScheduleRequestValidator : AbstractValidator<CreateDeliveryScheduleRequest>
{
    public CreateDeliveryScheduleRequestValidator()
    {
        RuleFor(x => x.RouteId).GreaterThan(0).WithMessage("Route is required.");
        RuleFor(x => x.ScheduleDate).NotEmpty().WithMessage("Schedule date is required.");
        RuleFor(x => x.Slot).NotEmpty().MustBeOneOf(SalesValues.Slots);
        RuleFor(x => x.PlannedStartTime).Must(SalesValues.IsValidTime).WithMessage("Planned start time must be HH:mm (24h).");
        RuleFor(x => x.Remarks).MaximumLength(255);
    }
}

public class UpdateDeliveryScheduleRequestValidator : AbstractValidator<UpdateDeliveryScheduleRequest>
{
    public UpdateDeliveryScheduleRequestValidator()
    {
        RuleFor(x => x.ScheduleDate).NotEmpty().WithMessage("Schedule date is required.");
        RuleFor(x => x.Slot).NotEmpty().MustBeOneOf(SalesValues.Slots);
        RuleFor(x => x.PlannedStartTime).Must(SalesValues.IsValidTime).WithMessage("Planned start time must be HH:mm (24h).");
        RuleFor(x => x.Remarks).MaximumLength(255);
    }
}

public class CreateDeliveryRequestValidator : AbstractValidator<CreateDeliveryRequest>
{
    public CreateDeliveryRequestValidator()
    {
        RuleFor(x => x.SalesOrderId).GreaterThan(0).WithMessage("Order is required.");
    }
}

public class BulkDeliveryRequestValidator : AbstractValidator<BulkDeliveryRequest>
{
    public BulkDeliveryRequestValidator()
    {
        RuleFor(x => x.DeliveryScheduleId).GreaterThan(0).WithMessage("Delivery schedule is required.");
        RuleFor(x => x.SalesOrderIds).NotEmpty().WithMessage("Select at least one order.")
            .Must(ids => ids.Count <= 500).WithMessage("At most 500 orders can be scheduled at once.");
    }
}

public class ConfirmDeliveryRequestValidator : AbstractValidator<ConfirmDeliveryRequest>
{
    public ConfirmDeliveryRequestValidator()
    {
        RuleFor(x => x.ReceivedBy).MaximumLength(100);
        RuleFor(x => x.Remarks).MaximumLength(255);
        RuleFor(x => x.FailureReason).MaximumLength(255);
        RuleFor(x => x.FailureReason).NotEmpty().When(x => !x.IsDelivered).WithMessage("A failure reason is required when the delivery was not completed.");
        RuleFor(x => x.Latitude).InclusiveBetween(-90m, 90m).When(x => x.Latitude.HasValue);
        RuleFor(x => x.Longitude).InclusiveBetween(-180m, 180m).When(x => x.Longitude.HasValue);
    }
}
