using Dairy.Application.BusinessRules;
using Dairy.Application.Common;
using Dairy.Application.DTOs.Delivery;
using Dairy.Application.Interfaces;
using Dairy.Application.Interfaces.Delivery;
using Dairy.Application.Requests.Delivery;

namespace Dairy.Infrastructure.Data.Repositories.Delivery;

public class VehicleRepository : IVehicleRepository
{
    private static readonly Dictionary<string, string> SortColumns = new(StringComparer.OrdinalIgnoreCase)
    {
        ["id"] = "v.VehicleId",
        ["registrationNo"] = "v.RegistrationNo",
        ["vehicleType"] = "v.VehicleType",
        ["capacity"] = "v.Capacity",
        ["status"] = "v.Status",
        ["insuranceExpiry"] = "v.InsuranceExpiry",
        ["fitnessExpiry"] = "v.FitnessExpiry"
    };

    private const string Select =
        "SELECT v.VehicleId AS Id, v.RegistrationNo, v.VehicleType, v.Make, v.Model, v.Capacity, v.CapacityUnit, v.DriverEmployeeId, e.FullName AS DriverName," +
        " v.BranchId, v.FuelType, v.InsuranceExpiry, v.FitnessExpiry, v.Status, v.IsActive" +
        " FROM `Vehicle` v LEFT JOIN `Employee` e ON e.EmployeeId = v.DriverEmployeeId";

    private const string Where =
        " WHERE (@Search IS NULL OR v.RegistrationNo LIKE @Search OR v.Make LIKE @Search OR v.Model LIKE @Search)" +
        " AND (@Status IS NULL OR v.Status = @Status)" +
        " AND (@VehicleType IS NULL OR v.VehicleType = @VehicleType)" +
        " AND (@IsActive IS NULL OR v.IsActive = @IsActive)";

    private readonly IDbConnectionFactory _db;

    public VehicleRepository(IDbConnectionFactory db) => _db = db;

    public async Task<PagedResult<VehicleDto>> GetPagedAsync(VehicleQuery query, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        var param = new
        {
            Search = query.SearchPattern,
            Status = SalesValues.Clean(query.Status),
            VehicleType = SalesValues.Clean(query.VehicleType),
            query.IsActive
        };
        return await conn.QueryPagedAsync<VehicleDto>(Select + Where, "SELECT COUNT(*) FROM `Vehicle` v" + Where, param, query, SortColumns, "v.RegistrationNo ASC", ct);
    }

    public async Task<VehicleDto?> GetByIdAsync(long id, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        return await conn.QueryFirstOrNullAsync<VehicleDto>(Select + " WHERE v.VehicleId = @Id", new { Id = id }, ct);
    }

    public async Task<bool> RegistrationExistsAsync(string registrationNo, long? excludeId, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        return await conn.ScalarAsync<int>(
            "SELECT COUNT(1) FROM `Vehicle` WHERE RegistrationNo = @RegistrationNo AND (@ExcludeId IS NULL OR VehicleId <> @ExcludeId)",
            new { RegistrationNo = registrationNo, ExcludeId = excludeId }, ct) > 0;
    }

    public async Task<long> CreateAsync(string registrationNo, CreateVehicleRequest r, long userId, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        return await conn.InsertAsync(
            "INSERT INTO `Vehicle` (RegistrationNo, VehicleType, Make, Model, Capacity, CapacityUnit, DriverEmployeeId, BranchId, FuelType, InsuranceExpiry, FitnessExpiry, Status, CreatedBy)" +
            " VALUES (@RegistrationNo, @VehicleType, @Make, @Model, @Capacity, @CapacityUnit, @DriverEmployeeId, @BranchId, @FuelType, @InsuranceExpiry, @FitnessExpiry, @Status, @UserId)",
            new
            {
                RegistrationNo = registrationNo,
                r.VehicleType,
                Make = SalesValues.Clean(r.Make),
                Model = SalesValues.Clean(r.Model),
                r.Capacity,
                CapacityUnit = r.CapacityUnit.Trim(),
                r.DriverEmployeeId,
                r.BranchId,
                FuelType = SalesValues.Clean(r.FuelType),
                InsuranceExpiry = r.InsuranceExpiry?.Date,
                FitnessExpiry = r.FitnessExpiry?.Date,
                r.Status,
                UserId = userId
            }, ct);
    }

    public async Task UpdateAsync(long id, string registrationNo, UpdateVehicleRequest r, bool isActive, long userId, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        await conn.ExecAsync(
            "UPDATE `Vehicle` SET RegistrationNo = @RegistrationNo, VehicleType = @VehicleType, Make = @Make, Model = @Model, Capacity = @Capacity, CapacityUnit = @CapacityUnit," +
            " DriverEmployeeId = @DriverEmployeeId, BranchId = @BranchId, FuelType = @FuelType, InsuranceExpiry = @InsuranceExpiry, FitnessExpiry = @FitnessExpiry," +
            " Status = @Status, IsActive = @IsActive, ModifiedBy = @UserId WHERE VehicleId = @Id",
            new
            {
                Id = id,
                RegistrationNo = registrationNo,
                r.VehicleType,
                Make = SalesValues.Clean(r.Make),
                Model = SalesValues.Clean(r.Model),
                r.Capacity,
                CapacityUnit = r.CapacityUnit.Trim(),
                r.DriverEmployeeId,
                r.BranchId,
                FuelType = SalesValues.Clean(r.FuelType),
                InsuranceExpiry = r.InsuranceExpiry?.Date,
                FitnessExpiry = r.FitnessExpiry?.Date,
                Status = isActive ? r.Status : "Inactive",
                IsActive = isActive,
                UserId = userId
            }, ct);
    }

    public async Task<int> CountOpenSchedulesAsync(long id, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        return await conn.ScalarAsync<int>(
            "SELECT COUNT(1) FROM `DeliverySchedule` WHERE VehicleId = @Id AND IsActive = 1 AND Status <> 'Completed' AND ScheduleDate >= CURDATE()",
            new { Id = id }, ct);
    }

    public async Task SetInactiveAsync(long id, long userId, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        await conn.ExecAsync("UPDATE `Vehicle` SET IsActive = 0, Status = 'Inactive', ModifiedBy = @UserId WHERE VehicleId = @Id",
            new { Id = id, UserId = userId }, ct);
    }
}
