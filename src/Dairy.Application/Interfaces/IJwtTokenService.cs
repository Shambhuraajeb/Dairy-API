using Dairy.Application.DTOs.Auth;

namespace Dairy.Application.Interfaces;

public record JwtTokenResult(string AccessToken, DateTime ExpiresAt);

public interface IJwtTokenService
{
    JwtTokenResult CreateToken(UserLoginRow user, IReadOnlyCollection<string> permissions);
}
