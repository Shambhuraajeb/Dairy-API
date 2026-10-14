using Dairy.Application.Requests.Settlement;
using FluentValidation;

namespace Dairy.Application.Validators.Settlement;

public class SettlementPreviewQueryValidator : AbstractValidator<SettlementPreviewQuery>
{
    public SettlementPreviewQueryValidator()
    {
        RuleFor(x => x.FarmerId).GreaterThan(0L).WithMessage("Farmer is required.");
        RuleFor(x => x.From).Must(d => d.Year >= 2000).WithMessage("'From' date is required.");
        RuleFor(x => x.To).Must(d => d.Year >= 2000).WithMessage("'To' date is required.");
        RuleFor(x => x.To).Must((r, to) => to.Date >= r.From.Date)
            .WithMessage("'To' date must be on or after the 'From' date.");
        RuleFor(x => x.To).Must((r, to) => (to.Date - r.From.Date).TotalDays <= 366)
            .WithMessage("The settlement period cannot exceed one year.");
    }
}

public class PendingFarmersQueryValidator : AbstractValidator<PendingFarmersQuery>
{
    public PendingFarmersQueryValidator()
    {
        RuleFor(x => x.From).Must(d => d.Year >= 2000).WithMessage("'From' date is required.");
        RuleFor(x => x.To).Must(d => d.Year >= 2000).WithMessage("'To' date is required.");
        RuleFor(x => x.To).Must((r, to) => to.Date >= r.From.Date)
            .WithMessage("'To' date must be on or after the 'From' date.");
        RuleFor(x => x.To).Must((r, to) => (to.Date - r.From.Date).TotalDays <= 366)
            .WithMessage("The settlement period cannot exceed one year.");
    }
}

public class CreateSettlementRequestValidator : AbstractValidator<CreateSettlementRequest>
{
    public CreateSettlementRequestValidator()
    {
        RuleFor(x => x.FarmerId).GreaterThan(0L).WithMessage("Farmer is required.");
        RuleFor(x => x.From).Must(d => d.Year >= 2000).WithMessage("'From' date is required.");
        RuleFor(x => x.To).Must(d => d.Year >= 2000).WithMessage("'To' date is required.");
        RuleFor(x => x.To).Must((r, to) => to.Date >= r.From.Date)
            .WithMessage("'To' date must be on or after the 'From' date.");
        RuleFor(x => x.To).Must((r, to) => (to.Date - r.From.Date).TotalDays <= 366)
            .WithMessage("The settlement period cannot exceed one year.");
        RuleFor(x => x.BonusAmount).InclusiveBetween(0m, 100000000m).WithMessage("Bonus must be zero or more.");
        RuleFor(x => x.PenaltyAmount).InclusiveBetween(0m, 100000000m).WithMessage("Penalty must be zero or more.");
        RuleFor(x => x.AdvanceDeduction).InclusiveBetween(0m, 100000000m).WithMessage("Advance deduction must be zero or more.");
        RuleFor(x => x.OtherDeduction).InclusiveBetween(0m, 100000000m).WithMessage("Other deduction must be zero or more.");
        RuleFor(x => x.OtherDeductionRemarks)
            .NotEmpty().When(x => x.OtherDeduction.GetValueOrDefault() > 0)
            .WithMessage("Remarks are required for other deductions.");
        RuleFor(x => x.OtherDeductionRemarks).MaximumLength(255);
        RuleFor(x => x.PaymentModeId).GreaterThan(0L).WithMessage("Payment mode is invalid.");
        RuleFor(x => x.ReferenceNo).MaximumLength(50);
        RuleFor(x => x.Remarks).MaximumLength(255);
    }
}

public class PaySettlementRequestValidator : AbstractValidator<PaySettlementRequest>
{
    public PaySettlementRequestValidator()
    {
        RuleFor(x => x.PaymentModeId).GreaterThan(0L).WithMessage("Payment mode is invalid.");
        RuleFor(x => x.PaidAmount).GreaterThan(0m).WithMessage("Paid amount must be greater than zero.");
        RuleFor(x => x.ReferenceNo).MaximumLength(50);
    }
}

public class CancelSettlementRequestValidator : AbstractValidator<CancelSettlementRequest>
{
    public CancelSettlementRequestValidator()
    {
        RuleFor(x => x.Reason).NotEmpty().WithMessage("A cancellation reason is required.").MaximumLength(255);
    }
}

public class FarmerPaymentQueryValidator : AbstractValidator<FarmerPaymentQuery>
{
    public FarmerPaymentQueryValidator()
    {
        RuleFor(x => x.Status)
            .Must(v => v is not null && new[] { "Draft", "Approved", "Paid", "Cancelled" }.Contains(v.Trim(), StringComparer.OrdinalIgnoreCase))
            .When(x => !string.IsNullOrWhiteSpace(x.Status))
            .WithMessage("Status must be Draft, Approved, Paid or Cancelled.");
        RuleFor(x => x.To)
            .Must((q, to) => !q.From.HasValue || !to.HasValue || to.Value.Date >= q.From.Value.Date)
            .WithMessage("'To' date must be on or after the 'From' date.");
    }
}
