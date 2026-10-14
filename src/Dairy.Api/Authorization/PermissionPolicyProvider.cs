using System.Collections.Concurrent;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace Dairy.Api.Authorization;

/// <summary>Builds "perm:code1|code2" policies on demand; everything else falls back to the default provider.</summary>
public class PermissionPolicyProvider : DefaultAuthorizationPolicyProvider
{
    private readonly ConcurrentDictionary<string, AuthorizationPolicy> _cache = new();

    public PermissionPolicyProvider(IOptions<AuthorizationOptions> options) : base(options) { }

    public override Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        if (!policyName.StartsWith(HasPermissionAttribute.PolicyPrefix, StringComparison.Ordinal))
            return base.GetPolicyAsync(policyName);

        var policy = _cache.GetOrAdd(policyName, name =>
        {
            var codes = name.Substring(HasPermissionAttribute.PolicyPrefix.Length)
                .Split('|', StringSplitOptions.RemoveEmptyEntries);
            return new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .AddRequirements(new PermissionRequirement(codes))
                .Build();
        });
        return Task.FromResult<AuthorizationPolicy?>(policy);
    }
}
