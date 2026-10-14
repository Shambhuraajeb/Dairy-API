using Dairy.Application.Common;
using Dairy.Application.DTOs.Admin;
using Dairy.Application.Requests.Admin;

namespace Dairy.Application.Interfaces.Admin;

// ---------- users ----------
public interface IUserAdminRepository
{
    Task<PagedResult<UserAdminDto>> GetPagedAsync(UserAdminQuery query, CancellationToken ct = default);
    Task<UserAdminDto?> GetByIdAsync(long id, CancellationToken ct = default);
    Task<bool> UsernameExistsAsync(string username, long? excludeId, CancellationToken ct = default);

    /// <summary>Role name for an active role id, or null.</summary>
    Task<string?> GetActiveRoleNameAsync(long roleId, CancellationToken ct = default);

    /// <summary>kind: farmer | customer | employee | branch | collectionCenter.</summary>
    Task<bool> LinkedEntityExistsAsync(string kind, long id, CancellationToken ct = default);
    Task<int> CountOtherActiveSuperAdminsAsync(long excludeUserId, CancellationToken ct = default);
    Task<long> CreateAsync(CreateUserRequest request, string passwordHash, bool mustChangePassword, bool isActive, long actorId, CancellationToken ct = default);
    Task UpdateAsync(long id, UpdateUserRequest request, bool isActive, long actorId, CancellationToken ct = default);
    Task SetPasswordAsync(long id, string passwordHash, bool mustChangePassword, long actorId, CancellationToken ct = default);
    Task UnlockAsync(long id, long actorId, CancellationToken ct = default);
}

public interface IUserAdminService
{
    Task<PagedResult<UserAdminDto>> GetPagedAsync(UserAdminQuery query, CancellationToken ct = default);
    Task<UserAdminDto> GetByIdAsync(long id, CancellationToken ct = default);
    Task<UserAdminDto> CreateAsync(CreateUserRequest request, CancellationToken ct = default);
    Task<UserAdminDto> UpdateAsync(long id, UpdateUserRequest request, CancellationToken ct = default);
    Task<ResetPasswordResultDto> ResetPasswordAsync(long id, ResetPasswordRequest? request, CancellationToken ct = default);
    Task<UserAdminDto> UnlockAsync(long id, CancellationToken ct = default);
}

// ---------- roles & permissions ----------
public interface IRoleAdminRepository
{
    Task<PagedResult<RoleDto>> GetPagedAsync(RoleAdminQuery query, CancellationToken ct = default);
    Task<RoleDto?> GetByIdAsync(long id, CancellationToken ct = default);
    Task<List<PermissionDto>> GetRolePermissionsAsync(long roleId, CancellationToken ct = default);
    Task<List<PermissionDto>> GetAllPermissionsAsync(CancellationToken ct = default);
    Task<List<PermissionDto>> FindPermissionsAsync(IReadOnlyCollection<long> ids, IReadOnlyCollection<string> codes, CancellationToken ct = default);
    Task<bool> NameExistsAsync(string name, long? excludeId, CancellationToken ct = default);
    Task<long> CreateAsync(string name, string? description, long actorId, CancellationToken ct = default);
    Task UpdateAsync(long id, string name, string? description, bool isActive, long actorId, CancellationToken ct = default);
    Task DeleteAsync(long id, CancellationToken ct = default);
    Task ReplacePermissionsAsync(long roleId, IReadOnlyCollection<long> permissionIds, long actorId, CancellationToken ct = default);
}

public interface IRoleAdminService
{
    Task<PagedResult<RoleDto>> GetPagedAsync(RoleAdminQuery query, CancellationToken ct = default);
    Task<RoleDetailDto> GetByIdAsync(long id, CancellationToken ct = default);
    Task<RoleDetailDto> CreateAsync(CreateRoleRequest request, CancellationToken ct = default);
    Task<RoleDetailDto> UpdateAsync(long id, UpdateRoleRequest request, CancellationToken ct = default);
    Task DeleteAsync(long id, CancellationToken ct = default);
    Task<RoleDetailDto> SetPermissionsAsync(long id, SetRolePermissionsRequest request, CancellationToken ct = default);
    Task<List<PermissionGroupDto>> GetPermissionGroupsAsync(CancellationToken ct = default);
}

// ---------- settings ----------
public interface ISettingAdminRepository
{
    Task<List<SettingDto>> GetListAsync(SettingQuery query, CancellationToken ct = default);
    Task<SettingDto?> GetByKeyAsync(string key, CancellationToken ct = default);
    Task UpdateValueAsync(string key, string? value, long actorId, CancellationToken ct = default);
}

public interface ISettingAdminService
{
    Task<List<SettingDto>> GetListAsync(SettingQuery query, CancellationToken ct = default);
    Task<SettingDto> UpdateAsync(string key, UpdateSettingRequest request, CancellationToken ct = default);
    Task<List<SettingDto>> BulkUpdateAsync(BulkUpdateSettingsRequest request, CancellationToken ct = default);
}

