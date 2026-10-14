using Dairy.Application.Common;
using Dairy.Application.DTOs.Hr;
using Dairy.Application.Interfaces;
using Dairy.Application.Interfaces.Hr;
using Dairy.Application.Requests.Hr;

namespace Dairy.Infrastructure.Data.Repositories.Hr;

public class EmployeeRepository : IEmployeeRepository
{
    private static readonly Dictionary<string, string> SortColumns = new(StringComparer.OrdinalIgnoreCase)
    {
        ["id"] = "e.EmployeeId",
        ["employeeCode"] = "e.EmployeeCode",
        ["fullName"] = "e.FullName",
        ["mobile"] = "e.Mobile",
        ["designationName"] = "d.Name",
        ["joinDate"] = "e.JoinDate",
        ["basicSalary"] = "e.BasicSalary",
        ["status"] = "e.Status",
        ["createdDate"] = "e.CreatedDate"
    };

    private const string From =
        " FROM `Employee` e" +
        " JOIN `Designation` d ON d.DesignationId = e.DesignationId" +
        " LEFT JOIN `Branch` b ON b.BranchId = e.BranchId";

    private const string Select =
        "SELECT e.EmployeeId AS Id, e.EmployeeCode, e.FullName, e.Gender, e.DateOfBirth, e.Mobile, e.Email, e.AddressLine, e.City, " +
        "e.DesignationId, d.Name AS DesignationName, e.BranchId, b.Name AS BranchName, e.JoinDate, e.ExitDate, " +
        "e.BasicSalary, e.DefaultAllowances, e.DefaultDeductions, e.BankName, e.BankAccountNo, e.IfscCode, e.AadhaarNo, e.PanNo, " +
        "e.EmergencyContact, e.Status, e.IsActive, e.CreatedDate" + From;

    private const string Where =
        " WHERE (@Search IS NULL OR e.FullName LIKE @Search OR e.EmployeeCode LIKE @Search OR e.Mobile LIKE @Search OR e.Email LIKE @Search)" +
        " AND (@DesignationId IS NULL OR e.DesignationId = @DesignationId)" +
        " AND (@Status IS NULL OR e.Status = @Status)" +
        " AND (@BranchId IS NULL OR e.BranchId = @BranchId)";

    private readonly IDbConnectionFactory _db;

    public EmployeeRepository(IDbConnectionFactory db) => _db = db;

    public async Task<PagedResult<EmployeeDto>> GetPagedAsync(EmployeeQuery query, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        var status = string.IsNullOrWhiteSpace(query.Status) ? null : query.Status.Trim();
        var param = new { Search = query.SearchPattern, query.DesignationId, Status = status, query.BranchId };
        return await conn.QueryPagedAsync<EmployeeDto>(
            Select + Where,
            "SELECT COUNT(*)" + From + Where,
            param, query, SortColumns, "e.FullName ASC", ct);
    }

    public async Task<EmployeeDto?> GetByIdAsync(long id, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        return await conn.QueryFirstOrNullAsync<EmployeeDto>(Select + " WHERE e.EmployeeId = @Id", new { Id = id }, ct);
    }

    public async Task<bool> CodeExistsAsync(string code, long? excludeId, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        var count = await conn.ScalarAsync<int>(
            "SELECT COUNT(1) FROM `Employee` WHERE EmployeeCode = @Code AND (@ExcludeId IS NULL OR EmployeeId <> @ExcludeId)",
            new { Code = code, ExcludeId = excludeId }, ct);
        return count > 0;
    }

    public async Task<string> GenerateCodeAsync(CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        var next = await conn.ScalarAsync<long>(
            "SELECT COALESCE(MAX(CAST(SUBSTRING(EmployeeCode, 4) AS UNSIGNED)), 0) + 1 FROM `Employee` WHERE EmployeeCode REGEXP '^EMP[0-9]+$'",
            null, ct);
        return "EMP" + next.ToString("D3");
    }

