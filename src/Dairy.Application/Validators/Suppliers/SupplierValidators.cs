using Dairy.Application.Requests.Suppliers;
using FluentValidation;

namespace Dairy.Application.Validators.Suppliers;

public abstract class SupplierRequestRules<T> : AbstractValidator<T> where T : CreateSupplierRequest
{
    protected SupplierRequestRules()
    {
        RuleFor(x => x.SupplierCode).NotEmpty().WithMessage("Supplier code is required.").MaximumLength(30);
        RuleFor(x => x.Name).NotEmpty().WithMessage("Name is required.").MaximumLength(150);
        RuleFor(x => x.ContactPerson).MaximumLength(150);
        RuleFor(x => x.Mobile).MustBeValidMobile();
        RuleFor(x => x.Email).EmailAddress().MaximumLength(150).When(x => !string.IsNullOrWhiteSpace(x.Email));
        RuleFor(x => x.AddressLine).MaximumLength(255);
        RuleFor(x => x.City).MaximumLength(100);
        RuleFor(x => x.State).MaximumLength(100);
        RuleFor(x => x.Pincode).MaximumLength(10);
        RuleFor(x => x.GstNumber).MaximumLength(20);
        RuleFor(x => x.PanNo).MaximumLength(15);
        RuleFor(x => x.PaymentTermsDays).InclusiveBetween(0, 365).WithMessage("Payment terms must be between 0 and 365 days.");
    }
}

public class CreateSupplierRequestValidator : SupplierRequestRules<CreateSupplierRequest> { }

public class UpdateSupplierRequestValidator : SupplierRequestRules<UpdateSupplierRequest> { }
