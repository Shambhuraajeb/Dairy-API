using Dairy.Application.BusinessRules;
using Dairy.Application.Requests.Subscriptions;
using FluentValidation;

namespace Dairy.Application.Validators.Subscriptions;

public class CreateSubscriptionPlanRequestValidator : AbstractValidator<CreateSubscriptionPlanRequest>
{
    public CreateSubscriptionPlanRequestValidator()
    {
        RuleFor(x => x.PlanCode).MaximumLength(30);
        RuleFor(x => x.Name).NotEmpty().WithMessage("Name is required.").MaximumLength(150);
        RuleFor(x => x.Description).MaximumLength(255);
        RuleFor(x => x.Frequency).NotEmpty().MustBeOneOf(SalesValues.Frequencies);
        RuleFor(x => x.DaysOfWeek).MaximumLength(30)
            .Must(SalesValues.IsValidWeekdayList).WithMessage("Days of week must be a comma list such as Mon,Wed,Fri.");
        RuleFor(x => x.DefaultQuantity).GreaterThan(0).WithMessage("Default quantity must be greater than zero.");
        RuleFor(x => x.PricePerUnit).GreaterThanOrEqualTo(0m).When(x => x.PricePerUnit.HasValue);
        RuleFor(x => x.DiscountPercent).InclusiveBetween(0, 100);
        RuleFor(x => x.BillingCycle).NotEmpty().MustBeOneOf(SalesValues.BillingCycles);
        RuleFor(x => x.MinimumDays).GreaterThanOrEqualTo(0);
    }
}

public class UpdateSubscriptionPlanRequestValidator : AbstractValidator<UpdateSubscriptionPlanRequest>
{
    public UpdateSubscriptionPlanRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().WithMessage("Name is required.").MaximumLength(150);
        RuleFor(x => x.Description).MaximumLength(255);
        RuleFor(x => x.Frequency).NotEmpty().MustBeOneOf(SalesValues.Frequencies);
        RuleFor(x => x.DaysOfWeek).MaximumLength(30)
            .Must(SalesValues.IsValidWeekdayList).WithMessage("Days of week must be a comma list such as Mon,Wed,Fri.");
        RuleFor(x => x.DefaultQuantity).GreaterThan(0).WithMessage("Default quantity must be greater than zero.");
        RuleFor(x => x.PricePerUnit).GreaterThanOrEqualTo(0m).When(x => x.PricePerUnit.HasValue);
        RuleFor(x => x.DiscountPercent).InclusiveBetween(0, 100);
        RuleFor(x => x.BillingCycle).NotEmpty().MustBeOneOf(SalesValues.BillingCycles);
        RuleFor(x => x.MinimumDays).GreaterThanOrEqualTo(0);
    }
}

public class CreateSubscriptionRequestValidator : AbstractValidator<CreateSubscriptionRequest>
{
    public CreateSubscriptionRequestValidator()
    {
        RuleFor(x => x.CustomerId).GreaterThan(0).WithMessage("Customer is required.");
        RuleFor(x => x.SubscriptionPlanId).GreaterThan(0).WithMessage("Plan is required.");
        RuleFor(x => x.CustomerAddressId).GreaterThan(0).WithMessage("Delivery address is required.");
        RuleFor(x => x.Quantity).GreaterThan(0m).When(x => x.Quantity.HasValue && x.Quantity.Value != 0m);
        RuleFor(x => x.DeliverySlot).NotEmpty().MustBeOneOf(SalesValues.Slots);
        RuleFor(x => x.EndDate).GreaterThanOrEqualTo(x => x.StartDate ?? DateTime.Today)
            .When(x => x.EndDate.HasValue).WithMessage("End date cannot be before the start date.");
    }
}

public class UpdateSubscriptionRequestValidator : AbstractValidator<UpdateSubscriptionRequest>
{
    public UpdateSubscriptionRequestValidator()
    {
        RuleFor(x => x.Quantity).GreaterThan(0m).When(x => x.Quantity.HasValue);
        RuleFor(x => x.DeliverySlot).MustBeOneOf(SalesValues.Slots);
        RuleFor(x => x.CustomerAddressId).GreaterThan(0).When(x => x.CustomerAddressId.HasValue);
        RuleFor(x => x.Remarks).MaximumLength(255);
    }
}

public class PauseSubscriptionRequestValidator : AbstractValidator<PauseSubscriptionRequest>
{
    public PauseSubscriptionRequestValidator()
    {
        RuleFor(x => x.PauseTo).GreaterThanOrEqualTo(x => x.PauseFrom ?? DateTime.Today)
            .When(x => x.PauseTo.HasValue).WithMessage("Pause end date cannot be before the pause start date.");
        RuleFor(x => x.Reason).MaximumLength(255);
    }
}

public class CancelSubscriptionRequestValidator : AbstractValidator<CancelSubscriptionRequest>
{
    public CancelSubscriptionRequestValidator()
    {
        RuleFor(x => x.Reason).MaximumLength(255);
    }
}
