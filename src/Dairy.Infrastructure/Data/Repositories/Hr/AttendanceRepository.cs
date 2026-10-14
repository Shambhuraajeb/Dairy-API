using Dairy.Application.Common;
using Dairy.Application.DTOs.Hr;
using Dairy.Application.Interfaces;
using Dairy.Application.Interfaces.Hr;
using Dairy.Application.Requests.Hr;
using Dapper;

namespace Dairy.Infrastructure.Data.Repositories.Hr;

public class AttendanceRepository : IAttendanceRepository
{
    private static readonly Dictionary<string, string> SortColumns = new(StringComparer.OrdinalIgnoreCase)
    {
        ["id"] = "a.AttendanceId",
        ["attendanceDate"] = "a.AttendanceDate",
        ["employeeName"] = "e.FullName",
        ["employeeCode"] = "e.EmployeeCode",
        ["status"] = "a.Status",
        ["workedHours"] = "a.WorkedHours"
    };

    private const string From =
        " FROM `Attendance` a JOIN `Employee` e ON e.EmployeeId = a.EmployeeId";

    private const string Select =
        "SELECT a.AttendanceId AS Id, a.EmployeeId, e.EmployeeCode, e.FullName AS EmployeeName, a.AttendanceDate, " +
        "a.CheckIn, a.CheckOut, a.WorkedHours, a.Status, a.Remarks" + From;

    private const string Where =
        " WHERE a.IsActive = 1" +
        " AND (@EmployeeId IS NULL OR a.EmployeeId = @EmployeeId)" +
        " AND (@FromDate IS NULL OR a.AttendanceDate >= @FromDate)" +
        " AND (@ToDate IS NULL OR a.AttendanceDate <= @ToDate)" +
        " AND (@Status IS NULL OR a.Status = @Status)" +
        " AND (@Search IS NULL OR e.FullName LIKE @Search OR e.EmployeeCode LIKE @Search)";

    private const string UpsertSql =
        "INSERT INTO `Attendance` (EmployeeId, AttendanceDate, CheckIn, CheckOut, WorkedHours, Status, Remarks, CreatedBy) " +
        "VALUES (@EmployeeId, @Date, @CheckIn, @CheckOut, @WorkedHours, @Status, @Remarks, @UserId) " +
        "ON DUPLICATE KEY UPDATE CheckIn = VALUES(CheckIn), CheckOut = VALUES(CheckOut), WorkedHours = VALUES(WorkedHours), " +
        "Status = VALUES(Status), Remarks = VALUES(Remarks), IsActive = 1, ModifiedBy = VALUES(CreatedBy)";

    private readonly IDbConnectionFactory _db;

    public AttendanceRepository(IDbConnectionFactory db) => _db = db;

    public async Task<PagedResult<AttendanceDto>> GetPagedAsync(AttendanceQuery query, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        var status = string.IsNullOrWhiteSpace(query.Status) ? null : query.Status.Trim();
        var param = new
        {
            query.EmployeeId,
            FromDate = query.From,
            ToDate = query.To,
            Status = status,
            Search = query.SearchPattern
        };
        return await conn.QueryPagedAsync<AttendanceDto>(
            Select + Where,
            "SELECT COUNT(*)" + From + Where,
            param, query, SortColumns, "a.AttendanceDate DESC, e.FullName ASC", ct);
    }

    public async Task<List<AttendanceSheetRowDto>> GetSheetAsync(DateTime date, long? designationId, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        return await conn.QueryListAsync<AttendanceSheetRowDto>(
            "SELECT e.EmployeeId, e.EmployeeCode, e.FullName AS EmployeeName, d.Name AS DesignationName, " +
            "a.AttendanceId, a.Status, a.CheckIn, a.CheckOut, a.WorkedHours, a.Remarks " +
            "FROM `Employee` e " +
            "JOIN `Designation` d ON d.DesignationId = e.DesignationId " +
            "LEFT JOIN `Attendance` a ON a.EmployeeId = e.EmployeeId AND a.AttendanceDate = @OnDate AND a.IsActive = 1 " +
            "WHERE e.IsActive = 1 AND e.Status = 'Active' AND e.JoinDate <= @OnDate AND (e.ExitDate IS NULL OR e.ExitDate >= @OnDate) " +
            "AND (@DesignationId IS NULL OR e.DesignationId = @DesignationId) " +
            "ORDER BY e.FullName",
            new { OnDate = date, DesignationId = designationId }, ct);
    }

    public async Task<AttendanceDto?> GetByEmployeeDateAsync(long employeeId, DateTime date, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        return await conn.QueryFirstOrNullAsync<AttendanceDto>(
            Select + " WHERE a.EmployeeId = @EmployeeId AND a.AttendanceDate = @OnDate AND a.IsActive = 1",
            new { EmployeeId = employeeId, OnDate = date }, ct);
    }

    public async Task<HashSet<long>> GetActiveEmployeeIdsAsync(IEnumerable<long> employeeIds, CancellationToken ct = default)
    {
        var ids = employeeIds.Distinct().ToList();
        if (ids.Count == 0) return new HashSet<long>();

        await using var conn = await _db.OpenAsync(ct);
        var found = await conn.QueryListAsync<long>(
            "SELECT EmployeeId FROM `Employee` WHERE IsActive = 1 AND Status = 'Active' AND EmployeeId IN @Ids",
            new { Ids = ids }, ct);
        return found.ToHashSet();
    }

    public async Task UpsertAsync(IReadOnlyCollection<AttendanceUpsertModel> entries, long userId, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        await using var tx = await conn.BeginTransactionAsync(ct);
        try
        {
            foreach (var e in entries)
            {
                await conn.ExecuteAsync(new CommandDefinition(UpsertSql, new
                {
                    e.EmployeeId,
                    e.Date,
                    e.CheckIn,
                    e.CheckOut,
                    e.WorkedHours,
                    e.Status,
                    e.Remarks,
                    UserId = userId
                }, tx, cancellationToken: ct));
            }
            await tx.CommitAsync(ct);
        }
        catch
        {
            await tx.RollbackAsync(CancellationToken.None);
            throw;
        }
    }
}
