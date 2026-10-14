using System.Text.RegularExpressions;
using Dairy.Application.Requests.Farmers;
using FluentValidation;

namespace Dairy.Application.Validators.Farmers;

internal static class FarmerRuleHelpers
{
    private static readonly Regex AadhaarRegex = new(@"^[0-9]{12}$", RegexOptions.Compiled);
    private static readonly Regex PanRegex = new(@"^[A-Za-z]{5}[0-9]{4}[A-Za-z]$", RegexOptions.Compiled);
    private static readonly Regex PincodeRegex = new(@"^[0-9]{6}$", RegexOptions.Compiled);

    public static bool Aadhaar(string? v) => string.IsNullOrWhiteSpace(v) || AadhaarRegex.IsMatch(v.Trim());
    public static bool Pan(string? v) => string.IsNullOrWhiteSpace(v) || PanRegex.IsMatch(v.Trim());
    public static bool Pincode(string? v) => string.IsNullOrWhiteSpace(v) || PincodeRegex.IsMatch(v.Trim());
}

public class CreateFarmerRequestValidator : AbstractValidator<CreateFarmerRequest>
{
    public CreateFarmerRequestValidator()
    {
        RuleFor(x => x.FarmerCode).MaximumLength(30);
        RuleFor(x => x.FullName).NotEmpty().WithMessage("Full name is required.").MaximumLength(150);
        RuleFor(x => x.FatherName).MaximumLength(150);
        RuleFor(x => x.Mobile).NotEmpty().WithMessage("Mobile number is required.").MustBeValidMobile();
        RuleFor(x => x.AlternateMobile).MustBeValidMobile();
        RuleFor(x => x.Email).EmailAddress().MaximumLength(150).When(x => !string.IsNullOrWhiteSpace(x.Email));
        RuleFor(x => x.Gender).MustBeOneOf("Male", "Female", "Other");
        RuleFor(x => x.DateOfBirth).LessThan(DateTime.Today.AddDays(1)).When(x => x.DateOfBirth.HasValue)
            .WithMessage("Date of birth cannot be in the future.");
        RuleFor(x => x.AadhaarNo).Must(FarmerRuleHelpers.Aadhaar).WithMessage("Aadhaar number must be 12 digits.");
        RuleFor(x => x.PanNo).Must(FarmerRuleHelpers.Pan).WithMessage("PAN must look like ABCDE1234F.");
        RuleFor(x => x.MilkType).NotEmpty().MustBeOneOf("Cow", "Buffalo", "Mixed");
        RuleFor(x => x.Notes).MaximumLength(500);

        RuleFor(x => x.Address!).SetValidator(new FarmerAddressRequestValidator()).When(x => x.Address is not null);
        RuleFor(x => x.BankAccount!).SetValidator(new FarmerBankAccountRequestValidator()).When(x => x.BankAccount is not null);
    }
}

public class UpdateFarmerRequestValidator : AbstractValidator<UpdateFarmerRequest>
{
    public UpdateFarmerRequestValidator()
    {
        RuleFor(x => x.FullName).NotEmpty().WithMessage("Full name is required.").MaximumLength(150);
        RuleFor(x => x.FatherName).MaximumLength(150);
        RuleFor(x => x.Mobile).NotEmpty().WithMessage("Mobile number is required.").MustBeValidMobile();
        RuleFor(x => x.AlternateMobile).MustBeValidMobile();
        RuleFor(x => x.Email).EmailAddress().MaximumLength(150).When(x => !string.IsNullOrWhiteSpace(x.Email));
        RuleFor(x => x.Gender).MustBeOneOf("Male", "Female", "Other");
        RuleFor(x => x.DateOfBirth).LessThan(DateTime.Today.AddDays(1)).When(x => x.DateOfBirth.HasValue)
            .WithMessage("Date of birth cannot be in the future.");
        RuleFor(x => x.AadhaarNo).Must(FarmerRuleHelpers.Aadhaar).WithMessage("Aadhaar number must be 12 digits.");
        RuleFor(x => x.PanNo).Must(FarmerRuleHelpers.Pan).WithMessage("PAN must look like ABCDE1234F.");
        RuleFor(x => x.MilkType).NotEmpty().MustBeOneOf("Cow", "Buffalo", "Mixed");
        RuleFor(x => x.Notes).MaximumLength(500);
    }
}

public class FarmerAddressRequestValidator : AbstractValidator<FarmerAddressRequest>
{
    public FarmerAddressRequestValidator()
    {
        RuleFor(x => x.AddressType).NotEmpty().MustBeOneOf("Home", "Farm", "Billing");
        RuleFor(x => x.Line1).NotEmpty().WithMessage("Address line 1 is required.").MaximumLength(200);
        RuleFor(x => x.Line2).MaximumLength(200);
        RuleFor(x => x.Village).MaximumLength(100);
        RuleFor(x => x.Taluka).MaximumLength(100);
        RuleFor(x => x.District).MaximumLength(100);
        RuleFor(x => x.City).MaximumLength(100);
        RuleFor(x => x.State).MaximumLength(100);
        RuleFor(x => x.Pincode).Must(FarmerRuleHelpers.Pincode).WithMessage("Pincode must be 6 digits.");
        RuleFor(x => x.Latitude).InclusiveBetween(-90m, 90m).When(x => x.Latitude.HasValue);
        RuleFor(x => x.Longitude).InclusiveBetween(-180m, 180m).When(x => x.Longitude.HasValue);
    }
}

