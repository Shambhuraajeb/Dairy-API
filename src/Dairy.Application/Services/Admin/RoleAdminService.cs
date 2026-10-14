using Dairy.Application.Common;
using Dairy.Application.DTOs.Admin;
using Dairy.Application.Interfaces;
using Dairy.Application.Interfaces.Admin;
using Dairy.Application.Requests.Admin;
using Dairy.Domain.Constants;

namespace Dairy.Application.Services.Admin;

public class RoleAdminService : IRoleAdminService
{
    private readonly IRoleAdminRepository _repo;
    private readonly IAuditService _audit;
    private readonly ICurrentUser _user;

    public RoleAdminService(IRoleAdminRepository repo, IAuditService audit, ICurrentUser user)
    {
        _repo = repo;
        _audit = audit;
        _user = user;
    }

    public Task<PagedResult<RoleDto>> GetPagedAsync(RoleAdminQuery query, CancellationToken ct = default) =>
        _repo.GetPagedAsync(query, ct);

    public async Task<RoleDetailDto> GetByIdAsync(long id, CancellationToken ct = default)
    {
        var role = await _repo.GetByIdAsync(id, ct) ?? throw new NotFoundException("Role", id);
        var permissions = await _repo.GetRolePermissionsAsync(id, ct);
        return ToDetail(role, permissions);
    }

    public async Task<RoleDetailDto> CreateAsync(CreateRoleRequest request, CancellationToken ct = default)
    {
        var name = request.Name.Trim();
        if (await _repo.NameExistsAsync(name, null, ct))
            throw new ConflictException($"A role named '{name}' already exists.");

        var id = await _repo.CreateAsync(name, B5Text.Clean(request.Description), _user.UserId, ct);
        var created = await GetByIdAsync(id, ct);
        await _audit.LogAsync(AuditActions.Create, "Role", id, null, new { created.Id, created.Name, created.Description }, ct);
        return created;
    }

    public async Task<RoleDetailDto> UpdateAsync(long id, UpdateRoleRequest request, CancellationToken ct = default)
    {
        var existing = await GetByIdAsync(id, ct);
        var name = request.Name.Trim();
        var isActive = request.IsActive ?? existing.IsActive;

        if (existing.IsSystem)
        {
            if (!string.Equals(name, existing.Name, StringComparison.Ordinal))
                throw new BusinessRuleException("System roles cannot be renamed.");
            if (!isActive)
                throw new BusinessRuleException("System roles cannot be deactivated.");
        }
        if (await _repo.NameExistsAsync(name, id, ct))
            throw new ConflictException($"A role named '{name}' already exists.");
        if (existing.IsActive && !isActive && existing.UserCount > 0)
            throw new BusinessRuleException($"Cannot deactivate a role assigned to {existing.UserCount} user(s).");

        await _repo.UpdateAsync(id, name, B5Text.Clean(request.Description), isActive, _user.UserId, ct);
        var updated = await GetByIdAsync(id, ct);

        var action = existing.IsActive && !updated.IsActive ? AuditActions.Deactivate : AuditActions.Update;
        await _audit.LogAsync(action, "Role", id,
            new { existing.Name, existing.Description, existing.IsActive },
            new { updated.Name, updated.Description, updated.IsActive }, ct);
        return updated;
    }

    public async Task DeleteAsync(long id, CancellationToken ct = default)
    {
        var existing = await GetByIdAsync(id, ct);
        if (existing.IsSystem)
            throw new BusinessRuleException("System roles cannot be deleted.");
        if (existing.UserCount > 0)
            throw new BusinessRuleException($"Cannot delete a role assigned to {existing.UserCount} user(s). Reassign them first.");

        await _repo.DeleteAsync(id, ct);
        await _audit.LogAsync(AuditActions.Delete, "Role", id, new { existing.Name, existing.Description, existing.PermissionCodes }, null, ct);
    }

    public async Task<RoleDetailDto> SetPermissionsAsync(long id, SetRolePermissionsRequest request, CancellationToken ct = default)
    {
        var existing = await GetByIdAsync(id, ct);

        var ids = (request.PermissionIds ?? new List<long>()).Distinct().ToList();
        var codes = (request.Codes ?? new List<string>())
            .Where(c => !string.IsNullOrWhiteSpace(c)).Select(c => c.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).ToList();

        var resolved = ids.Count == 0 && codes.Count == 0
            ? new List<PermissionDto>()
            : await _repo.FindPermissionsAsync(ids, codes, ct);

        var unknownIds = ids.Where(i => resolved.All(p => p.Id != i)).ToList();
        var unknownCodes = codes.Where(c => resolved.All(p => !string.Equals(p.Code, c, StringComparison.OrdinalIgnoreCase))).ToList();
        if (unknownIds.Count > 0 || unknownCodes.Count > 0)
        {
            var parts = new List<string>();
            if (unknownIds.Count > 0) parts.Add("ids " + string.Join(", ", unknownIds));
            if (unknownCodes.Count > 0) parts.Add("codes " + string.Join(", ", unknownCodes));
            throw new RequestValidationException("permissionIds", "Unknown permission(s): " + string.Join("; ", parts) + ".");
        }

        // Do not let an administrator strip their own access to role management.
        if (string.Equals(_user.Role, existing.Name, StringComparison.OrdinalIgnoreCase) &&
            !_user.IsInRole(Roles.SuperAdmin) &&
            resolved.All(p => p.Code != Permissions.RoleManage))
            throw new BusinessRuleException("You cannot remove 'role.manage' from your own role.");

        var oldCodes = existing.PermissionCodes.OrderBy(c => c, StringComparer.Ordinal).ToList();
        await _repo.ReplacePermissionsAsync(id, resolved.Select(p => p.Id).Distinct().ToList(), _user.UserId, ct);

        var updated = await GetByIdAsync(id, ct);
        await _audit.LogAsync(AuditActions.PermissionChange, "Role", id,
            new { existing.Name, PermissionCodes = oldCodes },
            new { updated.Name, PermissionCodes = updated.PermissionCodes.OrderBy(c => c, StringComparer.Ordinal).ToList() }, ct);
        return updated;
    }

    public async Task<List<PermissionGroupDto>> GetPermissionGroupsAsync(CancellationToken ct = default)
    {
        var all = await _repo.GetAllPermissionsAsync(ct);
        return all
            .GroupBy(p => p.Module)
            .OrderBy(g => g.Key, StringComparer.Ordinal)
            .Select(g => new PermissionGroupDto
            {
                Module = g.Key,
                Permissions = g.OrderBy(p => p.Code, StringComparer.Ordinal).ToList()
            })
            .ToList();
    }

    private static RoleDetailDto ToDetail(RoleDto role, List<PermissionDto> permissions) => new()
    {
        Id = role.Id,
        Name = role.Name,
        Description = role.Description,
        IsSystem = role.IsSystem,
        IsActive = role.IsActive,
        UserCount = role.UserCount,
        PermissionCount = permissions.Count,
        PermissionCodes = permissions.Select(p => p.Code).OrderBy(c => c, StringComparer.Ordinal).ToList(),
        PermissionIds = permissions.Select(p => p.Id).OrderBy(i => i).ToList()
    };
}