// ---------- audit ----------
public interface IAuditLogAdminRepository
{
    Task<PagedResult<AuditLogListDto>> GetPagedAsync(AuditLogQuery query, CancellationToken ct = default);
    Task<AuditLogDto?> GetByIdAsync(long id, CancellationToken ct = default);
    Task<AuditFilterOptionsDto> GetFilterOptionsAsync(CancellationToken ct = default);
}

public interface IAuditLogAdminService
{
    Task<PagedResult<AuditLogListDto>> GetPagedAsync(AuditLogQuery query, CancellationToken ct = default);
    Task<AuditLogDto> GetByIdAsync(long id, CancellationToken ct = default);
    Task<AuditFilterOptionsDto> GetFilterOptionsAsync(CancellationToken ct = default);
}

// ---------- organization (dairy / branches / collection centers) ----------
public interface IOrganizationAdminRepository
{
    Task<PagedResult<DairyDto>> GetDairiesAsync(OrgQuery query, CancellationToken ct = default);
    Task<DairyDto?> GetDairyAsync(long id, CancellationToken ct = default);
    Task<bool> DairyCodeExistsAsync(string code, long? excludeId, CancellationToken ct = default);
    Task<long> CreateDairyAsync(SaveDairyRequest r, long actorId, CancellationToken ct = default);
    Task UpdateDairyAsync(long id, SaveDairyRequest r, bool isActive, long actorId, CancellationToken ct = default);

    Task<PagedResult<BranchDto>> GetBranchesAsync(BranchAdminQuery query, CancellationToken ct = default);
    Task<BranchDto?> GetBranchAsync(long id, CancellationToken ct = default);
    Task<bool> BranchCodeExistsAsync(string code, long? excludeId, CancellationToken ct = default);
    Task<bool> DairyExistsAsync(long dairyId, CancellationToken ct = default);
    Task<long> CreateBranchAsync(SaveBranchRequest r, long actorId, CancellationToken ct = default);
    Task UpdateBranchAsync(long id, SaveBranchRequest r, bool isActive, long actorId, CancellationToken ct = default);
    Task<int> CountActiveCentersAsync(long branchId, CancellationToken ct = default);

    Task<PagedResult<CollectionCenterDto>> GetCentersAsync(CollectionCenterAdminQuery query, CancellationToken ct = default);
    Task<CollectionCenterDto?> GetCenterAsync(long id, CancellationToken ct = default);
    Task<bool> CenterCodeExistsAsync(string code, long? excludeId, CancellationToken ct = default);
    Task<bool> BranchExistsAsync(long branchId, CancellationToken ct = default);
    Task<bool> EmployeeExistsAsync(long employeeId, CancellationToken ct = default);
    Task<long> CreateCenterAsync(SaveCollectionCenterRequest r, long actorId, CancellationToken ct = default);
    Task UpdateCenterAsync(long id, SaveCollectionCenterRequest r, bool isActive, long actorId, CancellationToken ct = default);
}

public interface IOrganizationAdminService
{
    Task<PagedResult<DairyDto>> GetDairiesAsync(OrgQuery query, CancellationToken ct = default);
    Task<DairyDto> GetDairyAsync(long id, CancellationToken ct = default);
    Task<DairyDto> CreateDairyAsync(SaveDairyRequest request, CancellationToken ct = default);
    Task<DairyDto> UpdateDairyAsync(long id, SaveDairyRequest request, CancellationToken ct = default);

    Task<PagedResult<BranchDto>> GetBranchesAsync(BranchAdminQuery query, CancellationToken ct = default);
    Task<BranchDto> GetBranchAsync(long id, CancellationToken ct = default);
    Task<BranchDto> CreateBranchAsync(SaveBranchRequest request, CancellationToken ct = default);
    Task<BranchDto> UpdateBranchAsync(long id, SaveBranchRequest request, CancellationToken ct = default);

    Task<PagedResult<CollectionCenterDto>> GetCentersAsync(CollectionCenterAdminQuery query, CancellationToken ct = default);
    Task<CollectionCenterDto> GetCenterAsync(long id, CancellationToken ct = default);
    Task<CollectionCenterDto> CreateCenterAsync(SaveCollectionCenterRequest request, CancellationToken ct = default);
    Task<CollectionCenterDto> UpdateCenterAsync(long id, SaveCollectionCenterRequest request, CancellationToken ct = default);
}

// ---------- profile ----------
public interface IProfileRepository
{
    Task<ProfileDto?> GetAsync(long userId, CancellationToken ct = default);
    Task UpdateAsync(long userId, string displayName, string? email, string? mobile, CancellationToken ct = default);
}

public interface IProfileService
{
    Task<ProfileDto> GetAsync(CancellationToken ct = default);
    Task<ProfileDto> UpdateAsync(UpdateProfileRequest request, CancellationToken ct = default);
}
