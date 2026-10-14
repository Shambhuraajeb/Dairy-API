using System.Text.RegularExpressions;
using FluentValidation;

namespace Dairy.Application.Validators;

public static class CommonRules
{
    private static readonly Regex MobileRegex = new(@"^\+?[0-9]{10,13}$", RegexOptions.Compiled);
    private static readonly Regex IfscRegex = new(@"^[A-Za-z]{4}0[A-Za-z0-9]{6}$", RegexOptions.Compiled);

    /// <summary>10 to 13 digits with an optional leading '+'. Empty values pass (use NotEmpty for required fields).</summary>
    public static IRuleBuilderOptions<T, string?> MustBeValidMobile<T>(this IRuleBuilder<T, string?> ruleBuilder) =>
        ruleBuilder
            .Must(v => string.IsNullOrWhiteSpace(v) || MobileRegex.IsMatch(v.Trim()))
            .WithMessage("Mobile number must be 10 to 13 digits (optional leading +).");

    /// <summary>Standard IFSC format (4 letters, 0, 6 alphanumerics).</summary>
    public static IRuleBuilderOptions<T, string?> MustBeValidIfsc<T>(this IRuleBuilder<T, string?> ruleBuilder) =>
        ruleBuilder
            .Must(v => string.IsNullOrWhiteSpace(v) || IfscRegex.IsMatch(v.Trim()))
            .WithMessage("IFSC code must look like ABCD0123456.");

    /// <summary>Value must be one of the allowed options (case-sensitive, as stored). Empty values pass.</summary>
    public static IRuleBuilderOptions<T, string?> MustBeOneOf<T>(this IRuleBuilder<T, string?> ruleBuilder, params string[] allowed) =>
        ruleBuilder
            .Must(v => string.IsNullOrEmpty(v) || allowed.Contains(v))
            .WithMessage("Must be one of: " + string.Join(", ", allowed) + ".");
}
