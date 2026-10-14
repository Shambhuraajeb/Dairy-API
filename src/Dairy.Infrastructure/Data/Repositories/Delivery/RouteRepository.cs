using Dairy.Application.BusinessRules;
using Dairy.Application.Common;
using Dairy.Application.DTOs.Delivery;
using Dairy.Application.Interfaces;
using Dairy.Application.Interfaces.Delivery;
using Dairy.Application.Requests.Delivery;

namespace Dairy.Infrastructure.Data.Repositories.Delivery;

public class RouteRepository : IRouteRepository
{
    private static readonly Dictionary<string, string> SortColumns = new(StringComparer.OrdinalIgnoreCase)
    {
        ["id"] = "r.RouteId",
        ["code"] = "r.Code",
        ["name"] = "r.Name",
        ["distanceKm"] = "r.DistanceKm",
        ["estimatedMinutes"] = "r.EstimatedMinutes",
        ["isActive"] = "r.IsActive"
    };

    private const string Select =
        "SELECT r.RouteId AS Id, r.Code, r.Name, r.BranchId, b.Name AS BranchName, r.Description, r.StartLocation, r.EndLocation, r.DistanceKm, r.EstimatedMinutes," +
        " r.DefaultVehicleId, v.RegistrationNo AS DefaultVehicleNo, r.DefaultDriverEmployeeId, e.FullName AS DefaultDriverName, r.IsActive," +
        " (SELECT COUNT(*) FROM `CustomerAddress` a WHERE a.RouteId = r.RouteId AND a.IsActive = 1) AS AddressCount" +
        " FROM `Route` r LEFT JOIN `Branch` b ON b.BranchId = r.BranchId LEFT JOIN `Vehicle` v ON v.VehicleId = r.DefaultVehicleId" +
        " LEFT JOIN `Employee` e ON e.EmployeeId = r.DefaultDriverEmployeeId";

    private const string Where =
        " WHERE (@Search IS NULL OR r.Code LIKE @Search OR r.Name LIKE @Search OR r.StartLocation LIKE @Search OR r.EndLocation LIKE @Search)" +
        " AND (@BranchId IS NULL OR r.BranchId = @BranchId)" +
        " AND (@IsActive IS NULL OR r.IsActive = @IsActive)";

    private readonly IDbConnectionFactory _db;

    public RouteRepository(IDbConnectionFactory db) => _db = db;

    public async Task<PagedResult<RouteDto>> GetPagedAsync(RouteQuery query, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        var param = new { Search = query.SearchPattern, query.BranchId, query.IsActive };
        return await conn.QueryPagedAsync<RouteDto>(Select + Where, "SELECT COUNT(*) FROM `Route` r" + Where, param, query, SortColumns, "r.Name ASC", ct);
    }

    public async Task<RouteDto?> GetByIdAsync(long id, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        return await conn.QueryFirstOrNullAsync<RouteDto>(Select + " WHERE r.RouteId = @Id", new { Id = id }, ct);
    }

    public async Task<bool> CodeExistsAsync(string code, long? excludeId, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        return await conn.ScalarAsync<int>(
            "SELECT COUNT(1) FROM `Route` WHERE Code = @Code AND (@ExcludeId IS NULL OR RouteId <> @ExcludeId)",
            new { Code = code, ExcludeId = excludeId }, ct) > 0;
    }

    public async Task<CreatedResult> CreateAsync(string? code, CreateRouteRequest r, long userId, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        var (id, finalCode) = await AutoCode.InsertAsync(conn, "Route", "Code", "RT", 3, code, c => conn.InsertAsync(
            "INSERT INTO `Route` (Code, Name, BranchId, Description, StartLocation, EndLocation, DistanceKm, EstimatedMinutes, DefaultVehicleId, DefaultDriverEmployeeId, CreatedBy)" +
            " VALUES (@Code, @Name, @BranchId, @Description, @StartLocation, @EndLocation, @DistanceKm, @EstimatedMinutes, @DefaultVehicleId, @DefaultDriverEmployeeId, @UserId)",
            new
            {
                Code = c,
                Name = r.Name.Trim(),
                r.BranchId,
                Description = SalesValues.Clean(r.Description),
                StartLocation = SalesValues.Clean(r.StartLocation),
                EndLocation = SalesValues.Clean(r.EndLocation),
                r.DistanceKm,
                r.EstimatedMinutes,
                r.DefaultVehicleId,
                r.DefaultDriverEmployeeId,
                UserId = userId
            }, ct), ct);
        return new CreatedResult { Id = id, Number = finalCode };
    }

    public async Task UpdateAsync(long id, UpdateRouteRequest r, bool isActive, long userId, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        await conn.ExecAsync(
            "UPDATE `Route` SET Code = @Code, Name = @Name, BranchId = @BranchId, Description = @Description, StartLocation = @StartLocation, EndLocation = @EndLocation," +
            " DistanceKm = @DistanceKm, EstimatedMinutes = @EstimatedMinutes, DefaultVehicleId = @DefaultVehicleId, DefaultDriverEmployeeId = @DefaultDriverEmployeeId," +
            " IsActive = @IsActive, ModifiedBy = @UserId WHERE RouteId = @Id",
            new
            {
                Id = id,
                Code = r.Code.Trim().ToUpperInvariant(),
                Name = r.Name.Trim(),
                r.BranchId,
                Description = SalesValues.Clean(r.Description),
                StartLocation = SalesValues.Clean(r.StartLocation),
                EndLocation = SalesValues.Clean(r.EndLocation),
                r.DistanceKm,
                r.EstimatedMinutes,
                r.DefaultVehicleId,
                r.DefaultDriverEmployeeId,
                IsActive = isActive,
                UserId = userId
            }, ct);
    }

    public async Task<int> CountOpenSchedulesAsync(long id, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        return await conn.ScalarAsync<int>(
            "SELECT COUNT(1) FROM `DeliverySchedule` WHERE RouteId = @Id AND IsActive = 1 AND Status <> 'Completed' AND ScheduleDate >= CURDATE()",
            new { Id = id }, ct);
    }

    public async Task SetActiveAsync(long id, bool isActive, long userId, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        await conn.ExecAsync("UPDATE `Route` SET IsActive = @IsActive, ModifiedBy = @UserId WHERE RouteId = @Id",
            new { Id = id, IsActive = isActive, UserId = userId }, ct);
    }
}
