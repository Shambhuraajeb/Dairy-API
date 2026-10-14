using Dairy.Api.Authorization;
using Dairy.Application.Interfaces.Admin;
using Dairy.Application.Requests.Admin;
using Dairy.Domain.Constants;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Dairy.Api.Controllers.Admin;

[Route("api/admin/users")]
[HasPermission(Permissions.UserManage)]
public class UsersAdminController : ApiControllerBase
{
    private readonly IUserAdminService _users;

    public UsersAdminController(IUserAdminService users) => _users = users;

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] UserAdminQuery query, CancellationToken ct) =>
        Paged(await _users.GetPagedAsync(query, ct));

    [HttpGet("{id:long}")]
    public async Task<IActionResult> Get(long id, CancellationToken ct) =>
        Ok(await _users.GetByIdAsync(id, ct));

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateUserRequest request, CancellationToken ct) =>
        Created(await _users.CreateAsync(request, ct), "User created.");

    [HttpPut("{id:long}")]
    public async Task<IActionResult> Update(long id, [FromBody] UpdateUserRequest request, CancellationToken ct) =>
        Ok(await _users.UpdateAsync(id, request, ct), "User updated.");

    [HttpPost("{id:long}/reset-password")]
    public async Task<IActionResult> ResetPassword(long id, [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] ResetPasswordRequest? request, CancellationToken ct) =>
        Ok(await _users.ResetPasswordAsync(id, request, ct), "Password reset.");

    [HttpPost("{id:long}/unlock")]
    public async Task<IActionResult> Unlock(long id, CancellationToken ct) =>
        Ok(await _users.UnlockAsync(id, ct), "User unlocked.");
}

[Route("api/admin/roles")]
[HasPermission(Permissions.RoleManage)]
public class RolesAdminController : ApiControllerBase
{
    private readonly IRoleAdminService _roles;

    public RolesAdminController(IRoleAdminService roles) => _roles = roles;

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] RoleAdminQuery query, CancellationToken ct) =>
        Paged(await _roles.GetPagedAsync(query, ct));

    [HttpGet("{id:long}")]
    public async Task<IActionResult> Get(long id, CancellationToken ct) =>
        Ok(await _roles.GetByIdAsync(id, ct));

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateRoleRequest request, CancellationToken ct) =>
        Created(await _roles.CreateAsync(request, ct), "Role created.");

    [HttpPut("{id:long}")]
    public async Task<IActionResult> Update(long id, [FromBody] UpdateRoleRequest request, CancellationToken ct) =>
        Ok(await _roles.UpdateAsync(id, request, ct), "Role updated.");

    [HttpDelete("{id:long}")]
    public async Task<IActionResult> Delete(long id, CancellationToken ct)
    {
        await _roles.DeleteAsync(id, ct);
        return Message("Role deleted.");
    }

    /// <summary>Replaces the role's permission mappings. Takes effect at the affected users' next sign-in.</summary>
    [HttpPut("{id:long}/permissions")]
    public async Task<IActionResult> SetPermissions(long id, [FromBody] SetRolePermissionsRequest request, CancellationToken ct) =>
        Ok(await _roles.SetPermissionsAsync(id, request, ct), "Role permissions updated.");
}

[Route("api/admin/permissions")]
[HasPermission(Permissions.RoleManage, Permissions.UserManage)]
public class PermissionsAdminController : ApiControllerBase
{
    private readonly IRoleAdminService _roles;

    public PermissionsAdminController(IRoleAdminService roles) => _roles = roles;

    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct) =>
        Ok(await _roles.GetPermissionGroupsAsync(ct));
}

[Route("api/admin/settings")]
[HasPermission(Permissions.SettingsManage)]
public class SettingsAdminController : ApiControllerBase
{
    private readonly ISettingAdminService _settings;

    public SettingsAdminController(ISettingAdminService settings) => _settings = settings;

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] SettingQuery query, CancellationToken ct) =>
        Ok(await _settings.GetListAsync(query, ct));

    [HttpPut]
    public async Task<IActionResult> BulkUpdate([FromBody] BulkUpdateSettingsRequest request, CancellationToken ct) =>
        Ok(await _settings.BulkUpdateAsync(request, ct), "Settings saved.");

    [HttpPut("{key}")]
    public async Task<IActionResult> Update(string key, [FromBody] UpdateSettingRequest request, CancellationToken ct) =>
        Ok(await _settings.UpdateAsync(key, request, ct), "Setting saved.");
}

