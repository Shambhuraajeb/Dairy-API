using Dairy.Application.Requests.Collection;
using FluentValidation;

namespace Dairy.Application.Validators.Collection;

internal static class CollectionRules
{
    public static readonly string[] MilkTypes = { "Cow", "Buffalo", "Mixed" };
    public static readonly string[] CollectionStatuses = { "Pending", "Approved", "Rejected" };
    public static readonly string[] SettlementStatuses = { "Unsettled", "Settled" };
    public static readonly string[] ReasonCodes = { "LowFat", "LowSnf", "Adulterated", "HighTemperature", "Sour", "Other" };
    public static readonly string[] TankStatuses = { "Active", "Maintenance", "Inactive" };
    public static readonly string[] TankTxnTypes = { "In", "Out", "Adjust" };

    public static bool IsOneOf(string? value, string[] allowed) =>
        value is not null && allowed.Contains(value.Trim(), StringComparer.OrdinalIgnoreCase);
}

public class MilkCollectionQueryValidator : AbstractValidator<MilkCollectionQuery>
{
    public MilkCollectionQueryValidator()
    {
        RuleFor(x => x.Status)
            .Must(v => CollectionRules.IsOneOf(v, CollectionRules.CollectionStatuses))
            .When(x => !string.IsNullOrWhiteSpace(x.Status))
            .WithMessage("Status must be Pending, Approved or Rejected.");
        RuleFor(x => x.SettlementStatus)
            .Must(v => CollectionRules.IsOneOf(v, CollectionRules.SettlementStatuses))
            .When(x => !string.IsNullOrWhiteSpace(x.SettlementStatus))
            .WithMessage("Settlement status must be Unsettled or Settled.");
        RuleFor(x => x.To)
            .Must((q, to) => !q.From.HasValue || !to.HasValue || to.Value.Date >= q.From.Value.Date)
            .WithMessage("'To' date must be on or after the 'From' date.");
    }
}

public class MilkRejectionQueryValidator : AbstractValidator<MilkRejectionQuery>
{
    public MilkRejectionQueryValidator()
    {
        RuleFor(x => x.ReasonCode)
            .Must(v => CollectionRules.IsOneOf(v, CollectionRules.ReasonCodes))
            .When(x => !string.IsNullOrWhiteSpace(x.ReasonCode))
            .WithMessage("Reason code must be LowFat, LowSnf, Adulterated, HighTemperature, Sour or Other.");
        RuleFor(x => x.To)
            .Must((q, to) => !q.From.HasValue || !to.HasValue || to.Value.Date >= q.From.Value.Date)
            .WithMessage("'To' date must be on or after the 'From' date.");
    }
}

public class RatePreviewQueryValidator : AbstractValidator<RatePreviewQuery>
{
    public RatePreviewQueryValidator()
    {
        RuleFor(x => x.CenterId).GreaterThan(0).WithMessage("Collection center is required.");
        RuleFor(x => x.Fat).InclusiveBetween(0.1m, 25m).WithMessage("FAT must be between 0.1 and 25.");
        RuleFor(x => x.Snf).InclusiveBetween(0.1m, 25m).WithMessage("SNF must be between 0.1 and 25.");
        RuleFor(x => x.Clr).InclusiveBetween(0m, 60m).WithMessage("CLR must be between 0 and 60.");
        RuleFor(x => x.Quantity).GreaterThanOrEqualTo(0m).WithMessage("Quantity cannot be negative.");
        RuleFor(x => x.MilkType)
            .Must(v => CollectionRules.IsOneOf(v, CollectionRules.MilkTypes))
            .When(x => !string.IsNullOrWhiteSpace(x.MilkType))
            .WithMessage("Milk type must be Cow, Buffalo or Mixed.");
    }
}

