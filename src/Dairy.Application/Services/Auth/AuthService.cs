using Dairy.Application.Common;
using Dairy.Application.DTOs.Auth;
using Dairy.Application.Interfaces;
using Dairy.Application.Interfaces.Auth;
using Dairy.Application.Requests.Auth;
using Dairy.Application.Responses;
using Dairy.Domain.Constants;

namespace Dairy.Application.Services.Auth;

public class AuthService : IAuthService
{
    private const string InvalidCredentials = "Invalid username or password.";

    // Valid bcrypt hash used only to equalize response time for unknown usernames.
    private const string DummyHash = "$2b$11$rnBl7dqo1lKalx.qXg6yWOR84A9csMqF07oQ5G10DZqRMi6M7SEw2";

    private readonly IAuthRepository _repo;
    private readonly IPasswordHasher _hasher;
    private readonly IJwtTokenService _jwt;
    private readonly IAuditService _audit;
    private readonly ICurrentUser _current;

    public AuthService(IAuthRepository repo, IPasswordHasher hasher, IJwtTokenService jwt, IAuditService audit, ICurrentUser current)
    {
        _repo = repo;
        _hasher = hasher;
        _jwt = jwt;
        _audit = audit;
        _current = current;
    }

    public async Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken ct = default)
    {
        var username = request.Username.Trim();

        var user = await _repo.GetUserForLoginAsync(username, ct);
        if (user is null)
        {
            _hasher.Verify(request.Password, DummyHash);
            await RecordFailureAsync(null, username, "UserNotFound", ct);
            throw new UnauthorizedException(InvalidCredentials);
        }

        if (!user.IsActive)
        {
            await RecordFailureAsync(user, username, "UserInactive", ct);
            throw new UnauthorizedException(InvalidCredentials);
        }

        // DB timestamps (LockedUntil) come from MySQL NOW(), i.e. server-local time.
        if (user.LockedUntil.HasValue && user.LockedUntil.Value > DateTime.Now)
        {
            await RecordFailureAsync(user, username, "AccountLocked", ct);
            throw new UnauthorizedException("Account is temporarily locked after repeated failed attempts. Please try again later.");
        }

        if (!_hasher.Verify(request.Password, user.PasswordHash))
        {
            await RecordFailureAsync(user, username, "InvalidPassword", ct);
            throw new UnauthorizedException(InvalidCredentials);
        }

        await _repo.RecordLoginAttemptAsync(user.UserId, user.Username, true, null, _current.IpAddress, _current.UserAgent, ct);

        var permissions = await LoadPermissionsAsync(user, ct);
        var token = _jwt.CreateToken(user, permissions);

        await _audit.LogForUserAsync(user.UserId, user.Username, AuditActions.Login, "User", user.UserId, null, null, ct);

        return new LoginResponse
        {
            AccessToken = token.AccessToken,
            ExpiresAt = token.ExpiresAt,
            User = ToDto(user, permissions)
        };
    }

    public async Task ChangePasswordAsync(ChangePasswordRequest request, CancellationToken ct = default)
    {
        var user = await _repo.GetUserForLoginAsync(_current.Username, ct)
                   ?? throw new UnauthorizedException("Authentication is required.");

        if (!_hasher.Verify(request.CurrentPassword, user.PasswordHash))
            throw new BusinessRuleException("Current password is incorrect.");

        var hash = _hasher.Hash(request.NewPassword);
        await _repo.UpdatePasswordAsync(user.UserId, hash, ct);

        await _audit.LogAsync(AuditActions.ChangePassword, "User", user.UserId, null, null, ct);
    }

    public async Task<AuthUserDto> GetCurrentUserAsync(CancellationToken ct = default)
    {
        var user = await _repo.GetUserForLoginAsync(_current.Username, ct);
        if (user is null || !user.IsActive)
            throw new UnauthorizedException("Authentication is required.");

        var permissions = await LoadPermissionsAsync(user, ct);
        return ToDto(user, permissions);
    }

    private async Task<List<string>> LoadPermissionsAsync(UserLoginRow user, CancellationToken ct)
    {
        var codes = await _repo.GetPermissionsAsync(user.UserId, ct);
        if (string.Equals(user.RoleName, Roles.SuperAdmin, StringComparison.OrdinalIgnoreCase))
            codes = codes.Union(Permissions.All, StringComparer.OrdinalIgnoreCase).ToList();
        return codes.Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(c => c, StringComparer.Ordinal).ToList();
    }

    private async Task RecordFailureAsync(UserLoginRow? user, string username, string reason, CancellationToken ct)
    {
        await _repo.RecordLoginAttemptAsync(user?.UserId, username, false, reason, _current.IpAddress, _current.UserAgent, ct);
        await _audit.LogForUserAsync(user?.UserId, username, AuditActions.LoginFailed, "User", user?.UserId, null, new { reason }, ct);
    }

    private static AuthUserDto ToDto(UserLoginRow user, List<string> permissions) => new()
    {
        Id = user.UserId,
        Username = user.Username,
        DisplayName = user.DisplayName,
        Role = user.RoleName,
        Permissions = permissions,
        Email = user.Email,
        FarmerId = user.FarmerId,
        CustomerId = user.CustomerId,
        EmployeeId = user.EmployeeId,
        MustChangePassword = user.MustChangePassword
    };
}
