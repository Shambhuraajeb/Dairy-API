using Dairy.Application.Common;
using Dairy.Application.DTOs.Hr;
using Dairy.Application.Interfaces;
using Dairy.Application.Interfaces.Hr;
using Dairy.Application.Requests.Hr;
using Dapper;

namespace Dairy.Infrastructure.Data.Repositories.Hr;

public class LeaveRequestRepository : ILeaveRequestRepository
{
    private static readonly Dictionary<string, string> SortColumns = new(StringComparer.OrdinalIgnoreCase)
    {
        ["id"] = "l.LeaveRequestId",
        ["employeeName"] = "e.FullName",
        ["leaveType"] = "l.LeaveType",
        ["fromDate"] = "l.FromDate",
        ["toDate"] = "l.ToDate",
        ["days"] = "l.Days",
        ["status"] = "l.Status",
        ["createdDate"] = "l.CreatedDate"
    };

    private const string From =
        " FROM `LeaveRequest` l" +
        " JOIN `Employee` e ON e.EmployeeId = l.EmployeeId" +
        " LEFT JOIN `User` au ON au.UserId = l.ApprovedBy";

    private const string Select =
        "SELECT l.LeaveRequestId AS Id, l.EmployeeId, e.EmployeeCode, e.FullName AS EmployeeName, l.LeaveType, l.FromDate, l.ToDate, " +
        "l.Days, l.Reason, l.Status, l.ApprovedBy, au.DisplayName AS ApprovedByName, l.ApprovedDate, l.RejectionReason, l.CreatedDate" + From;

    private const string Where =
        " WHERE l.IsActive = 1" +
        " AND (@EmployeeId IS NULL OR l.EmployeeId = @EmployeeId)" +
        " AND (@Status IS NULL OR l.Status = @Status)" +
        " AND (@LeaveType IS NULL OR l.LeaveType = @LeaveType)" +
        " AND (@FromDate IS NULL OR l.ToDate >= @FromDate)" +
        " AND (@ToDate IS NULL OR l.FromDate <= @ToDate)" +
        " AND (@Search IS NULL OR e.FullName LIKE @Search OR e.EmployeeCode LIKE @Search OR l.Reason LIKE @Search)";

    private const string AttendanceFillSql =
        "INSERT INTO `Attendance` (EmployeeId, AttendanceDate, WorkedHours, Status, Remarks, CreatedBy) " +
        "VALUES (@EmployeeId, @Date, 0, @Status, 'Approved leave', @UserId) " +
        "ON DUPLICATE KEY UPDATE Status = IF(CheckIn IS NULL, VALUES(Status), Status), " +
        "Remarks = IF(CheckIn IS NULL, VALUES(Remarks), Remarks), IsActive = 1";

    private readonly IDbConnectionFactory _db;

    public LeaveRequestRepository(IDbConnectionFactory db) => _db = db;

    public async Task<PagedResult<LeaveRequestDto>> GetPagedAsync(LeaveRequestQuery query, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        var param = new
        {
            query.EmployeeId,
            Status = string.IsNullOrWhiteSpace(query.Status) ? null : query.Status.Trim(),
            LeaveType = string.IsNullOrWhiteSpace(query.LeaveType) ? null : query.LeaveType.Trim(),
            FromDate = query.From,
            ToDate = query.To,
            Search = query.SearchPattern
        };
        return await conn.QueryPagedAsync<LeaveRequestDto>(
            Select + Where,
            "SELECT COUNT(*)" + From + Where,
            param, query, SortColumns, "l.CreatedDate DESC, l.LeaveRequestId DESC", ct);
    }

    public async Task<LeaveRequestDto?> GetByIdAsync(long id, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        return await conn.QueryFirstOrNullAsync<LeaveRequestDto>(Select + " WHERE l.LeaveRequestId = @Id AND l.IsActive = 1", new { Id = id }, ct);
    }

    public async Task<bool> HasOverlapAsync(long employeeId, DateTime from, DateTime to, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        var count = await conn.ScalarAsync<int>(
            "SELECT COUNT(1) FROM `LeaveRequest` WHERE IsActive = 1 AND EmployeeId = @EmployeeId AND Status IN ('Pending', 'Approved') " +
            "AND FromDate <= @ToDate AND ToDate >= @FromDate",
            new { EmployeeId = employeeId, FromDate = from, ToDate = to }, ct);
        return count > 0;
    }

    public async Task<long> CreateAsync(long employeeId, string leaveType, DateTime from, DateTime to, decimal days, string? reason, long userId, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        return await conn.InsertAsync(
            "INSERT INTO `LeaveRequest` (EmployeeId, LeaveType, FromDate, ToDate, Days, Reason, Status, CreatedBy) " +
            "VALUES (@EmployeeId, @LeaveType, @FromDate, @ToDate, @Days, @Reason, 'Pending', @UserId)",
            new { EmployeeId = employeeId, LeaveType = leaveType, FromDate = from, ToDate = to, Days = days, Reason = reason, UserId = userId }, ct);
    }

    public async Task<bool> ApproveAsync(long id, long userId, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        await using var tx = await conn.BeginTransactionAsync(ct);
        try
        {
            var affected = await conn.ExecuteAsync(new CommandDefinition(
                "UPDATE `LeaveRequest` SET Status = 'Approved', ApprovedBy = @UserId, ApprovedDate = NOW(), RejectionReason = NULL, ModifiedBy = @UserId " +
                "WHERE LeaveRequestId = @Id AND Status = 'Pending'",
                new { Id = id, UserId = userId }, tx, cancellationToken: ct));
            if (affected == 0)
            {
                await tx.RollbackAsync(CancellationToken.None);
                return false;
            }

            var leave = await conn.QueryFirstAsync<LeaveSpan>(new CommandDefinition(
                "SELECT EmployeeId, LeaveType, FromDate, ToDate FROM `LeaveRequest` WHERE LeaveRequestId = @Id",
                new { Id = id }, tx, cancellationToken: ct));

            // Unpaid leave counts as absence for salary purposes; paid leave stays 'Leave'.
            var status = leave.LeaveType == "Unpaid" ? "Absent" : "Leave";
            for (var day = leave.FromDate.Date; day <= leave.ToDate.Date; day = day.AddDays(1))
            {
                await conn.ExecuteAsync(new CommandDefinition(AttendanceFillSql,
                    new { leave.EmployeeId, Date = day, Status = status, UserId = userId }, tx, cancellationToken: ct));
            }

            await tx.CommitAsync(ct);
            return true;
        }
        catch
        {
            await tx.RollbackAsync(CancellationToken.None);
            throw;
        }
    }

    public async Task<bool> RejectAsync(long id, long userId, string? reason, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        var affected = await conn.ExecAsync(
            "UPDATE `LeaveRequest` SET Status = 'Rejected', ApprovedBy = @UserId, ApprovedDate = NOW(), RejectionReason = @Reason, ModifiedBy = @UserId " +
            "WHERE LeaveRequestId = @Id AND Status = 'Pending'",
            new { Id = id, UserId = userId, Reason = reason }, ct);
        return affected > 0;
    }

    private sealed class LeaveSpan
    {
        public long EmployeeId { get; set; }
        public string LeaveType { get; set; } = string.Empty;
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
    }
}
