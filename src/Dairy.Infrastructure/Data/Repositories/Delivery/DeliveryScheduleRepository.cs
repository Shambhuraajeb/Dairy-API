using Dairy.Application.BusinessRules;
using Dairy.Application.Common;
using Dairy.Application.DTOs.Delivery;
using Dairy.Application.Interfaces;
using Dairy.Application.Interfaces.Delivery;
using Dairy.Application.Requests.Delivery;
using Dapper;

namespace Dairy.Infrastructure.Data.Repositories.Delivery;

public class DeliveryScheduleRepository : IDeliveryScheduleRepository
{
    private static readonly Dictionary<string, string> SortColumns = new(StringComparer.OrdinalIgnoreCase)
    {
        ["id"] = "ds.DeliveryScheduleId",
        ["scheduleDate"] = "ds.ScheduleDate",
        ["slot"] = "ds.Slot",
        ["routeName"] = "r.Name",
        ["status"] = "ds.Status",
        ["driverName"] = "e.FullName"
    };

    private const string FromJoins =
        " FROM `DeliverySchedule` ds" +
        " JOIN `Route` r ON r.RouteId = ds.RouteId" +
        " LEFT JOIN `Vehicle` v ON v.VehicleId = ds.VehicleId" +
        " LEFT JOIN `Employee` e ON e.EmployeeId = ds.DriverEmployeeId";

    private const string Select =
        "SELECT ds.DeliveryScheduleId AS Id, ds.RouteId, r.Code AS RouteCode, r.Name AS RouteName, ds.VehicleId, v.RegistrationNo AS VehicleNo," +
        " ds.DriverEmployeeId, e.FullName AS DriverName, ds.ScheduleDate, ds.Slot, ds.Status, TIME_FORMAT(ds.PlannedStartTime, '%H:%i') AS PlannedStartTime," +
        " ds.DispatchedDate, ds.CompletedDate, ds.Remarks," +
        " (SELECT COUNT(*) FROM `Delivery` d WHERE d.DeliveryScheduleId = ds.DeliveryScheduleId AND d.IsActive = 1 AND d.Status <> 'Cancelled') AS DeliveryCount," +
        " (SELECT COUNT(*) FROM `Delivery` d WHERE d.DeliveryScheduleId = ds.DeliveryScheduleId AND d.IsActive = 1 AND d.Status = 'Delivered') AS DeliveredCount," +
        " ds.IsActive" + FromJoins;

    private const string Where =
        " WHERE ds.IsActive = 1" +
        " AND (@Search IS NULL OR r.Name LIKE @Search OR r.Code LIKE @Search OR v.RegistrationNo LIKE @Search OR e.FullName LIKE @Search)" +
        " AND (@Date IS NULL OR ds.ScheduleDate = @Date)" +
        " AND (@From IS NULL OR ds.ScheduleDate >= @From)" +
        " AND (@To IS NULL OR ds.ScheduleDate <= @To)" +
        " AND (@RouteId IS NULL OR ds.RouteId = @RouteId)" +
        " AND (@Status IS NULL OR ds.Status = @Status)" +
        " AND (@Slot IS NULL OR ds.Slot = @Slot)" +
        " AND (@DriverEmployeeId IS NULL OR ds.DriverEmployeeId = @DriverEmployeeId)";

    private readonly IDbConnectionFactory _db;

    public DeliveryScheduleRepository(IDbConnectionFactory db) => _db = db;

    public async Task<PagedResult<DeliveryScheduleDto>> GetPagedAsync(DeliveryScheduleQuery query, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        var param = new
        {
            Search = query.SearchPattern,
            Date = query.Date?.Date,
            From = query.From?.Date,
            To = query.To?.Date,
            query.RouteId,
            Status = SalesValues.Clean(query.Status),
            Slot = SalesValues.Clean(query.Slot),
            query.DriverEmployeeId
        };
        return await conn.QueryPagedAsync<DeliveryScheduleDto>(Select + Where, "SELECT COUNT(*)" + FromJoins + Where, param, query, SortColumns,
            "ds.ScheduleDate DESC, ds.Slot ASC, ds.DeliveryScheduleId DESC", ct);
    }

    public async Task<DeliveryScheduleDto?> GetByIdAsync(long id, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        return await conn.QueryFirstOrNullAsync<DeliveryScheduleDto>(Select + " WHERE ds.DeliveryScheduleId = @Id AND ds.IsActive = 1", new { Id = id }, ct);
    }

    public async Task<bool> RouteActiveAsync(long routeId, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        return await conn.ScalarAsync<int>("SELECT COUNT(1) FROM `Route` WHERE RouteId = @Id AND IsActive = 1", new { Id = routeId }, ct) > 0;
    }

    public async Task<bool> SlotTakenAsync(long routeId, DateTime date, string slot, long? excludeId, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        return await conn.ScalarAsync<int>(
            "SELECT COUNT(1) FROM `DeliverySchedule` WHERE RouteId = @RouteId AND ScheduleDate = @Date AND Slot = @Slot AND IsActive = 1" +
            " AND (@ExcludeId IS NULL OR DeliveryScheduleId <> @ExcludeId)",
            new { RouteId = routeId, Date = date.Date, Slot = slot, ExcludeId = excludeId }, ct) > 0;
    }

