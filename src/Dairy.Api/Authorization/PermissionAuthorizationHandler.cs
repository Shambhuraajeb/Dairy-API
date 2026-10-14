using Dairy.Domain.Constants;
using Microsoft.AspNetCore.Authorization;

namespace Dairy.Api.Authorization;

public class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, PermissionRequirement requirement)
    {
        var user = context.User;

        if (user.HasClaim(AppClaims.Role, Roles.SuperAdmin) ||
            requirement.Permissions.Any(p => user.HasClaim(AppClaims.Permission, p)))
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
