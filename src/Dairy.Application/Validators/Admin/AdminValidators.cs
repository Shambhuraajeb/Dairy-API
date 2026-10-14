using Dairy.Application.Requests.Admin;
using FluentValidation;

namespace Dairy.Application.Validators.Admin;

public abstract class UserRequestValidatorBase<T> : AbstractValidator<T> where T : UserRequestBase
{
    protected UserRequestValidatorBase()
    {
        RuleFor(x => x.DisplayName).NotEmpty().WithMessage("Display name is required.").MaximumLength(150);
        RuleFor(x => x.Email).EmailAddress().WithMessage("Email is not valid.").MaximumLength(150)
            .When(x => !string.IsNullOrWhiteSpace(x.Email));
        RuleFor(x => x.Mobile).MustBeValidMobile();
        RuleFor(x => x.RoleId).GreaterThan(0).WithMessage("Role is required.");
    }
}

public class CreateUserRequestValidator : UserRequestValidatorBase<CreateUserRequest>
{
    public CreateUserRequestValidator()
    {
        RuleFor(x => x.Username).NotEmpty().WithMessage("Username is required.")
            .Matches("^[A-Za-z0-9._@-]{3,100}$")
            .WithMessage("Username must be 3-100 characters: letters, digits, '.', '_', '-' or '@'.");
        RuleFor(x => x.Password).NotEmpty().WithMessage("Password is required.").MustBeStrongPassword();
    }
}

public class UpdateUserRequestValidator : UserRequestValidatorBase<UpdateUserRequest>
{
}

public class ResetPasswordRequestValidator : AbstractValidator<ResetPasswordRequest>
{
    public ResetPasswordRequestValidator()
    {
        RuleFor(x => x.NewPassword!).MustBeStrongPassword().When(x => !string.IsNullOrWhiteSpace(x.NewPassword));
    }
}

public class CreateRoleRequestValidator : AbstractValidator<CreateRoleRequest>
{
    public CreateRoleRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().WithMessage("Name is required.").MaximumLength(100);
        RuleFor(x => x.Description).MaximumLength(255);
    }
}

public class UpdateRoleRequestValidator : AbstractValidator<UpdateRoleRequest>
{
    public UpdateRoleRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().WithMessage("Name is required.").MaximumLength(100);
        RuleFor(x => x.Description).MaximumLength(255);
    }
}

public class SetRolePermissionsRequestValidator : AbstractValidator<SetRolePermissionsRequest>
{
    public SetRolePermissionsRequestValidator()
    {
        RuleFor(x => x).Must(x => x.PermissionIds != null || x.Codes != null)
            .WithMessage("Provide permissionIds and/or codes (an empty list removes all permissions).")
            .OverridePropertyName("permissionIds");
    }
}

public class UpdateSettingRequestValidator : AbstractValidator<UpdateSettingRequest>
{
    public UpdateSettingRequestValidator()
    {
        RuleFor(x => x.Value).NotNull().WithMessage("Value is required.").MaximumLength(4000);
    }
}

public class SettingItemRequestValidator : AbstractValidator<SettingItemRequest>
{
    public SettingItemRequestValidator()
    {
        RuleFor(x => x.Key).NotEmpty().WithMessage("Key is required.").MaximumLength(100);
        RuleFor(x => x.Value).NotNull().WithMessage("Value is required.").MaximumLength(4000);
    }
}

public class BulkUpdateSettingsRequestValidator : AbstractValidator<BulkUpdateSettingsRequest>
{
    public BulkUpdateSettingsRequestValidator()
    {
        RuleFor(x => x.Items).NotEmpty().WithMessage("At least one setting is required.")
            .Must(i => i.Count <= 200).WithMessage("At most 200 settings per request.");
        RuleForEach(x => x.Items).SetValidator(new SettingItemRequestValidator());
    }
}

