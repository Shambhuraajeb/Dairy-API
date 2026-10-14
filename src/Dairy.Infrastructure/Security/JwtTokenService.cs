using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Dairy.Application.DTOs.Auth;
using Dairy.Application.Interfaces;
using Dairy.Domain.Constants;
using Microsoft.IdentityModel.Tokens;

namespace Dairy.Infrastructure.Security;

public class JwtTokenService : IJwtTokenService
{
    private readonly JwtOptions _options;
    private readonly SigningCredentials _credentials;

    public JwtTokenService(JwtOptions options)
    {
        _options = options;
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(options.Secret));
        _credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
    }

    public JwtTokenResult CreateToken(UserLoginRow user, IReadOnlyCollection<string> permissions)
    {
        var now = DateTime.UtcNow;
        var expires = now.AddMinutes(_options.ExpiryMinutes);

        var claims = new List<Claim>
        {
            new(AppClaims.Subject, user.Username),
            new(AppClaims.Name, user.DisplayName),
            new(AppClaims.Role, user.RoleName),
            new(AppClaims.UserId, user.UserId.ToString(CultureInfo.InvariantCulture)),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N"))
        };

        if (user.FarmerId.HasValue)
            claims.Add(new Claim(AppClaims.FarmerId, user.FarmerId.Value.ToString(CultureInfo.InvariantCulture)));
        if (user.CustomerId.HasValue)
            claims.Add(new Claim(AppClaims.CustomerId, user.CustomerId.Value.ToString(CultureInfo.InvariantCulture)));
        if (user.EmployeeId.HasValue)
            claims.Add(new Claim(AppClaims.EmployeeId, user.EmployeeId.Value.ToString(CultureInfo.InvariantCulture)));

        foreach (var permission in permissions)
            claims.Add(new Claim(AppClaims.Permission, permission));

        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            notBefore: now,
            expires: expires,
            signingCredentials: _credentials);

        return new JwtTokenResult(new JwtSecurityTokenHandler().WriteToken(token), expires);
    }
}