public class CreateMilkCollectionRequestValidator : AbstractValidator<CreateMilkCollectionRequest>
{
    public CreateMilkCollectionRequestValidator()
    {
        RuleFor(x => x.ShiftId).GreaterThan(0).WithMessage("Shift is required.");
        RuleFor(x => x.CollectionCenterId).GreaterThan(0).WithMessage("Collection center is required.");
        RuleFor(x => x.FarmerId).GreaterThan(0).WithMessage("Farmer is required.");
        RuleFor(x => x.QuantityLiters)
            .GreaterThan(0m).WithMessage("Quantity must be greater than zero.")
            .LessThanOrEqualTo(100000m).WithMessage("Quantity is too large.");
        RuleFor(x => x.Fat).InclusiveBetween(0.1m, 25m).WithMessage("FAT must be between 0.1 and 25.");
        RuleFor(x => x.Snf).InclusiveBetween(0.1m, 25m).WithMessage("SNF must be between 0.1 and 25.");
        RuleFor(x => x.Clr).InclusiveBetween(0m, 60m).WithMessage("CLR must be between 0 and 60.");
        RuleFor(x => x.Temperature).InclusiveBetween(-10m, 100m).WithMessage("Temperature must be between -10 and 100.");
        RuleFor(x => x.Acidity).InclusiveBetween(0m, 100m).WithMessage("Acidity must be between 0 and 100.");
        RuleFor(x => x.CanCount).GreaterThanOrEqualTo(0).WithMessage("Can count cannot be negative.");
        RuleFor(x => x.MilkTankId).GreaterThan(0L).WithMessage("Milk tank is invalid.");
        RuleFor(x => x.MilkType)
            .Must(v => CollectionRules.IsOneOf(v, CollectionRules.MilkTypes))
            .When(x => !string.IsNullOrWhiteSpace(x.MilkType))
            .WithMessage("Milk type must be Cow, Buffalo or Mixed.");
        RuleFor(x => x.CollectionDate)
            .Must(d => d!.Value.Date <= DateTime.Today)
            .When(x => x.CollectionDate.HasValue)
            .WithMessage("Collection date cannot be in the future.");
        RuleFor(x => x.Remarks).MaximumLength(255);
    }
}

public class AdjustMilkCollectionRequestValidator : AbstractValidator<AdjustMilkCollectionRequest>
{
    public AdjustMilkCollectionRequestValidator()
    {
        RuleFor(x => x.Reason).NotEmpty().WithMessage("An adjustment reason is required.")
            .MaximumLength(255);
        RuleFor(x => x.QuantityLiters)
            .Must((r, _) => r.QuantityLiters.HasValue || r.Fat.HasValue || r.Snf.HasValue || r.Clr.HasValue)
            .WithMessage("Provide at least one value to adjust (quantityLiters, fat, snf or clr).");
        RuleFor(x => x.QuantityLiters)
            .GreaterThan(0m).WithMessage("Quantity must be greater than zero.")
            .LessThanOrEqualTo(100000m).WithMessage("Quantity is too large.");
        RuleFor(x => x.Fat).InclusiveBetween(0.1m, 25m).WithMessage("FAT must be between 0.1 and 25.");
        RuleFor(x => x.Snf).InclusiveBetween(0.1m, 25m).WithMessage("SNF must be between 0.1 and 25.");
        RuleFor(x => x.Clr).InclusiveBetween(0m, 60m).WithMessage("CLR must be between 0 and 60.");
    }
}

public class RejectMilkCollectionRequestValidator : AbstractValidator<RejectMilkCollectionRequest>
{
    public RejectMilkCollectionRequestValidator()
    {
        RuleFor(x => x.Reason).NotEmpty().WithMessage("A rejection reason is required.").MaximumLength(255);
        RuleFor(x => x.QuantityLiters).GreaterThan(0m).WithMessage("Rejected quantity must be greater than zero.");
    }
}

public class BulkApproveRequestValidator : AbstractValidator<BulkApproveRequest>
{
    public BulkApproveRequestValidator()
    {
        RuleFor(x => x.Ids).NotEmpty().WithMessage("Select at least one collection.")
            .Must(ids => ids.Count <= 200).WithMessage("At most 200 collections can be approved at once.");
        RuleForEach(x => x.Ids).GreaterThan(0L).WithMessage("Collection id is invalid.");
    }
}

public class RecordQualityRequestValidator : AbstractValidator<RecordQualityRequest>
{
    public RecordQualityRequestValidator()
    {
        RuleFor(x => x.Fat).InclusiveBetween(0.1m, 25m).WithMessage("FAT must be between 0.1 and 25.");
        RuleFor(x => x.Snf).InclusiveBetween(0.1m, 25m).WithMessage("SNF must be between 0.1 and 25.");
        RuleFor(x => x.Clr).InclusiveBetween(0m, 60m).WithMessage("CLR must be between 0 and 60.");
        RuleFor(x => x.Protein).InclusiveBetween(0m, 100m).WithMessage("Protein must be between 0 and 100.");
        RuleFor(x => x.Lactose).InclusiveBetween(0m, 100m).WithMessage("Lactose must be between 0 and 100.");
        RuleFor(x => x.Temperature).InclusiveBetween(-10m, 100m).WithMessage("Temperature must be between -10 and 100.");
        RuleFor(x => x.Acidity).InclusiveBetween(0m, 100m).WithMessage("Acidity must be between 0 and 100.");
        RuleFor(x => x.Result)
            .Must(v => CollectionRules.IsOneOf(v, new[] { "Pass", "Fail" }))
            .When(x => !string.IsNullOrWhiteSpace(x.Result))
            .WithMessage("Result must be Pass or Fail.");
        RuleFor(x => x.OtherAdulteration).MaximumLength(150);
        RuleFor(x => x.FailureReason).MaximumLength(255);
        RuleFor(x => x.Remarks).MaximumLength(255);
    }
}

