using Dairy.Application.Common;
using Dairy.Application.DTOs.Admin;
using Dairy.Application.Interfaces;
using Dairy.Application.Interfaces.Admin;
using Dairy.Application.Requests.Admin;

namespace Dairy.Infrastructure.Data.Repositories.Admin;

public class OrganizationAdminRepository : IOrganizationAdminRepository
{
    private static readonly Dictionary<string, string> DairySort = new(StringComparer.OrdinalIgnoreCase)
    {
        ["id"] = "d.DairyId", ["code"] = "d.Code", ["name"] = "d.Name", ["city"] = "d.City", ["isActive"] = "d.IsActive"
    };

    private static readonly Dictionary<string, string> BranchSort = new(StringComparer.OrdinalIgnoreCase)
    {
        ["id"] = "b.BranchId", ["code"] = "b.Code", ["name"] = "b.Name", ["city"] = "b.City", ["dairyName"] = "d.Name", ["isActive"] = "b.IsActive"
    };

    private static readonly Dictionary<string, string> CenterSort = new(StringComparer.OrdinalIgnoreCase)
    {
        ["id"] = "cc.CollectionCenterId", ["code"] = "cc.Code", ["name"] = "cc.Name", ["city"] = "cc.City",
        ["branchName"] = "b.Name", ["isActive"] = "cc.IsActive"
    };

    private const string DairySelect =
        "SELECT d.DairyId AS Id, d.Code, d.Name, d.OwnerName, d.Mobile, d.Email, d.AddressLine, d.City, d.State, d.Pincode, " +
        "d.GstNumber, d.LicenseNo, d.RegistrationNo, d.IsActive FROM `Dairy` d";

    private const string DairyWhere =
        " WHERE (@Search IS NULL OR d.Code LIKE @Search OR d.Name LIKE @Search OR d.City LIKE @Search)" +
        " AND (@IsActive IS NULL OR d.IsActive = @IsActive)";

    private const string BranchFrom =
        " FROM `Branch` b JOIN `Dairy` d ON d.DairyId = b.DairyId";

    private const string BranchSelect =
        "SELECT b.BranchId AS Id, b.DairyId, d.Name AS DairyName, b.Code, b.Name, b.AddressLine, b.City, b.State, b.Pincode, " +
        "b.Mobile, b.Email, b.ManagerName, b.IsActive" + BranchFrom;

    private const string BranchWhere =
        " WHERE (@Search IS NULL OR b.Code LIKE @Search OR b.Name LIKE @Search OR b.City LIKE @Search)" +
        " AND (@IsActive IS NULL OR b.IsActive = @IsActive)" +
        " AND (@DairyId IS NULL OR b.DairyId = @DairyId)";

    private const string CenterFrom =
        " FROM `CollectionCenter` cc JOIN `Branch` b ON b.BranchId = cc.BranchId" +
        " LEFT JOIN `Employee` e ON e.EmployeeId = cc.SupervisorEmployeeId";

    private const string CenterSelect =
        "SELECT cc.CollectionCenterId AS Id, cc.BranchId, b.Name AS BranchName, cc.Code, cc.Name, cc.AddressLine, cc.City, cc.Pincode, " +
        "cc.Mobile, cc.ContactPerson, cc.Latitude, cc.Longitude, cc.SupervisorEmployeeId, e.FullName AS SupervisorName, " +
        "cc.CapacityLiters, cc.IsActive" + CenterFrom;

    private const string CenterWhere =
        " WHERE (@Search IS NULL OR cc.Code LIKE @Search OR cc.Name LIKE @Search OR cc.City LIKE @Search)" +
        " AND (@IsActive IS NULL OR cc.IsActive = @IsActive)" +
        " AND (@BranchId IS NULL OR cc.BranchId = @BranchId)";

    private readonly IDbConnectionFactory _db;

    public OrganizationAdminRepository(IDbConnectionFactory db) => _db = db;

    // ---------- dairy ----------
    public async Task<PagedResult<DairyDto>> GetDairiesAsync(OrgQuery query, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        var param = new { Search = query.SearchPattern, query.IsActive };
        return await conn.QueryPagedAsync<DairyDto>(
            DairySelect + DairyWhere, "SELECT COUNT(*) FROM `Dairy` d" + DairyWhere,
            param, query, DairySort, "d.Name ASC", ct);
    }

    public async Task<DairyDto?> GetDairyAsync(long id, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        return await conn.QueryFirstOrNullAsync<DairyDto>(DairySelect + " WHERE d.DairyId = @Id", new { Id = id }, ct);
    }

