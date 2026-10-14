using System.Security.Cryptography;
using Dairy.Application.BusinessRules;
using Dairy.Application.Common;
using Dairy.Application.DTOs.Admin;
using Dairy.Application.Interfaces;
using Dairy.Application.Interfaces.Admin;
using Dairy.Application.Requests.Admin;
using Dairy.Domain.Constants;

namespace Dairy.Application.Services.Admin;

public class UserAdminService : IUserAdminService
{
    private readonly IUserAdminRepository _repo;
    private readonly IPasswordHasher _hasher;
    private readonly IAuditService _audit;
    private readonly ICurrentUser _user;

    public UserAdminService(IUserAdminRepository repo, IPasswordHasher hasher, IAuditService audit, ICurrentUser user)
    {
        _repo = repo;
        _hasher = hasher;
        _audit = audit;
        _user = user;
    }

    public Task<PagedResult<UserAdminDto>> GetPagedAsync(UserAdminQuery query, CancellationToken ct = default) =>
        _repo.GetPagedAsync(query, ct);

    public async Task<UserAdminDto> GetByIdAsync(long id, CancellationToken ct = default) =>
        await _repo.GetByIdAsync(id, ct) ?? throw new NotFoundException("User", id);

    public async Task<UserAdminDto> CreateAsync(CreateUserRequest request, CancellationToken ct = default)
    {
        var username = request.Username.Trim();
        if (await _repo.UsernameExistsAsync(username, null, ct))
            throw new ConflictException($"Username '{username}' is already taken.");

        Normalize(request);
        await ValidateRoleAndLinksAsync(request, ct);

        var hash = _hasher.Hash(request.Password);
        request.Username = username;
        var id = await _repo.CreateAsync(request, hash, request.MustChangePassword ?? true, request.IsActive ?? true, _user.UserId, ct);

        var created = await GetByIdAsync(id, ct);
        await _audit.LogAsync(AuditActions.Create, "User", id, null, created, ct);
        return created;
    }

    public async Task<UserAdminDto> UpdateAsync(long id, UpdateUserRequest request, CancellationToken ct = default)
    {
        var existing = await GetByIdAsync(id, ct);
        Normalize(request);
        var roleName = await ValidateRoleAndLinksAsync(request, ct);
        var isActive = request.IsActive ?? existing.IsActive;
        var roleChanged = request.RoleId != existing.RoleId;

        if (id == _user.UserId)
        {
            if (!isActive) throw new BusinessRuleException("You cannot deactivate your own account.");
            if (roleChanged) throw new BusinessRuleException("You cannot change your own role.");
        }

        var losesSuperAdmin = existing.IsActive &&
                              string.Equals(existing.RoleName, Roles.SuperAdmin, StringComparison.OrdinalIgnoreCase) &&
                              (!isActive || !string.Equals(roleName, Roles.SuperAdmin, StringComparison.OrdinalIgnoreCase));
        if (losesSuperAdmin && await _repo.CountOtherActiveSuperAdminsAsync(id, ct) == 0)
            throw new BusinessRuleException("At least one active Super Admin must remain.");

        await _repo.UpdateAsync(id, request, isActive, _user.UserId, ct);
        var updated = await GetByIdAsync(id, ct);

        var action = existing.IsActive && !updated.IsActive ? AuditActions.Deactivate
            : !existing.IsActive && updated.IsActive ? AuditActions.Activate
            : AuditActions.Update;
        await _audit.LogAsync(action, "User", id, existing, updated, ct);
        if (roleChanged)
            await _audit.LogAsync(AuditActions.RoleChange, "User", id, new { existing.RoleId, existing.RoleName }, new { updated.RoleId, updated.RoleName }, ct);
        return updated;
    }

