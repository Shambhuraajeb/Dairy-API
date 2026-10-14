using Microsoft.AspNetCore.Authorization;

namespace Dairy.Api.Authorization;

/// <summary>
/// Requires the JWT to carry at least one of the given permission codes (Super Admin bypasses).
/// Apply per action; multiple attributes on the same endpoint are ANDed.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public class HasPermissionAttribute : AuthorizeAttribute
{
    public const string PolicyPrefix = "perm:";

    public HasPermissionAttribute(params string[] permissions)
    {
        Policy = PolicyPrefix + string.Join("|", permissions);
    }
}