[Route("api/admin/audit-logs")]
[HasPermission(Permissions.AuditView)]
public class AuditLogsAdminController : ApiControllerBase
{
    private readonly IAuditLogAdminService _audit;

    public AuditLogsAdminController(IAuditLogAdminService audit) => _audit = audit;

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] AuditLogQuery query, CancellationToken ct) =>
        Paged(await _audit.GetPagedAsync(query, ct));

    /// <summary>Distinct actions and entity names present in the log (filter dropdowns).</summary>
    [HttpGet("filters")]
    public async Task<IActionResult> Filters(CancellationToken ct) =>
        Ok(await _audit.GetFilterOptionsAsync(ct));

    [HttpGet("{id:long}")]
    public async Task<IActionResult> Get(long id, CancellationToken ct) =>
        Ok(await _audit.GetByIdAsync(id, ct));
}

[Route("api/admin/dairy")]
[HasPermission(Permissions.SettingsManage)]
public class DairyAdminController : ApiControllerBase
{
    private readonly IOrganizationAdminService _org;

    public DairyAdminController(IOrganizationAdminService org) => _org = org;

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] OrgQuery query, CancellationToken ct) =>
        Paged(await _org.GetDairiesAsync(query, ct));

    [HttpGet("{id:long}")]
    public async Task<IActionResult> Get(long id, CancellationToken ct) =>
        Ok(await _org.GetDairyAsync(id, ct));

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] SaveDairyRequest request, CancellationToken ct) =>
        Created(await _org.CreateDairyAsync(request, ct), "Dairy created.");

    [HttpPut("{id:long}")]
    public async Task<IActionResult> Update(long id, [FromBody] SaveDairyRequest request, CancellationToken ct) =>
        Ok(await _org.UpdateDairyAsync(id, request, ct), "Dairy updated.");
}

[Route("api/admin/branches")]
[HasPermission(Permissions.SettingsManage)]
public class BranchesAdminController : ApiControllerBase
{
    private readonly IOrganizationAdminService _org;

    public BranchesAdminController(IOrganizationAdminService org) => _org = org;

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] BranchAdminQuery query, CancellationToken ct) =>
        Paged(await _org.GetBranchesAsync(query, ct));

    [HttpGet("{id:long}")]
    public async Task<IActionResult> Get(long id, CancellationToken ct) =>
        Ok(await _org.GetBranchAsync(id, ct));

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] SaveBranchRequest request, CancellationToken ct) =>
        Created(await _org.CreateBranchAsync(request, ct), "Branch created.");

    [HttpPut("{id:long}")]
    public async Task<IActionResult> Update(long id, [FromBody] SaveBranchRequest request, CancellationToken ct) =>
        Ok(await _org.UpdateBranchAsync(id, request, ct), "Branch updated.");
}

[Route("api/admin/collection-centers")]
[HasPermission(Permissions.SettingsManage)]
public class CollectionCentersAdminController : ApiControllerBase
{
    private readonly IOrganizationAdminService _org;

    public CollectionCentersAdminController(IOrganizationAdminService org) => _org = org;

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] CollectionCenterAdminQuery query, CancellationToken ct) =>
        Paged(await _org.GetCentersAsync(query, ct));

    [HttpGet("{id:long}")]
    public async Task<IActionResult> Get(long id, CancellationToken ct) =>
        Ok(await _org.GetCenterAsync(id, ct));

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] SaveCollectionCenterRequest request, CancellationToken ct) =>
        Created(await _org.CreateCenterAsync(request, ct), "Collection center created.");

    [HttpPut("{id:long}")]
    public async Task<IActionResult> Update(long id, [FromBody] SaveCollectionCenterRequest request, CancellationToken ct) =>
        Ok(await _org.UpdateCenterAsync(id, request, ct), "Collection center updated.");
}

/// <summary>Own profile; any authenticated user.</summary>
[Route("api/profile")]
public class ProfileController : ApiControllerBase
{
    private readonly IProfileService _profile;

    public ProfileController(IProfileService profile) => _profile = profile;

    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken ct) =>
        Ok(await _profile.GetAsync(ct));

    [HttpPut]
    public async Task<IActionResult> Update([FromBody] UpdateProfileRequest request, CancellationToken ct) =>
        Ok(await _profile.UpdateAsync(request, ct), "Profile updated.");
}
