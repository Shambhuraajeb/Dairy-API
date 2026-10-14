using Dairy.Application.Requests.Auth;
using Dairy.Application.Responses;

namespace Dairy.Application.Interfaces.Auth;

public interface IAuthService
{
    Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken ct = default);
    Task ChangePasswordAsync(ChangePasswordRequest request, CancellationToken ct = default);
    Task<AuthUserDto> GetCurrentUserAsync(CancellationToken ct = default);
}
