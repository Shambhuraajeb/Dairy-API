namespace Dairy.Application.DTOs.Admin;

/// <summary>User row for administration. Never carries the password hash.</summary>
public class UserAdminDto
{
    public long Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Mobile { get; set; }
    public long RoleId { get; set; }
    public string RoleName { get; set; } = string.Empty;
    public long? FarmerId { get; set; }
    public string? FarmerName { get; set; }
    public long? CustomerId { get; set; }
    public string? CustomerName { get; set; }
    public long? EmployeeId { get; set; }
    public string? EmployeeName { get; set; }
    public long? BranchId { get; set; }
    public string? BranchName { get; set; }
    public long? CollectionCenterId { get; set; }
    public string? CollectionCenterName { get; set; }
    public bool IsActive { get; set; }
    public bool IsLocked { get; set; }
    public DateTime? LockedUntil { get; set; }
    public int FailedLoginCount { get; set; }
    public DateTime? LastLoginDate { get; set; }
    public bool MustChangePassword { get; set; }
    public DateTime CreatedDate { get; set; }
}

public class ResetPasswordResultDto
{
    /// <summary>Only set when the server generated the password (no newPassword supplied). Shown once.</summary>
    public string? TemporaryPassword { get; set; }
}

public class RoleDto
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsSystem { get; set; }
    public bool IsActive { get; set; }
    public int UserCount { get; set; }
    public int PermissionCount { get; set; }
}

public class RoleDetailDto : RoleDto
{
    public List<string> PermissionCodes { get; set; } = new();
    public List<long> PermissionIds { get; set; } = new();
}

public class PermissionDto
{
    public long Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Module { get; set; } = string.Empty;
}

public class PermissionGroupDto
{
    public string Module { get; set; } = string.Empty;
    public List<PermissionDto> Permissions { get; set; } = new();
}

public class SettingDto
{
    public long Id { get; set; }
    public string Key { get; set; } = string.Empty;
    public string? Value { get; set; }
    public string? Description { get; set; }
    public string? Group { get; set; }

    /// <summary>string | number | bool | json</summary>
    public string DataType { get; set; } = "string";
    public bool IsActive { get; set; }
    public DateTime? ModifiedDate { get; set; }
}

public class AuditLogListDto
{
    public long Id { get; set; }
    public long? UserId { get; set; }
    public string? Username { get; set; }
    public string Action { get; set; } = string.Empty;
    public string EntityName { get; set; } = string.Empty;
    public string? EntityId { get; set; }
    public string? IpAddress { get; set; }
    public DateTime CreatedDate { get; set; }
}

public class AuditLogDto : AuditLogListDto
{
    public string? OldValues { get; set; }
    public string? NewValues { get; set; }
    public string? UserAgent { get; set; }
}

public class AuditFilterOptionsDto
{
    public List<string> Actions { get; set; } = new();
    public List<string> Entities { get; set; } = new();
}

public class DairyDto
{
    public long Id { get; set; }
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
    public bool IsActive { get; set; }
}

public class BranchDto
{
    public long Id { get; set; }
    public long DairyId { get; set; }
    public string? DairyName { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? AddressLine { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? Pincode { get; set; }
    public string? Mobile { get; set; }
    public string? Email { get; set; }
    public string? ManagerName { get; set; }
    public bool IsActive { get; set; }
}

public class CollectionCenterDto
{
    public long Id { get; set; }
    public long BranchId { get; set; }
    public string? BranchName { get; set; }
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
    public string? SupervisorName { get; set; }
    public decimal? CapacityLiters { get; set; }
    public bool IsActive { get; set; }
}

public class ProfileDto
{
    public long Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Mobile { get; set; }
    public string RoleName { get; set; } = string.Empty;
    public long? FarmerId { get; set; }
    public long? CustomerId { get; set; }
    public long? EmployeeId { get; set; }
    public long? BranchId { get; set; }
    public string? BranchName { get; set; }
    public long? CollectionCenterId { get; set; }
    public string? CollectionCenterName { get; set; }
    public DateTime? LastLoginDate { get; set; }
}
