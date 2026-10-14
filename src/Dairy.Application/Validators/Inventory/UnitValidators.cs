using Dairy.Application.Requests.Inventory;
using FluentValidation;

namespace Dairy.Application.Validators.Inventory;

public class CreateUnitRequestValidator : AbstractValidator<CreateUnitRequest>
{
    public CreateUnitRequestValidator()
    {
        RuleFor(x => x.Code).NotEmpty().WithMessage("Code is required.").MaximumLength(20);
        RuleFor(x => x.Name).NotEmpty().WithMessage("Name is required.").MaximumLength(100);
    }
}

public class UpdateUnitRequestValidator : AbstractValidator<UpdateUnitRequest>
{
    public UpdateUnitRequestValidator()
    {
        RuleFor(x => x.Code).NotEmpty().WithMessage("Code is required.").MaximumLength(20);
        RuleFor(x => x.Name).NotEmpty().WithMessage("Name is required.").MaximumLength(100);
    }
}