    public Task<bool> DairyCodeExistsAsync(string code, long? excludeId, CancellationToken ct = default) =>
        CodeExistsAsync("SELECT COUNT(1) FROM `Dairy` WHERE Code = @Code AND (@ExcludeId IS NULL OR DairyId <> @ExcludeId)", code, excludeId, ct);

    public async Task<long> CreateDairyAsync(SaveDairyRequest r, long actorId, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        return await conn.InsertAsync(
            "INSERT INTO `Dairy` (Code, Name, OwnerName, Mobile, Email, AddressLine, City, State, Pincode, GstNumber, LicenseNo, RegistrationNo, IsActive, CreatedBy) " +
            "VALUES (@Code, @Name, @OwnerName, @Mobile, @Email, @AddressLine, @City, @State, @Pincode, @GstNumber, @LicenseNo, @RegistrationNo, 1, @ActorId)",
            new
            {
                r.Code, r.Name, r.OwnerName, r.Mobile, r.Email, r.AddressLine, r.City, r.State, r.Pincode,
                r.GstNumber, r.LicenseNo, r.RegistrationNo, ActorId = actorId
            }, ct);
    }

    public async Task UpdateDairyAsync(long id, SaveDairyRequest r, bool isActive, long actorId, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        await conn.ExecAsync(
            "UPDATE `Dairy` SET Code = @Code, Name = @Name, OwnerName = @OwnerName, Mobile = @Mobile, Email = @Email, AddressLine = @AddressLine, " +
            "City = @City, State = @State, Pincode = @Pincode, GstNumber = @GstNumber, LicenseNo = @LicenseNo, RegistrationNo = @RegistrationNo, " +
            "IsActive = @IsActive, ModifiedBy = @ActorId WHERE DairyId = @Id",
            new
            {
                Id = id, r.Code, r.Name, r.OwnerName, r.Mobile, r.Email, r.AddressLine, r.City, r.State, r.Pincode,
                r.GstNumber, r.LicenseNo, r.RegistrationNo, IsActive = isActive, ActorId = actorId
            }, ct);
    }

    // ---------- branches ----------
    public async Task<PagedResult<BranchDto>> GetBranchesAsync(BranchAdminQuery query, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        var param = new { Search = query.SearchPattern, query.IsActive, query.DairyId };
        return await conn.QueryPagedAsync<BranchDto>(
            BranchSelect + BranchWhere, "SELECT COUNT(*)" + BranchFrom + BranchWhere,
            param, query, BranchSort, "b.Name ASC", ct);
    }

    public async Task<BranchDto?> GetBranchAsync(long id, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        return await conn.QueryFirstOrNullAsync<BranchDto>(BranchSelect + " WHERE b.BranchId = @Id", new { Id = id }, ct);
    }

    public Task<bool> BranchCodeExistsAsync(string code, long? excludeId, CancellationToken ct = default) =>
        CodeExistsAsync("SELECT COUNT(1) FROM `Branch` WHERE Code = @Code AND (@ExcludeId IS NULL OR BranchId <> @ExcludeId)", code, excludeId, ct);

    public async Task<bool> DairyExistsAsync(long dairyId, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        return await conn.ScalarAsync<int>("SELECT COUNT(1) FROM `Dairy` WHERE DairyId = @Id AND IsActive = 1", new { Id = dairyId }, ct) > 0;
    }

    public async Task<long> CreateBranchAsync(SaveBranchRequest r, long actorId, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        return await conn.InsertAsync(
            "INSERT INTO `Branch` (DairyId, Code, Name, AddressLine, City, State, Pincode, Mobile, Email, ManagerName, IsActive, CreatedBy) " +
            "VALUES (@DairyId, @Code, @Name, @AddressLine, @City, @State, @Pincode, @Mobile, @Email, @ManagerName, 1, @ActorId)",
            new { r.DairyId, r.Code, r.Name, r.AddressLine, r.City, r.State, r.Pincode, r.Mobile, r.Email, r.ManagerName, ActorId = actorId }, ct);
    }

    public async Task UpdateBranchAsync(long id, SaveBranchRequest r, bool isActive, long actorId, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        await conn.ExecAsync(
            "UPDATE `Branch` SET DairyId = @DairyId, Code = @Code, Name = @Name, AddressLine = @AddressLine, City = @City, State = @State, " +
            "Pincode = @Pincode, Mobile = @Mobile, Email = @Email, ManagerName = @ManagerName, IsActive = @IsActive, ModifiedBy = @ActorId " +
            "WHERE BranchId = @Id",
            new
            {
                Id = id, r.DairyId, r.Code, r.Name, r.AddressLine, r.City, r.State, r.Pincode, r.Mobile, r.Email, r.ManagerName,
                IsActive = isActive, ActorId = actorId
            }, ct);
    }

