using Dairy.Application.Common;
using Dairy.Application.DTOs.Maintenance;
using Dairy.Application.Interfaces;
using Dairy.Application.Interfaces.Maintenance;
using Dairy.Application.Requests.Maintenance;

namespace Dairy.Infrastructure.Data.Repositories.Maintenance;

public class MaintenanceRecordRepository : IMaintenanceRecordRepository
{
    private static readonly Dictionary<string, string> SortColumns = new(StringComparer.OrdinalIgnoreCase)
    {
        ["id"] = "r.MaintenanceRecordId",
        ["maintenanceDate"] = "r.MaintenanceDate",
        ["equipmentName"] = "e.Name",
        ["maintenanceType"] = "r.MaintenanceType",
        ["cost"] = "r.Cost",
        ["nextDueDate"] = "r.NextDueDate"
    };

    private const string Columns =
        "SELECT r.MaintenanceRecordId AS Id, r.EquipmentId, e.EquipmentCode, e.Name AS EquipmentName, r.MaintenanceScheduleId," +
        " s.TaskName, r.MaintenanceType, r.MaintenanceDate, r.Description, r.VendorName, r.Cost, r.NextDueDate, r.PerformedBy," +
        " r.DowntimeHours, r.InvoiceNo, r.Status, r.Remarks, r.CreatedBy, cu.DisplayName AS CreatedByName, r.CreatedDate";

    private const string From =
        " FROM `MaintenanceRecord` r" +
        " JOIN `Equipment` e ON e.EquipmentId = r.EquipmentId" +
        " LEFT JOIN `MaintenanceSchedule` s ON s.MaintenanceScheduleId = r.MaintenanceScheduleId" +
        " LEFT JOIN `User` cu ON cu.UserId = r.CreatedBy";

    private const string Where =
        " WHERE r.IsActive = 1" +
        " AND (@Search IS NULL OR r.Description LIKE @Search OR r.VendorName LIKE @Search OR e.Name LIKE @Search OR e.EquipmentCode LIKE @Search OR r.InvoiceNo LIKE @Search)" +
        " AND (@EquipmentId IS NULL OR r.EquipmentId = @EquipmentId)" +
        " AND (@ScheduleId IS NULL OR r.MaintenanceScheduleId = @ScheduleId)" +
        " AND (@Type IS NULL OR r.MaintenanceType = @Type)" +
        " AND (@From IS NULL OR r.MaintenanceDate >= DATE(@From))" +
        " AND (@To IS NULL OR r.MaintenanceDate <= DATE(@To))";

    private readonly IDbConnectionFactory _db;

    public MaintenanceRecordRepository(IDbConnectionFactory db) => _db = db;

    public async Task<PagedResult<MaintenanceRecordDto>> GetPagedAsync(MaintenanceRecordQuery query, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        var param = new
        {
            Search = query.SearchPattern,
            query.EquipmentId,
            query.ScheduleId,
            query.Type,
            query.From,
            query.To
        };
        return await conn.QueryPagedAsync<MaintenanceRecordDto>(
            Columns + From + Where,
            "SELECT COUNT(*)" + From + Where,
            param, query, SortColumns, "r.MaintenanceDate DESC, r.MaintenanceRecordId DESC", ct);
    }

    public async Task<MaintenanceRecordDto?> GetByIdAsync(long id, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        return await conn.QueryFirstOrNullAsync<MaintenanceRecordDto>(
            Columns + From + " WHERE r.MaintenanceRecordId = @Id AND r.IsActive = 1", new { Id = id }, ct);
    }

    public async Task<MaintenanceRecordedDto> RecordAsync(CreateMaintenanceRecordRequest request, long userId, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        var row = await conn.QuerySpFirstOrDefaultAsync<MaintenanceRecordedDto>("sp_RecordMaintenance", new
        {
            p_EquipmentId = request.EquipmentId,
            p_MaintenanceScheduleId = request.MaintenanceScheduleId,
            p_MaintenanceDate = request.MaintenanceDate,
            p_Description = request.Description,
            p_VendorName = request.VendorName,
            p_Cost = request.Cost,
            p_UserId = userId
        }, ct);
        return row ?? throw new InvalidOperationException("sp_RecordMaintenance did not return a row.");
    }

    public async Task SetExtrasAsync(long id, CreateMaintenanceRecordRequest r, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        await conn.ExecAsync(
            "UPDATE `MaintenanceRecord` SET MaintenanceType = COALESCE(@MaintenanceType, MaintenanceType)," +
            " PerformedBy = COALESCE(@PerformedBy, PerformedBy), DowntimeHours = COALESCE(@DowntimeHours, DowntimeHours)," +
            " InvoiceNo = COALESCE(@InvoiceNo, InvoiceNo), Remarks = COALESCE(@Remarks, Remarks)" +
            " WHERE MaintenanceRecordId = @Id",
            new { Id = id, r.MaintenanceType, r.PerformedBy, r.DowntimeHours, r.InvoiceNo, r.Remarks }, ct);
    }

    public async Task<List<MaintenanceDueDto>> GetDueAsync(int? days, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        return await conn.QuerySpAsync<MaintenanceDueDto>("sp_GetMaintenanceDue", new { p_Days = days }, ct);
    }
}
