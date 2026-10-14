using Microsoft.AspNetCore.Authorization;

namespace Dairy.Api.Authorization;

public class PermissionRequirement : IAuthorizationRequirement
{
    public PermissionRequirement(IReadOnlyList<string> permissions) => Permissions = permissions;

    /// <summary>Any one of these satisfies the requirement.</summary>
    public IReadOnlyList<string> Permissions { get; }
}
