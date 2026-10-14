using Dairy.Application.Requests.Billing;
using FluentValidation;

namespace Dairy.Application.Validators.Billing;

public class GenerateInvoiceRequestValidator : AbstractValidator<GenerateInvoiceRequest>
{
    public GenerateInvoiceRequestValidator()
    {
        RuleFor(x => x.CustomerId).GreaterThan(0).WithMessage("Customer is required.");

        When(x => x.SalesOrderIds is null || x.SalesOrderIds.Count == 0, () =>
        {
            RuleFor(x => x.PeriodFrom).NotNull().WithMessage("Billing period start is required when no orders are selected.");
            RuleFor(x => x.PeriodTo).NotNull().WithMessage("Billing period end is required when no orders are selected.");
            RuleFor(x => x.PeriodTo).GreaterThanOrEqualTo(x => x.PeriodFrom!.Value)
                .When(x => x.PeriodFrom.HasValue && x.PeriodTo.HasValue)
                .WithMessage("Billing period end cannot be before the start.");
        });

        RuleFor(x => x.SalesOrderIds!).Must(ids => ids.Count <= 500).When(x => x.SalesOrderIds is not null)
            .WithMessage("At most 500 orders can be invoiced at once.");
    }
}

public class CancelInvoiceRequestValidator : AbstractValidator<CancelInvoiceRequest>
{
    public CancelInvoiceRequestValidator()
    {
        RuleFor(x => x.Reason).MaximumLength(255);
    }
}

public class RecordCustomerPaymentRequestValidator : AbstractValidator<RecordCustomerPaymentRequest>
{
    public RecordCustomerPaymentRequestValidator()
    {
        RuleFor(x => x.CustomerId).GreaterThan(0).WithMessage("Customer is required.");
        RuleFor(x => x.Amount).GreaterThan(0).WithMessage("Payment amount must be greater than zero.")
            .Must(a => decimal.Round(a, 2) == a).WithMessage("Amount can have at most 2 decimal places.");
        RuleFor(x => x.PaymentModeId).GreaterThan(0).WithMessage("Payment mode is required.");
        RuleFor(x => x.PaymentDate).Must(d => d!.Value.Date <= DateTime.Today)
            .When(x => x.PaymentDate.HasValue).WithMessage("Payment date cannot be in the future.");
        RuleFor(x => x.ReferenceNo).MaximumLength(50);
        RuleFor(x => x.Remarks).MaximumLength(255);
    }
}