public class MilkTankQueryValidator : AbstractValidator<MilkTankQuery>
{
    public MilkTankQueryValidator()
    {
        RuleFor(x => x.Status)
            .Must(v => CollectionRules.IsOneOf(v, CollectionRules.TankStatuses))
            .When(x => !string.IsNullOrWhiteSpace(x.Status))
            .WithMessage("Status must be Active, Maintenance or Inactive.");
    }
}

public class TankTransactionQueryValidator : AbstractValidator<TankTransactionQuery>
{
    public TankTransactionQueryValidator()
    {
        RuleFor(x => x.Type)
            .Must(v => CollectionRules.IsOneOf(v, CollectionRules.TankTxnTypes))
            .When(x => !string.IsNullOrWhiteSpace(x.Type))
            .WithMessage("Type must be In, Out or Adjust.");
    }
}

public class CreateMilkTankRequestValidator : AbstractValidator<CreateMilkTankRequest>
{
    public CreateMilkTankRequestValidator()
    {
        RuleFor(x => x.Code).NotEmpty().WithMessage("Code is required.").MaximumLength(30);
        RuleFor(x => x.Name).NotEmpty().WithMessage("Name is required.").MaximumLength(100);
        RuleFor(x => x.CollectionCenterId).GreaterThan(0).WithMessage("Collection center is required.");
        RuleFor(x => x.CapacityLiters).GreaterThan(0m).LessThanOrEqualTo(1000000m)
            .WithMessage("Capacity must be between 0 and 1,000,000 liters.");
        RuleFor(x => x.MilkType)
            .Must(v => CollectionRules.IsOneOf(v, CollectionRules.MilkTypes))
            .When(x => !string.IsNullOrWhiteSpace(x.MilkType))
            .WithMessage("Milk type must be Cow, Buffalo or Mixed.");
        RuleFor(x => x.MinTemperature).InclusiveBetween(-50m, 100m);
        RuleFor(x => x.MaxTemperature).InclusiveBetween(-50m, 100m);
        RuleFor(x => x.CurrentTemperature).InclusiveBetween(-50m, 100m);
        RuleFor(x => x.MaxTemperature)
            .Must((r, max) => !r.MinTemperature.HasValue || !max.HasValue || max.Value >= r.MinTemperature.Value)
            .WithMessage("Max temperature must be greater than or equal to min temperature.");
    }
}

public class UpdateMilkTankRequestValidator : AbstractValidator<UpdateMilkTankRequest>
{
    public UpdateMilkTankRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().WithMessage("Name is required.").MaximumLength(100);
        RuleFor(x => x.CollectionCenterId).GreaterThan(0).WithMessage("Collection center is required.");
        RuleFor(x => x.CapacityLiters).GreaterThan(0m).LessThanOrEqualTo(1000000m)
            .WithMessage("Capacity must be between 0 and 1,000,000 liters.");
        RuleFor(x => x.MilkType)
            .Must(v => CollectionRules.IsOneOf(v, CollectionRules.MilkTypes))
            .When(x => !string.IsNullOrWhiteSpace(x.MilkType))
            .WithMessage("Milk type must be Cow, Buffalo or Mixed.");
        RuleFor(x => x.Status)
            .Must(v => CollectionRules.IsOneOf(v, CollectionRules.TankStatuses))
            .WithMessage("Status must be Active, Maintenance or Inactive.");
        RuleFor(x => x.MinTemperature).InclusiveBetween(-50m, 100m);
        RuleFor(x => x.MaxTemperature).InclusiveBetween(-50m, 100m);
        RuleFor(x => x.CurrentTemperature).InclusiveBetween(-50m, 100m);
        RuleFor(x => x.MaxTemperature)
            .Must((r, max) => !r.MinTemperature.HasValue || !max.HasValue || max.Value >= r.MinTemperature.Value)
            .WithMessage("Max temperature must be greater than or equal to min temperature.");
    }
}

public class CreateTankTransactionRequestValidator : AbstractValidator<CreateTankTransactionRequest>
{
    public CreateTankTransactionRequestValidator()
    {
        RuleFor(x => x.TransactionType)
            .Must(v => CollectionRules.IsOneOf(v, new[] { "Out", "Adjust" }))
            .WithMessage("Transaction type must be Out or Adjust.");
        RuleFor(x => x.Quantity).NotEqual(0m).WithMessage("Quantity cannot be zero.")
            .InclusiveBetween(-1000000m, 1000000m);
        RuleFor(x => x.Quantity).GreaterThan(0m)
            .When(x => string.Equals(x.TransactionType, "Out", StringComparison.OrdinalIgnoreCase))
            .WithMessage("Quantity must be greater than zero for an Out transaction.");
        RuleFor(x => x.Destination).MaximumLength(150);
        RuleFor(x => x.Remarks).MaximumLength(255);
    }
}