    public async Task<bool> DesignationActiveAsync(long designationId, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        var count = await conn.ScalarAsync<int>(
            "SELECT COUNT(1) FROM `Designation` WHERE DesignationId = @Id AND IsActive = 1", new { Id = designationId }, ct);
        return count > 0;
    }

    public async Task<bool> BranchExistsAsync(long branchId, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        var count = await conn.ScalarAsync<int>(
            "SELECT COUNT(1) FROM `Branch` WHERE BranchId = @Id AND IsActive = 1", new { Id = branchId }, ct);
        return count > 0;
    }

    public async Task<long> CreateAsync(CreateEmployeeRequest r, string employeeCode, DateTime joinDate, long userId, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        return await conn.InsertAsync(
            "INSERT INTO `Employee` (EmployeeCode, FullName, Gender, DateOfBirth, Mobile, Email, AddressLine, City, DesignationId, BranchId, JoinDate, " +
            "BasicSalary, DefaultAllowances, DefaultDeductions, BankName, BankAccountNo, IfscCode, AadhaarNo, PanNo, EmergencyContact, Status, IsActive, CreatedBy) " +
            "VALUES (@EmployeeCode, @FullName, @Gender, @DateOfBirth, @Mobile, @Email, @AddressLine, @City, @DesignationId, @BranchId, @JoinDate, " +
            "@BasicSalary, @DefaultAllowances, @DefaultDeductions, @BankName, @BankAccountNo, @IfscCode, @AadhaarNo, @PanNo, @EmergencyContact, 'Active', 1, @UserId)",
            new
            {
                EmployeeCode = employeeCode,
                r.FullName,
                r.Gender,
                r.DateOfBirth,
                r.Mobile,
                r.Email,
                r.AddressLine,
                r.City,
                r.DesignationId,
                r.BranchId,
                JoinDate = joinDate,
                r.BasicSalary,
                r.DefaultAllowances,
                r.DefaultDeductions,
                r.BankName,
                r.BankAccountNo,
                r.IfscCode,
                r.AadhaarNo,
                r.PanNo,
                r.EmergencyContact,
                UserId = userId
            }, ct);
    }

    public async Task UpdateAsync(long id, UpdateEmployeeRequest r, DateTime joinDate, long userId, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        await conn.ExecAsync(
            "UPDATE `Employee` SET FullName = @FullName, Gender = @Gender, DateOfBirth = @DateOfBirth, Mobile = @Mobile, Email = @Email, " +
            "AddressLine = @AddressLine, City = @City, DesignationId = @DesignationId, BranchId = @BranchId, JoinDate = @JoinDate, ExitDate = @ExitDate, " +
            "BasicSalary = @BasicSalary, DefaultAllowances = @DefaultAllowances, DefaultDeductions = @DefaultDeductions, BankName = @BankName, " +
            "BankAccountNo = @BankAccountNo, IfscCode = @IfscCode, AadhaarNo = @AadhaarNo, PanNo = @PanNo, EmergencyContact = @EmergencyContact, " +
            "ModifiedBy = @UserId WHERE EmployeeId = @Id",
            new
            {
                Id = id,
                r.FullName,
                r.Gender,
                r.DateOfBirth,
                r.Mobile,
                r.Email,
                r.AddressLine,
                r.City,
                r.DesignationId,
                r.BranchId,
                JoinDate = joinDate,
                r.ExitDate,
                r.BasicSalary,
                r.DefaultAllowances,
                r.DefaultDeductions,
                r.BankName,
                r.BankAccountNo,
                r.IfscCode,
                r.AadhaarNo,
                r.PanNo,
                r.EmergencyContact,
                UserId = userId
            }, ct);
    }

    public async Task SetStatusAsync(long id, string status, bool isActive, DateTime? exitDate, long userId, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        // Reactivation (exitDate null) clears any previous exit date.
        await conn.ExecAsync(
            "UPDATE `Employee` SET Status = @Status, IsActive = @IsActive, ExitDate = @ExitDate, ModifiedBy = @UserId WHERE EmployeeId = @Id",
            new { Id = id, Status = status, IsActive = isActive, ExitDate = exitDate, UserId = userId }, ct);
    }
}