    public async Task<ResetPasswordResultDto> ResetPasswordAsync(long id, ResetPasswordRequest? request, CancellationToken ct = default)
    {
        var existing = await GetByIdAsync(id, ct);

        string? generated = null;
        var password = request?.NewPassword;
        if (string.IsNullOrWhiteSpace(password))
        {
            generated = GeneratePassword();
            password = generated;
        }
        else
        {
            var problems = PasswordPolicy.Validate(password);
            if (problems.Count > 0)
                throw new RequestValidationException(problems.Select(p => new ApiError("newPassword", p)));
        }

        await _repo.SetPasswordAsync(id, _hasher.Hash(password), request?.MustChangePassword ?? true, _user.UserId, ct);
        await _audit.LogAsync(AuditActions.ChangePassword, "User", id, null, new { existing.Username, reset = true, generated = generated != null }, ct);
        return new ResetPasswordResultDto { TemporaryPassword = generated };
    }

    public async Task<UserAdminDto> UnlockAsync(long id, CancellationToken ct = default)
    {
        var existing = await GetByIdAsync(id, ct);
        await _repo.UnlockAsync(id, _user.UserId, ct);
        var updated = await GetByIdAsync(id, ct);
        await _audit.LogAsync(AuditActions.Update, "User", id,
            new { existing.IsLocked, existing.LockedUntil, existing.FailedLoginCount },
            new { updated.IsLocked, updated.LockedUntil, updated.FailedLoginCount }, ct);
        return updated;
    }

    /// <summary>Validates role + linked records; returns the role name.</summary>
    private async Task<string> ValidateRoleAndLinksAsync(UserRequestBase r, CancellationToken ct)
    {
        var roleName = await _repo.GetActiveRoleNameAsync(r.RoleId, ct)
                       ?? throw new RequestValidationException("roleId", "Role not found or inactive.");

        if (string.Equals(roleName, Roles.Farmer, StringComparison.OrdinalIgnoreCase) && !r.FarmerId.HasValue)
            throw new RequestValidationException("farmerId", "A Farmer user must be linked to a farmer.");
        if (Roles.IsCustomerRole(roleName) && !r.CustomerId.HasValue)
            throw new RequestValidationException("customerId", "A Customer/Distributor user must be linked to a customer.");

        await EnsureLinkAsync("farmer", "farmerId", r.FarmerId, ct);
        await EnsureLinkAsync("customer", "customerId", r.CustomerId, ct);
        await EnsureLinkAsync("employee", "employeeId", r.EmployeeId, ct);
        await EnsureLinkAsync("branch", "branchId", r.BranchId, ct);
        await EnsureLinkAsync("collectionCenter", "collectionCenterId", r.CollectionCenterId, ct);
        return roleName;
    }

    private async Task EnsureLinkAsync(string kind, string field, long? id, CancellationToken ct)
    {
        if (id.HasValue && !await _repo.LinkedEntityExistsAsync(kind, id.Value, ct))
            throw new RequestValidationException(field, "Referenced record not found.");
    }

    private static void Normalize(UserRequestBase r)
    {
        r.DisplayName = r.DisplayName.Trim();
        r.Email = B5Text.Clean(r.Email);
        r.Mobile = B5Text.Clean(r.Mobile);
    }

    /// <summary>12-character password containing upper, lower, digit and symbol (no look-alike characters).</summary>
    private static string GeneratePassword()
    {
        const string upper = "ABCDEFGHJKLMNPQRSTUVWXYZ";
        const string lower = "abcdefghijkmnpqrstuvwxyz";
        const string digits = "23456789";
        const string symbols = "!@#$%&*?";
        const string all = upper + lower + digits + symbols;

        var chars = new List<char>
        {
            upper[RandomNumberGenerator.GetInt32(upper.Length)],
            lower[RandomNumberGenerator.GetInt32(lower.Length)],
            digits[RandomNumberGenerator.GetInt32(digits.Length)],
            symbols[RandomNumberGenerator.GetInt32(symbols.Length)]
        };
        while (chars.Count < 12)
            chars.Add(all[RandomNumberGenerator.GetInt32(all.Length)]);

        // Fisher-Yates shuffle with a cryptographic RNG.
        for (var i = chars.Count - 1; i > 0; i--)
        {
            var j = RandomNumberGenerator.GetInt32(i + 1);
            (chars[i], chars[j]) = (chars[j], chars[i]);
        }
        return new string(chars.ToArray());
    }
}
