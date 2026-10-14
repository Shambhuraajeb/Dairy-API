using Dairy.Application.Common;
using Dairy.Application.DTOs.Maintenance;
using Dairy.Application.Interfaces;
using Dairy.Application.Interfaces.Maintenance;
using Dairy.Application.Requests.Maintenance;

namespace Dairy.Infrastructure.Data.Repositories.Maintenance;

public class MaintenanceScheduleRepository : IMaintenanceScheduleRepository
{
    private static readonly Dictionary<string, string> SortColumns = new(StringComparer.OrdinalIgnoreCase)
    {
        ["id"] = "s.MaintenanceScheduleId",
        ["equipmentName"] = "e.Name",
        ["taskName"] = "s.TaskName",
        ["nextDueDate"] = "s.NextDueDate",
        ["lastServiceDate"] = "s.LastServiceDate",
        ["frequencyDays"] = "s.FrequencyDays",
        ["status"] = "s.Status"
    };

    private const string Columns =
        "SELECT s.MaintenanceScheduleId AS Id, s.EquipmentId, e.EquipmentCode, e.Name AS EquipmentName, s.TaskName, s.Description," +
        " s.FrequencyDays, s.LastServiceDate, s.NextDueDate, s.AlertBeforeDays, s.AssignedEmployeeId, emp.FullName AS AssignedEmployeeName," +
        " s.Status, s.IsActive, DATEDIFF(s.NextDueDate, CURDATE()) AS DaysToDue," +
        " IF(s.NextDueDate < CURDATE(), 1, 0) AS IsOverdue," +
        " IF(s.Status = 'Active' AND s.NextDueDate <= DATE_ADD(CURDATE(), INTERVAL s.AlertBeforeDays DAY), 1, 0) AS IsDue";

    private const string From =
        " FROM `MaintenanceSchedule` s" +
        " JOIN `Equipment` e ON e.EquipmentId = s.EquipmentId" +
        " LEFT JOIN `Employee` emp ON emp.EmployeeId = s.AssignedEmployeeId";

    private const string Where =
        " WHERE s.IsActive = 1" +
        " AND (@Search IS NULL OR s.TaskName LIKE @Search OR e.Name LIKE @Search OR e.EquipmentCode LIKE @Search)" +
        " AND (@EquipmentId IS NULL OR s.EquipmentId = @EquipmentId)" +
        " AND (@Status IS NULL OR s.Status = @Status)" +
        " AND (@AssignedEmployeeId IS NULL OR s.AssignedEmployeeId = @AssignedEmployeeId)" +
        " AND (@DueOnly IS NULL OR @DueOnly = 0 OR (s.Status = 'Active' AND e.IsActive = 1 AND e.Status <> 'Retired'" +
        "      AND s.NextDueDate <= DATE_ADD(CURDATE(), INTERVAL s.AlertBeforeDays DAY)))";

    private readonly IDbConnectionFactory _db;

    public MaintenanceScheduleRepository(IDbConnectionFactory db) => _db = db;

    public async Task<PagedResult<MaintenanceScheduleDto>> GetPagedAsync(MaintenanceScheduleQuery query, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        var param = new
        {
            Search = query.SearchPattern,
            query.EquipmentId,
            query.Status,
            query.AssignedEmployeeId,
            query.DueOnly
        };
        return await conn.QueryPagedAsync<MaintenanceScheduleDto>(
            Columns + From + Where,
            "SELECT COUNT(*)" + From + Where,
            param, query, SortColumns, "s.NextDueDate ASC, s.MaintenanceScheduleId ASC", ct);
    }

    public async Task<MaintenanceScheduleDto?> GetByIdAsync(long id, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        return await conn.QueryFirstOrNullAsync<MaintenanceScheduleDto>(
            Columns + From + " WHERE s.MaintenanceScheduleId = @Id AND s.IsActive = 1", new { Id = id }, ct);
    }

    public async Task<bool> EquipmentExistsAsync(long id, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        return await conn.ScalarAsync<int>("SELECT COUNT(1) FROM `Equipment` WHERE EquipmentId = @Id AND IsActive = 1", new { Id = id }, ct) > 0;
    }

    public async Task<bool> EmployeeExistsAsync(long id, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        return await conn.ScalarAsync<int>("SELECT COUNT(1) FROM `Employee` WHERE EmployeeId = @Id", new { Id = id }, ct) > 0;
    }

    public async Task<long> CreateAsync(CreateMaintenanceScheduleRequest r, DateTime nextDueDate, long userId, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        return await conn.InsertAsync(
            "INSERT INTO `MaintenanceSchedule` (EquipmentId, TaskName, Description, FrequencyDays, LastServiceDate, NextDueDate," +
            " AlertBeforeDays, AssignedEmployeeId, Status, IsActive, CreatedBy)" +
            " VALUES (@EquipmentId, @TaskName, @Description, @FrequencyDays, @LastServiceDate, @NextDueDate," +
            " @AlertBeforeDays, @AssignedEmployeeId, 'Active', 1, @UserId)",
            new
            {
                r.EquipmentId,
                r.TaskName,
                r.Description,
                r.FrequencyDays,
                r.LastServiceDate,
                NextDueDate = nextDueDate,
                r.AlertBeforeDays,
                r.AssignedEmployeeId,
                UserId = userId
            }, ct);
    }

    public async Task UpdateAsync(long id, UpdateMaintenanceScheduleRequest r, DateTime nextDueDate, string status, bool isActive, long userId, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        await conn.ExecAsync(
            "UPDATE `MaintenanceSchedule` SET TaskName = @TaskName, Description = @Description, FrequencyDays = @FrequencyDays," +
            " LastServiceDate = @LastServiceDate, NextDueDate = @NextDueDate, AlertBeforeDays = @AlertBeforeDays," +
            " AssignedEmployeeId = @AssignedEmployeeId, Status = @Status, IsActive = @IsActive, ModifiedBy = @UserId" +
            " WHERE MaintenanceScheduleId = @Id",
            new
            {
                Id = id,
                r.TaskName,
                r.Description,
                r.FrequencyDays,
                r.LastServiceDate,
                NextDueDate = nextDueDate,
                r.AlertBeforeDays,
                r.AssignedEmployeeId,
                Status = status,
                IsActive = isActive,
                UserId = userId
            }, ct);
    }
}
