using System.Globalization;
using System.Security.Claims;
using Dairy.Application.Interfaces;
using Dairy.Domain.Constants;
using Microsoft.AspNetCore.Http;

namespace Dairy.Infrastructure.Security;

public class CurrentUser : ICurrentUser
{
    private readonly IHttpContextAccessor _accessor;

    public CurrentUser(IHttpContextAccessor accessor) => _accessor = accessor;

    private ClaimsPrincipal? Principal => _accessor.HttpContext?.User;

    public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated == true;

    public long UserId => GetLong(AppClaims.UserId) ?? 0;

    public string Username => Principal?.FindFirst(AppClaims.Subject)?.Value ?? string.Empty;

    public string Role => Principal?.FindFirst(AppClaims.Role)?.Value ?? string.Empty;

    public long? FarmerId => GetLong(AppClaims.FarmerId);

    public long? CustomerId => GetLong(AppClaims.CustomerId);

    public long? EmployeeId => GetLong(AppClaims.EmployeeId);

    public string? IpAddress => _accessor.HttpContext?.Connection.RemoteIpAddress?.ToString();

    public string? UserAgent
    {
        get
        {
            var ua = _accessor.HttpContext?.Request.Headers.UserAgent.ToString();
            if (string.IsNullOrEmpty(ua)) return null;
            return ua.Length > 255 ? ua.Substring(0, 255) : ua;
        }
    }

    public bool IsInRole(string role) => string.Equals(Role, role, StringComparison.OrdinalIgnoreCase);

    public bool HasPermission(string permission)
    {
        if (!IsAuthenticated) return false;
        if (IsInRole(Roles.SuperAdmin)) return true;
        return Principal!.HasClaim(AppClaims.Permission, permission);
    }

    private long? GetLong(string claimType)
    {
        var value = Principal?.FindFirst(claimType)?.Value;
        return long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var id) ? id : null;
    }
}