    public async Task<int> CountActiveCentersAsync(long branchId, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        return await conn.ScalarAsync<int>(
            "SELECT COUNT(1) FROM `CollectionCenter` WHERE BranchId = @Id AND IsActive = 1", new { Id = branchId }, ct);
    }

    // ---------- collection centers ----------
    public async Task<PagedResult<CollectionCenterDto>> GetCentersAsync(CollectionCenterAdminQuery query, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        var param = new { Search = query.SearchPattern, query.IsActive, query.BranchId };
        return await conn.QueryPagedAsync<CollectionCenterDto>(
            CenterSelect + CenterWhere, "SELECT COUNT(*)" + CenterFrom + CenterWhere,
            param, query, CenterSort, "cc.Name ASC", ct);
    }

    public async Task<CollectionCenterDto?> GetCenterAsync(long id, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        return await conn.QueryFirstOrNullAsync<CollectionCenterDto>(CenterSelect + " WHERE cc.CollectionCenterId = @Id", new { Id = id }, ct);
    }

    public Task<bool> CenterCodeExistsAsync(string code, long? excludeId, CancellationToken ct = default) =>
        CodeExistsAsync("SELECT COUNT(1) FROM `CollectionCenter` WHERE Code = @Code AND (@ExcludeId IS NULL OR CollectionCenterId <> @ExcludeId)", code, excludeId, ct);

    public async Task<bool> BranchExistsAsync(long branchId, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        return await conn.ScalarAsync<int>("SELECT COUNT(1) FROM `Branch` WHERE BranchId = @Id AND IsActive = 1", new { Id = branchId }, ct) > 0;
    }

    public async Task<bool> EmployeeExistsAsync(long employeeId, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        return await conn.ScalarAsync<int>("SELECT COUNT(1) FROM `Employee` WHERE EmployeeId = @Id AND IsActive = 1", new { Id = employeeId }, ct) > 0;
    }

    public async Task<long> CreateCenterAsync(SaveCollectionCenterRequest r, long actorId, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        return await conn.InsertAsync(
            "INSERT INTO `CollectionCenter` (BranchId, Code, Name, AddressLine, City, Pincode, Mobile, ContactPerson, Latitude, Longitude, " +
            "SupervisorEmployeeId, CapacityLiters, IsActive, CreatedBy) " +
            "VALUES (@BranchId, @Code, @Name, @AddressLine, @City, @Pincode, @Mobile, @ContactPerson, @Latitude, @Longitude, " +
            "@SupervisorEmployeeId, @CapacityLiters, 1, @ActorId)",
            new
            {
                r.BranchId, r.Code, r.Name, r.AddressLine, r.City, r.Pincode, r.Mobile, r.ContactPerson, r.Latitude, r.Longitude,
                r.SupervisorEmployeeId, r.CapacityLiters, ActorId = actorId
            }, ct);
    }

    public async Task UpdateCenterAsync(long id, SaveCollectionCenterRequest r, bool isActive, long actorId, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        await conn.ExecAsync(
            "UPDATE `CollectionCenter` SET BranchId = @BranchId, Code = @Code, Name = @Name, AddressLine = @AddressLine, City = @City, " +
            "Pincode = @Pincode, Mobile = @Mobile, ContactPerson = @ContactPerson, Latitude = @Latitude, Longitude = @Longitude, " +
            "SupervisorEmployeeId = @SupervisorEmployeeId, CapacityLiters = @CapacityLiters, IsActive = @IsActive, ModifiedBy = @ActorId " +
            "WHERE CollectionCenterId = @Id",
            new
            {
                Id = id, r.BranchId, r.Code, r.Name, r.AddressLine, r.City, r.Pincode, r.Mobile, r.ContactPerson, r.Latitude, r.Longitude,
                r.SupervisorEmployeeId, r.CapacityLiters, IsActive = isActive, ActorId = actorId
            }, ct);
    }

    private async Task<bool> CodeExistsAsync(string sql, string code, long? excludeId, CancellationToken ct)
    {
        await using var conn = await _db.OpenAsync(ct);
        return await conn.ScalarAsync<int>(sql, new { Code = code, ExcludeId = excludeId }, ct) > 0;
    }
}