public class FarmerBankAccountRequestValidator : AbstractValidator<FarmerBankAccountRequest>
{
    public FarmerBankAccountRequestValidator()
    {
        RuleFor(x => x.AccountHolderName).NotEmpty().WithMessage("Account holder name is required.").MaximumLength(150);
        RuleFor(x => x.BankName).NotEmpty().WithMessage("Bank name is required.").MaximumLength(100);
        RuleFor(x => x.BranchName).MaximumLength(100);
        RuleFor(x => x.AccountNumber).NotEmpty().WithMessage("Account number is required.")
            .Matches(@"^[0-9A-Za-z]{6,30}$").WithMessage("Account number must be 6 to 30 letters/digits.");
        RuleFor(x => x.IfscCode).NotEmpty().WithMessage("IFSC code is required.").MustBeValidIfsc();
        RuleFor(x => x.UpiId).MaximumLength(100);
    }
}

public class FarmerStatusRequestValidator : AbstractValidator<FarmerStatusRequest>
{
    public FarmerStatusRequestValidator()
    {
        RuleFor(x => x.Reason).MaximumLength(255);
    }
}

public class VerifyFarmerRequestValidator : AbstractValidator<VerifyFarmerRequest>
{
    public VerifyFarmerRequestValidator()
    {
        RuleFor(x => x.Remarks).MaximumLength(255);
        RuleFor(x => x.Remarks).NotEmpty().When(x => !x.Approved).WithMessage("Remarks are required when rejecting.");
    }
}

public class FarmerDocumentRequestValidator : AbstractValidator<FarmerDocumentRequest>
{
    public FarmerDocumentRequestValidator()
    {
        RuleFor(x => x.DocumentNumber).MaximumLength(50);
        RuleFor(x => x.FileName).MaximumLength(255);
        RuleFor(x => x.Remarks).MaximumLength(255);
    }
}

public class UpdateFarmerDocumentRequestValidator : AbstractValidator<UpdateFarmerDocumentRequest>
{
    public UpdateFarmerDocumentRequestValidator()
    {
        RuleFor(x => x.DocumentNumber).MaximumLength(50);
        RuleFor(x => x.Remarks).MaximumLength(255);
        RuleFor(x => x.VerificationStatus).MustBeOneOf("Pending", "Verified", "Rejected");
    }
}

public class CreateFarmerAdvanceRequestValidator : AbstractValidator<CreateFarmerAdvanceRequest>
{
    public CreateFarmerAdvanceRequestValidator()
    {
        RuleFor(x => x.FarmerId).GreaterThan(0).WithMessage("Farmer is required.");
        RuleFor(x => x.Amount).GreaterThan(0m).WithMessage("Amount must be greater than zero.")
            .LessThanOrEqualTo(100000000m);
        RuleFor(x => x.RecoveryPerSettlement).GreaterThan(0m).When(x => x.RecoveryPerSettlement.HasValue)
            .WithMessage("Recovery per settlement must be greater than zero.");
        RuleFor(x => x.Purpose).MaximumLength(255);
        RuleFor(x => x.ReferenceNo).MaximumLength(50);
        RuleFor(x => x.Remarks).MaximumLength(255);
    }
}

public class CreateFarmerLoanRequestValidator : AbstractValidator<CreateFarmerLoanRequest>
{
    public CreateFarmerLoanRequestValidator()
    {
        RuleFor(x => x.FarmerId).GreaterThan(0).WithMessage("Farmer is required.");
        RuleFor(x => x.PrincipalAmount).GreaterThan(0m).WithMessage("Principal must be greater than zero.")
            .LessThanOrEqualTo(100000000m);
        RuleFor(x => x.InterestRate).InclusiveBetween(0m, 100m).WithMessage("Interest rate must be between 0 and 100.");
        RuleFor(x => x.NumberOfInstallments).InclusiveBetween(1, 120).WithMessage("Installments must be between 1 and 120.");
        RuleFor(x => x.StartDate).NotNull().WithMessage("Start date is required.");
        RuleFor(x => x.Remarks).MaximumLength(255);
    }
}

public class PayLoanInstallmentRequestValidator : AbstractValidator<PayLoanInstallmentRequest>
{
    public PayLoanInstallmentRequestValidator()
    {
        RuleFor(x => x.Amount).GreaterThan(0m).When(x => x.Amount.HasValue)
            .WithMessage("Amount must be greater than zero.");
    }
}
