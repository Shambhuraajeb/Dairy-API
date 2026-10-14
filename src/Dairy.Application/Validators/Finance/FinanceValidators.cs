using Dairy.Application.Requests.Finance;
using FluentValidation;

namespace Dairy.Application.Validators.Finance;

public class CreateExpenseCategoryRequestValidator : AbstractValidator<CreateExpenseCategoryRequest>
{
    public CreateExpenseCategoryRequestValidator()
    {
        RuleFor(x => x.Code).NotEmpty().WithMessage("Code is required.").MaximumLength(30);
        RuleFor(x => x.Name).NotEmpty().WithMessage("Name is required.").MaximumLength(100);
        RuleFor(x => x.Description).MaximumLength(255);
    }
}

public class UpdateExpenseCategoryRequestValidator : AbstractValidator<UpdateExpenseCategoryRequest>
{
    public UpdateExpenseCategoryRequestValidator()
    {
        RuleFor(x => x.Code).NotEmpty().WithMessage("Code is required.").MaximumLength(30);
        RuleFor(x => x.Name).NotEmpty().WithMessage("Name is required.").MaximumLength(100);
        RuleFor(x => x.Description).MaximumLength(255);
    }
}

public class CreateExpenseRequestValidator : AbstractValidator<CreateExpenseRequest>
{
    public CreateExpenseRequestValidator()
    {
        RuleFor(x => x.ExpenseCategoryId).GreaterThan(0).WithMessage("Expense category is required.");
        RuleFor(x => x.Amount).GreaterThan(0).WithMessage("Amount must be greater than zero.");
        RuleFor(x => x.ExpenseDate).LessThan(DateTime.Today.AddDays(1)).When(x => x.ExpenseDate.HasValue)
            .WithMessage("Expense date cannot be in the future.");
        RuleFor(x => x.PaymentModeId).GreaterThan(0).When(x => x.PaymentModeId.HasValue);
        RuleFor(x => x.BranchId).GreaterThan(0).When(x => x.BranchId.HasValue);
        RuleFor(x => x.Description).MaximumLength(500);
        RuleFor(x => x.Payee).MaximumLength(150);
        RuleFor(x => x.ReceiptNo).MaximumLength(50);
    }
}

public class ApproveExpenseRequestValidator : AbstractValidator<ApproveExpenseRequest>
{
    public ApproveExpenseRequestValidator()
    {
        RuleFor(x => x.Remarks).MaximumLength(255);
    }
}

public class RejectExpenseRequestValidator : AbstractValidator<RejectExpenseRequest>
{
    public RejectExpenseRequestValidator()
    {
        RuleFor(x => x.Reason).NotEmpty().WithMessage("A reason is required to reject an expense.").MaximumLength(255);
    }
}
