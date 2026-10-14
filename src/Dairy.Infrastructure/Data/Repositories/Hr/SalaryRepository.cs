using Dairy.Application.Common;
using Dairy.Application.DTOs.Hr;
using Dairy.Application.Interfaces;
using Dairy.Application.Interfaces.Hr;
using Dairy.Application.Requests.Hr;

namespace Dairy.Infrastructure.Data.Repositories.Hr;

public class SalaryRepository : ISalaryRepository
{
    private static readonly Dictionary<string, string> SortColumns = new(StringComparer.OrdinalIgnoreCase)
    {
        ["id"] = "s.SalaryId",
        ["employeeName"] = "e.FullName",
        ["employeeCode"] = "e.EmployeeCode",
        ["salaryYear"] = "s.SalaryYear",
        ["salaryMonth"] = "s.SalaryMonth",
        ["netAmount"] = "s.NetAmount",
        ["status"] = "s.Status",
        ["paidDate"] = "s.PaidDate"
    };

    private const string From =
        " FROM `Salary` s" +
        " JOIN `Employee` e ON e.EmployeeId = s.EmployeeId" +
        " LEFT JOIN `Designation` d ON d.DesignationId = e.DesignationId" +
        " LEFT JOIN `PaymentMode` pm ON pm.PaymentModeId = s.PaymentModeId";

    private const string Select =
        "SELECT s.SalaryId AS Id, s.EmployeeId, e.EmployeeCode, e.FullName AS EmployeeName, d.Name AS DesignationName, " +
        "s.SalaryMonth, s.SalaryYear, s.WorkingDays, s.PresentDays, s.LeaveDays, s.BasicAmount, s.Allowances, s.Deductions, s.NetAmount, " +
        "s.Status, s.PaymentModeId, pm.Name AS PaymentModeName, s.ReferenceNo, s.PaidDate, s.Remarks" + From;

    private const string Where =
        " WHERE s.IsActive = 1" +
        " AND (@Month IS NULL OR s.SalaryMonth = @Month)" +
        " AND (@Year IS NULL OR s.SalaryYear = @Year)" +
        " AND (@EmployeeId IS NULL OR s.EmployeeId = @EmployeeId)" +
        " AND (@Status IS NULL OR s.Status = @Status)" +
        " AND (@Search IS NULL OR e.FullName LIKE @Search OR e.EmployeeCode LIKE @Search)";

    private readonly IDbConnectionFactory _db;

    public SalaryRepository(IDbConnectionFactory db) => _db = db;

    public async Task<PagedResult<SalaryDto>> GetPagedAsync(SalaryQuery query, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        var param = new
        {
            query.Month,
            query.Year,
            query.EmployeeId,
            Status = string.IsNullOrWhiteSpace(query.Status) ? null : query.Status.Trim(),
            Search = query.SearchPattern
        };
        return await conn.QueryPagedAsync<SalaryDto>(
            Select + Where,
            "SELECT COUNT(*)" + From + Where,
            param, query, SortColumns, "s.SalaryYear DESC, s.SalaryMonth DESC, e.FullName ASC", ct);
    }

    public async Task<SalaryDto?> GetByIdAsync(long id, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        return await conn.QueryFirstOrNullAsync<SalaryDto>(Select + " WHERE s.SalaryId = @Id", new { Id = id }, ct);
    }

    public async Task<SalaryGenerateResultDto> GenerateAsync(int month, int year, long userId, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        var result = await conn.QuerySpFirstOrDefaultAsync<SalaryGenerateResultDto>(
            "sp_GenerateSalaries", new { p_Month = month, p_Year = year, p_UserId = userId }, ct);
        return result ?? new SalaryGenerateResultDto { SalaryMonth = month, SalaryYear = year };
    }

    public async Task PayAsync(long id, long paymentModeId, long userId, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        await conn.ExecuteSpAsync("sp_PaySalary", new { p_SalaryId = id, p_PaymentModeId = paymentModeId, p_UserId = userId }, ct);
    }
}