    public async Task<long> CreateAsync(CreateDeliveryScheduleRequest r, long userId, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        var p = new
        {
            r.RouteId,
            r.VehicleId,
            r.DriverEmployeeId,
            ScheduleDate = r.ScheduleDate.Date,
            r.Slot,
            PlannedStartTime = SalesValues.Clean(r.PlannedStartTime),
            Remarks = SalesValues.Clean(r.Remarks),
            UserId = userId
        };

        // the unique key (RouteId, ScheduleDate, Slot) also covers deactivated schedules: reuse such a row instead of colliding with it
        var reusedId = await conn.ScalarAsync<long?>(
            "SELECT DeliveryScheduleId FROM `DeliverySchedule` WHERE RouteId = @RouteId AND ScheduleDate = @ScheduleDate AND Slot = @Slot AND IsActive = 0 LIMIT 1",
            p, ct);
        if (reusedId.HasValue)
        {
            await conn.ExecAsync(
                "UPDATE `DeliverySchedule` ds JOIN `Route` r ON r.RouteId = ds.RouteId SET ds.IsActive = 1, ds.Status = 'Planned'," +
                " ds.VehicleId = COALESCE(@VehicleId, r.DefaultVehicleId), ds.DriverEmployeeId = COALESCE(@DriverEmployeeId, r.DefaultDriverEmployeeId)," +
                " ds.PlannedStartTime = NULLIF(@PlannedStartTime, ''), ds.Remarks = @Remarks, ds.DispatchedDate = NULL, ds.CompletedDate = NULL, ds.ModifiedBy = @UserId" +
                " WHERE ds.DeliveryScheduleId = @ReusedId",
                new { ReusedId = reusedId.Value, p.VehicleId, p.DriverEmployeeId, p.PlannedStartTime, p.Remarks, p.UserId }, ct);
            return reusedId.Value;
        }

        return await conn.InsertAsync(
            "INSERT INTO `DeliverySchedule` (RouteId, VehicleId, DriverEmployeeId, ScheduleDate, Slot, Status, PlannedStartTime, Remarks, CreatedBy)" +
            " SELECT r.RouteId, COALESCE(@VehicleId, r.DefaultVehicleId), COALESCE(@DriverEmployeeId, r.DefaultDriverEmployeeId), @ScheduleDate, @Slot, 'Planned'," +
            " NULLIF(@PlannedStartTime, ''), @Remarks, @UserId FROM `Route` r WHERE r.RouteId = @RouteId",
            p, ct);
    }

    public async Task UpdateAsync(long id, UpdateDeliveryScheduleRequest r, bool driverChanged, long userId, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        await using var tx = await conn.BeginTransactionAsync(ct);

        await conn.ExecuteAsync(new CommandDefinition(
            "UPDATE `DeliverySchedule` SET VehicleId = @VehicleId, DriverEmployeeId = @DriverEmployeeId," +
            " ScheduleDate = IF(Status = 'Planned', @ScheduleDate, ScheduleDate), Slot = IF(Status = 'Planned', @Slot, Slot)," +
            " PlannedStartTime = NULLIF(@PlannedStartTime, ''), Remarks = @Remarks, ModifiedBy = @UserId WHERE DeliveryScheduleId = @Id",
            new
            {
                Id = id,
                r.VehicleId,
                r.DriverEmployeeId,
                ScheduleDate = r.ScheduleDate.Date,
                r.Slot,
                PlannedStartTime = SalesValues.Clean(r.PlannedStartTime),
                Remarks = SalesValues.Clean(r.Remarks),
                UserId = userId
            }, tx, cancellationToken: ct));

        if (driverChanged)
        {
            // pending stops follow the new driver (and that driver's login becomes the authorized confirmer)
            await conn.ExecuteAsync(new CommandDefinition(
                "UPDATE `Delivery` SET DriverEmployeeId = @DriverEmployeeId," +
                " AssignedUserId = (SELECT u.UserId FROM `User` u WHERE u.EmployeeId = @DriverEmployeeId AND u.IsActive = 1 ORDER BY u.UserId LIMIT 1)," +
                " ModifiedBy = @UserId WHERE DeliveryScheduleId = @Id AND IsActive = 1 AND Status = 'Pending'",
                new { Id = id, r.DriverEmployeeId, UserId = userId }, tx, cancellationToken: ct));
        }

        await tx.CommitAsync(ct);
    }

    public async Task<int> CountLiveDeliveriesAsync(long id, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        return await conn.ScalarAsync<int>(
            "SELECT COUNT(1) FROM `Delivery` WHERE DeliveryScheduleId = @Id AND IsActive = 1 AND Status <> 'Cancelled'", new { Id = id }, ct);
    }

    public async Task DeactivateAsync(long id, long userId, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        await conn.ExecAsync("UPDATE `DeliverySchedule` SET IsActive = 0, ModifiedBy = @UserId WHERE DeliveryScheduleId = @Id", new { Id = id, UserId = userId }, ct);
    }
}
