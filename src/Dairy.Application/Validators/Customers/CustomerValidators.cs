using Dairy.Application.BusinessRules;
using Dairy.Application.Requests.Customers;
using FluentValidation;

namespace Dairy.Application.Validators.Customers;

public class CreateCustomerRequestValidator : AbstractValidator<CreateCustomerRequest>
{
    public CreateCustomerRequestValidator()
    {
        RuleFor(x => x.CustomerCode).MaximumLength(30);
        RuleFor(x => x.CustomerType).NotEmpty().MustBeOneOf(SalesValues.CustomerTypes);
        RuleFor(x => x.Name).NotEmpty().WithMessage("Name is required.").MaximumLength(150);
        RuleFor(x => x.ContactPerson).MaximumLength(150);
        RuleFor(x => x.Mobile).NotEmpty().WithMessage("Mobile number is required.").MustBeValidMobile();
        RuleFor(x => x.AlternateMobile).MustBeValidMobile();
        RuleFor(x => x.Email).EmailAddress().MaximumLength(150).When(x => !string.IsNullOrWhiteSpace(x.Email));
        RuleFor(x => x.GstNumber).MaximumLength(20);
        RuleFor(x => x.CreditLimit).GreaterThanOrEqualTo(0);
        RuleFor(x => x.CreditDays).InclusiveBetween(0, 365);
        RuleFor(x => x.DiscountPercent).InclusiveBetween(0, 100);
        RuleFor(x => x.Notes).MaximumLength(500);
    }
}

public class UpdateCustomerRequestValidator : AbstractValidator<UpdateCustomerRequest>
{
    public UpdateCustomerRequestValidator()
    {
        RuleFor(x => x.CustomerType).NotEmpty().MustBeOneOf(SalesValues.CustomerTypes);
        RuleFor(x => x.Name).NotEmpty().WithMessage("Name is required.").MaximumLength(150);
        RuleFor(x => x.ContactPerson).MaximumLength(150);
        RuleFor(x => x.Mobile).NotEmpty().WithMessage("Mobile number is required.").MustBeValidMobile();
        RuleFor(x => x.AlternateMobile).MustBeValidMobile();
        RuleFor(x => x.Email).EmailAddress().MaximumLength(150).When(x => !string.IsNullOrWhiteSpace(x.Email));
        RuleFor(x => x.GstNumber).MaximumLength(20);
        RuleFor(x => x.CreditLimit).GreaterThanOrEqualTo(0);
        RuleFor(x => x.CreditDays).InclusiveBetween(0, 365);
        RuleFor(x => x.DiscountPercent).InclusiveBetween(0, 100);
        RuleFor(x => x.Notes).MaximumLength(500);
    }
}

public class CustomerAddressRequestValidator : AbstractValidator<CustomerAddressRequest>
{
    public CustomerAddressRequestValidator()
    {
        RuleFor(x => x.AddressType).NotEmpty().MustBeOneOf(SalesValues.AddressTypes);
        RuleFor(x => x.ContactName).MaximumLength(150);
        RuleFor(x => x.ContactMobile).MustBeValidMobile();
        RuleFor(x => x.Line1).NotEmpty().WithMessage("Address line 1 is required.").MaximumLength(200);
        RuleFor(x => x.Line2).MaximumLength(200);
        RuleFor(x => x.Landmark).MaximumLength(150);
        RuleFor(x => x.City).MaximumLength(100);
        RuleFor(x => x.State).MaximumLength(100);
        RuleFor(x => x.Pincode).Must(SalesValues.IsValidPincode).WithMessage("Pincode must be 6 digits.");
        RuleFor(x => x.Latitude).InclusiveBetween(-90m, 90m).When(x => x.Latitude.HasValue);
        RuleFor(x => x.Longitude).InclusiveBetween(-180m, 180m).When(x => x.Longitude.HasValue);
        RuleFor(x => x.DeliveryInstructions).MaximumLength(255);
    }
}

public class CreateComplaintRequestValidator : AbstractValidator<CreateComplaintRequest>
{
    public CreateComplaintRequestValidator()
    {
        RuleFor(x => x.Category).NotEmpty().MustBeOneOf(SalesValues.ComplaintCategories);
        RuleFor(x => x.Subject).NotEmpty().WithMessage("Subject is required.").MaximumLength(150);
        RuleFor(x => x.Description).NotEmpty().WithMessage("Description is required.").MaximumLength(4000);
        RuleFor(x => x.Priority).NotEmpty().MustBeOneOf(SalesValues.ComplaintPriorities);
    }
}

public class AssignComplaintRequestValidator : AbstractValidator<AssignComplaintRequest>
{
    public AssignComplaintRequestValidator()
    {
        RuleFor(x => x.AssignedTo).GreaterThan(0).WithMessage("assignedTo (user id) is required.");
    }
}

public class ComplaintStatusRequestValidator : AbstractValidator<ComplaintStatusRequest>
{
    public ComplaintStatusRequestValidator()
    {
        RuleFor(x => x.Status).NotEmpty().MustBeOneOf("Open", "Assigned", "InProgress");
    }
}

public class ResolveComplaintRequestValidator : AbstractValidator<ResolveComplaintRequest>
{
    public ResolveComplaintRequestValidator()
    {
        RuleFor(x => x.Resolution).NotEmpty().WithMessage("Resolution is required.").MaximumLength(4000);
    }
}

public class ComplaintFeedbackRequestValidator : AbstractValidator<ComplaintFeedbackRequest>
{
    public ComplaintFeedbackRequestValidator()
    {
        RuleFor(x => x.Rating).InclusiveBetween(1, 5);
        RuleFor(x => x.Comments).MaximumLength(500);
    }
}
