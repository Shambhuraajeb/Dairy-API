namespace Dairy.Application.BusinessRules;

/// <summary>Password policy: min 8 chars, upper, lower, digit, symbol.</summary>
public static class PasswordPolicy
{
    public const int MinLength = 8;

    public const string Description =
        "Password must be at least 8 characters and contain an uppercase letter, a lowercase letter, a digit and a symbol.";

    /// <summary>Returns the list of violated rules (empty when valid).</summary>
    public static IReadOnlyList<string> Validate(string? password)
    {
        var errors = new List<string>();
        if (string.IsNullOrEmpty(password) || password.Length < MinLength)
            errors.Add($"Password must be at least {MinLength} characters long.");
        if (string.IsNullOrEmpty(password)) return errors;

        if (!password.Any(char.IsUpper)) errors.Add("Password must contain an uppercase letter.");
        if (!password.Any(char.IsLower)) errors.Add("Password must contain a lowercase letter.");
        if (!password.Any(char.IsDigit)) errors.Add("Password must contain a digit.");
        if (!password.Any(c => !char.IsLetterOrDigit(c) && !char.IsWhiteSpace(c)))
            errors.Add("Password must contain a symbol.");
        return errors;
    }

    public static bool IsValid(string? password) => Validate(password).Count == 0;
}