public class SaveDairyRequestValidator : AbstractValidator<SaveDairyRequest>
{
    public SaveDairyRequestValidator()
    {
        RuleFor(x => x.Code).NotEmpty().WithMessage("Code is required.").MaximumLength(30);
        RuleFor(x => x.Name).NotEmpty().WithMessage("Name is required.").MaximumLength(150);
        RuleFor(x => x.OwnerName).MaximumLength(150);
        RuleFor(x => x.Mobile).MustBeValidMobile();
        RuleFor(x => x.Email).EmailAddress().WithMessage("Email is not valid.").MaximumLength(150)
            .When(x => !string.IsNullOrWhiteSpace(x.Email));
        RuleFor(x => x.AddressLine).MaximumLength(255);
        RuleFor(x => x.City).MaximumLength(100);
        RuleFor(x => x.State).MaximumLength(100);
        RuleFor(x => x.Pincode).MaximumLength(10);
        RuleFor(x => x.GstNumber).MaximumLength(20);
        RuleFor(x => x.LicenseNo).MaximumLength(50);
        RuleFor(x => x.RegistrationNo).MaximumLength(50);
    }
}

public class SaveBranchRequestValidator : AbstractValidator<SaveBranchRequest>
{
    public SaveBranchRequestValidator()
    {
        RuleFor(x => x.DairyId).GreaterThan(0).WithMessage("Dairy is required.");
        RuleFor(x => x.Code).NotEmpty().WithMessage("Code is required.").MaximumLength(30);
        RuleFor(x => x.Name).NotEmpty().WithMessage("Name is required.").MaximumLength(150);
        RuleFor(x => x.AddressLine).MaximumLength(255);
        RuleFor(x => x.City).MaximumLength(100);
        RuleFor(x => x.State).MaximumLength(100);
        RuleFor(x => x.Pincode).MaximumLength(10);
        RuleFor(x => x.Mobile).MustBeValidMobile();
        RuleFor(x => x.Email).EmailAddress().WithMessage("Email is not valid.").MaximumLength(150)
            .When(x => !string.IsNullOrWhiteSpace(x.Email));
        RuleFor(x => x.ManagerName).MaximumLength(150);
    }
}

public class SaveCollectionCenterRequestValidator : AbstractValidator<SaveCollectionCenterRequest>
{
    public SaveCollectionCenterRequestValidator()
    {
        RuleFor(x => x.BranchId).GreaterThan(0).WithMessage("Branch is required.");
        RuleFor(x => x.Code).NotEmpty().WithMessage("Code is required.").MaximumLength(30);
        RuleFor(x => x.Name).NotEmpty().WithMessage("Name is required.").MaximumLength(150);
        RuleFor(x => x.AddressLine).MaximumLength(255);
        RuleFor(x => x.City).MaximumLength(100);
        RuleFor(x => x.Pincode).MaximumLength(10);
        RuleFor(x => x.Mobile).MustBeValidMobile();
        RuleFor(x => x.ContactPerson).MaximumLength(150);
        RuleFor(x => x.Latitude).InclusiveBetween(-90m, 90m).When(x => x.Latitude.HasValue);
        RuleFor(x => x.Longitude).InclusiveBetween(-180m, 180m).When(x => x.Longitude.HasValue);
        RuleFor(x => x.CapacityLiters).GreaterThanOrEqualTo(0m).When(x => x.CapacityLiters.HasValue);
    }
}

public class UpdateProfileRequestValidator : AbstractValidator<UpdateProfileRequest>
{
    public UpdateProfileRequestValidator()
    {
        RuleFor(x => x.DisplayName).NotEmpty().WithMessage("Display name is required.").MaximumLength(150);
        RuleFor(x => x.Email).EmailAddress().WithMessage("Email is not valid.").MaximumLength(150)
            .When(x => !string.IsNullOrWhiteSpace(x.Email));
        RuleFor(x => x.Mobile).MustBeValidMobile();
    }
}
