using Dairy.Application.Common;
using Dairy.Application.DTOs.Maintenance;
using Dairy.Application.Interfaces;
using Dairy.Application.Interfaces.Maintenance;
using Dairy.Application.Requests.Maintenance;

namespace Dairy.Infrastructure.Data.Repositories.Maintenance;

public class EquipmentRepository : IEquipmentRepository
{
    private static readonly Dictionary<string, string> SortColumns = new(StringComparer.OrdinalIgnoreCase)
    {
        ["id"] = "e.EquipmentId",
        ["equipmentCode"] = "e.EquipmentCode",
        ["name"] = "e.Name",
        ["category"] = "e.Category",
        ["status"] = "e.Status",
        ["purchaseDate"] = "e.PurchaseDate",
        ["warrantyExpiry"] = "e.WarrantyExpiry",
        ["nextDueDate"] = "NextDueDate",
        ["isActive"] = "e.IsActive"
    };

    private const string Columns =
        "SELECT e.EquipmentId AS Id, e.EquipmentCode, e.Name, e.Category, e.Make, e.Model, e.SerialNumber, e.PurchaseDate," +
        " e.PurchaseCost, e.Location, e.BranchId, b.Name AS BranchName, e.CollectionCenterId, cc.Name AS CollectionCenterName," +
        " e.WarrantyExpiry, e.VendorName, e.Status, e.Remarks, e.IsActive, e.CreatedDate," +
        " (SELECT MIN(s.NextDueDate) FROM `MaintenanceSchedule` s" +
        "   WHERE s.EquipmentId = e.EquipmentId AND s.IsActive = 1 AND s.Status = 'Active') AS NextDueDate," +
        " (SELECT MAX(r.MaintenanceDate) FROM `MaintenanceRecord` r" +
        "   WHERE r.EquipmentId = e.EquipmentId AND r.IsActive = 1) AS LastMaintenanceDate";

    private const string From =
        " FROM `Equipment` e" +
        " LEFT JOIN `Branch` b ON b.BranchId = e.BranchId" +
        " LEFT JOIN `CollectionCenter` cc ON cc.CollectionCenterId = e.CollectionCenterId";

    private const string Where =
        " WHERE (@Search IS NULL OR e.EquipmentCode LIKE @Search OR e.Name LIKE @Search OR e.Category LIKE @Search" +
        "        OR e.Make LIKE @Search OR e.Model LIKE @Search OR e.SerialNumber LIKE @Search OR e.Location LIKE @Search)" +
        " AND (@Status IS NULL OR e.Status = @Status)" +
        " AND (@Category IS NULL OR e.Category = @Category)" +
        " AND (@BranchId IS NULL OR e.BranchId = @BranchId)" +
        " AND (@CollectionCenterId IS NULL OR e.CollectionCenterId = @CollectionCenterId)" +
        " AND (@IsActive IS NULL OR e.IsActive = @IsActive)";

    private readonly IDbConnectionFactory _db;

    public EquipmentRepository(IDbConnectionFactory db) => _db = db;

    public async Task<PagedResult<EquipmentDto>> GetPagedAsync(EquipmentQuery query, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        var param = new
        {
            Search = query.SearchPattern,
            query.Status,
            query.Category,
            query.BranchId,
            query.CollectionCenterId,
            query.IsActive
        };
        return await conn.QueryPagedAsync<EquipmentDto>(
            Columns + From + Where,
            "SELECT COUNT(*)" + From + Where,
            param, query, SortColumns, "e.Name ASC", ct);
    }

    public async Task<EquipmentDto?> GetByIdAsync(long id, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        return await conn.QueryFirstOrNullAsync<EquipmentDto>(Columns + From + " WHERE e.EquipmentId = @Id", new { Id = id }, ct);
    }

    public async Task<bool> CodeExistsAsync(string code, long? excludeId, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        var count = await conn.ScalarAsync<int>(
            "SELECT COUNT(1) FROM `Equipment` WHERE EquipmentCode = @Code AND (@ExcludeId IS NULL OR EquipmentId <> @ExcludeId)",
            new { Code = code, ExcludeId = excludeId }, ct);
        return count > 0;
    }

    public async Task<bool> BranchExistsAsync(long id, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        return await conn.ScalarAsync<int>("SELECT COUNT(1) FROM `Branch` WHERE BranchId = @Id", new { Id = id }, ct) > 0;
    }

    public async Task<bool> CollectionCenterExistsAsync(long id, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        return await conn.ScalarAsync<int>("SELECT COUNT(1) FROM `CollectionCenter` WHERE CollectionCenterId = @Id", new { Id = id }, ct) > 0;
    }

    public async Task<long> CreateAsync(CreateEquipmentRequest r, long userId, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        return await conn.InsertAsync(
            "INSERT INTO `Equipment` (EquipmentCode, Name, Category, Make, Model, SerialNumber, PurchaseDate, PurchaseCost, Location," +
            " BranchId, CollectionCenterId, WarrantyExpiry, VendorName, Status, Remarks, IsActive, CreatedBy)" +
            " VALUES (@EquipmentCode, @Name, @Category, @Make, @Model, @SerialNumber, @PurchaseDate, @PurchaseCost, @Location," +
            " @BranchId, @CollectionCenterId, @WarrantyExpiry, @VendorName, 'Operational', @Remarks, 1, @UserId)",
            new
            {
                r.EquipmentCode,
                r.Name,
                r.Category,
                r.Make,
                r.Model,
                r.SerialNumber,
                r.PurchaseDate,
                r.PurchaseCost,
                r.Location,
                r.BranchId,
                r.CollectionCenterId,
                r.WarrantyExpiry,
                r.VendorName,
                r.Remarks,
                UserId = userId
            }, ct);
    }

    public async Task UpdateAsync(long id, UpdateEquipmentRequest r, string status, bool isActive, long userId, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        await conn.ExecAsync(
            "UPDATE `Equipment` SET EquipmentCode = @EquipmentCode, Name = @Name, Category = @Category, Make = @Make, Model = @Model," +
            " SerialNumber = @SerialNumber, PurchaseDate = @PurchaseDate, PurchaseCost = @PurchaseCost, Location = @Location," +
            " BranchId = @BranchId, CollectionCenterId = @CollectionCenterId, WarrantyExpiry = @WarrantyExpiry, VendorName = @VendorName," +
            " Status = @Status, Remarks = @Remarks, IsActive = @IsActive, ModifiedBy = @UserId WHERE EquipmentId = @Id",
            new
            {
                Id = id,
                r.EquipmentCode,
                r.Name,
                r.Category,
                r.Make,
                r.Model,
                r.SerialNumber,
                r.PurchaseDate,
                r.PurchaseCost,
                r.Location,
                r.BranchId,
                r.CollectionCenterId,
                r.WarrantyExpiry,
                r.VendorName,
                Status = status,
                r.Remarks,
                IsActive = isActive,
                UserId = userId
            }, ct);
    }
}
