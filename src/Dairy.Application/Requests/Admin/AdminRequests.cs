using Dairy.Application.Common;

namespace Dairy.Application.Requests.Admin;

// ---------- users ----------
public class UserAdminQuery : PagedQuery
{
    public long? RoleId { get; set; }
    public bool? IsActive { get; set; }

    /// <summary>true = only currently locked accounts.</summary>
    public bool? IsLocked { get; set; }
}

public abstract class UserRequestBase
{
    public string DisplayName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Mobile { get; set; }
    public long RoleId { get; set; }
    public long? FarmerId { get; set; }
    public long? CustomerId { get; set; }
    public long? EmployeeId { get; set; }
    public long? BranchId { get; set; }
    public long? CollectionCenterId { get; set; }
}

public class CreateUserRequest : UserRequestBase
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;

    /// <summary>Default true: the user must change the password at first sign-in.</summary>
    public bool? MustChangePassword { get; set; }
    public bool? IsActive { get; set; }
}

public class UpdateUserRequest : UserRequestBase
{
    /// <summary>Null = unchanged.</summary>
    public bool? IsActive { get; set; }
}

public class ResetPasswordRequest
{
    /// <summary>Blank = generate a temporary password and return it once.</summary>
    public string? NewPassword { get; set; }

    /// <summary>Default true.</summary>
    public bool? MustChangePassword { get; set; }
}

// ---------- roles / permissions ----------
public class RoleAdminQuery : PagedQuery
{
    public bool? IsActive { get; set; }
}

public class CreateRoleRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
}

public class UpdateRoleRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool? IsActive { get; set; }
}

public class SetRolePermissionsRequest
{
    public List<long>? PermissionIds { get; set; }
    public List<string>? Codes { get; set; }
}

// ---------- settings ----------
public class SettingQuery
{
    public string? Group { get; set; }
    public string? Search { get; set; }
}

public class UpdateSettingRequest
{
    public string? Value { get; set; }
}

public class SettingItemRequest
{
    public string Key { get; set; } = string.Empty;
    public string? Value { get; set; }
}

public class BulkUpdateSettingsRequest
{
    public List<SettingItemRequest> Items { get; set; } = new();
}

// ---------- audit ----------
public class AuditLogQuery : PagedQuery
{
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }

    /// <summary>Username (partial match).</summary>
    public string? User { get; set; }
    public long? UserId { get; set; }
    public string? Action { get; set; }

    /// <summary>EntityName (exact).</summary>
    public string? Entity { get; set; }
    public string? EntityId { get; set; }
}

// ---------- organization ----------
public class OrgQuery : PagedQuery
{
    public bool? IsActive { get; set; }
}

public class BranchAdminQuery : OrgQuery
{
    public long? DairyId { get; set; }
}

public class CollectionCenterAdminQuery : OrgQuery
{
    public long? BranchId { get; set; }
}

public class SaveDairyRequest
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? OwnerName { get; set; }
    public string? Mobile { get; set; }
    public string? Email { get; set; }
    public string? AddressLine { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? Pincode { get; set; }
    public string? GstNumber { get; set; }
    public string? LicenseNo { get; set; }
    public string? RegistrationNo { get; set; }

    /// <summary>Update only; null = unchanged.</summary>
    public bool? IsActive { get; set; }
}

public class SaveBranchRequest
{
    public long DairyId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? AddressLine { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? Pincode { get; set; }
    public string? Mobile { get; set; }
    public string? Email { get; set; }
    public string? ManagerName { get; set; }
    public bool? IsActive { get; set; }
}

public class SaveCollectionCenterRequest
{
    public long BranchId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? AddressLine { get; set; }
    public string? City { get; set; }
    public string? Pincode { get; set; }
    public string? Mobile { get; set; }
    public string? ContactPerson { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public long? SupervisorEmployeeId { get; set; }
    public decimal? CapacityLiters { get; set; }
    public bool? IsActive { get; set; }
}

// ---------- profile ----------
public class UpdateProfileRequest
{
    public string DisplayName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Mobile { get; set; }
}
