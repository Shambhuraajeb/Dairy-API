using Dairy.Application.BusinessRules;
using FluentValidation;

namespace Dairy.Application.Validators;

public static class PasswordRules
{
    /// <summary>Applies the password policy (min 8, upper, lower, digit, symbol).</summary>
    public static IRuleBuilderOptions<T, string> MustBeStrongPassword<T>(this IRuleBuilder<T, string> ruleBuilder) =>
        ruleBuilder
            .Must(p => PasswordPolicy.IsValid(p))
            .WithMessage(PasswordPolicy.Description);
}
