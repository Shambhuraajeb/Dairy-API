using Dairy.Application.DTOs.Lookups;
using Dairy.Application.Interfaces;
using Dairy.Application.Interfaces.Lookups;

namespace Dairy.Infrastructure.Data.Repositories.Lookups;

/// <summary>
/// Whitelist-driven lookups: the URL name maps to a fixed, trusted SQL statement. User input only ever
/// reaches the database as parameters (@Search, @ParentId, @Status, @OwnerId).
/// </summary>
public class LookupRepository : ILookupRepository
{
    private sealed record Def(
        string From,
        string Id,
        string Code,
        string Name,
        string Extra = "NULL",
        string? Parent = null,
        string? Status = null,
        string? Owner = null,
        string? Order = null);

    private static readonly Dictionary<string, Def> Defs = new(StringComparer.OrdinalIgnoreCase)
    {
        ["dairies"] = new("`Dairy` t", "t.DairyId", "t.Code", "t.Name", "t.City"),
        ["branches"] = new("`Branch` t", "t.BranchId", "t.Code", "t.Name", "t.City", Parent: "t.DairyId = @ParentId"),
        ["collection-centers"] = new("`CollectionCenter` t", "t.CollectionCenterId", "t.Code", "t.Name", "t.City",
            Parent: "t.BranchId = @ParentId"),
        ["shifts"] = new("`CollectionShift` t", "t.CollectionShiftId", "t.ShiftName", "t.ShiftName",
            "CONCAT(TIME_FORMAT(t.StartTime, '%H:%i'), '-', TIME_FORMAT(t.EndTime, '%H:%i'))",
            Parent: "(t.CollectionCenterId IS NULL OR t.CollectionCenterId = @ParentId)", Order: "t.StartTime"),
        ["units"] = new("`Unit` t", "t.UnitId", "t.Code", "t.Name", "t.Symbol"),
        ["breeds"] = new("`Breed` t", "t.BreedId", "t.Code", "t.Name", "t.AnimalType", Status: "t.AnimalType"),
        ["payment-modes"] = new("`PaymentMode` t", "t.PaymentModeId", "t.Code", "t.Name",
            "IF(t.RequiresReference = 1, 'ReferenceRequired', NULL)"),
        ["document-types"] = new("`DocumentType` t", "t.DocumentTypeId", "t.Code", "t.Name", "t.AppliesTo",
            Status: "t.AppliesTo"),
        ["farmers"] = new("`Farmer` t", "t.FarmerId", "t.FarmerCode", "t.FullName", "t.Mobile",
            Parent: "t.CollectionCenterId = @ParentId", Status: "t.Status", Owner: "t.FarmerId"),
        ["customers"] = new("`Customer` t", "t.CustomerId", "t.CustomerCode", "t.Name", "t.Mobile",
            Parent: "t.BranchId = @ParentId", Status: "t.Status", Owner: "t.CustomerId"),
        ["products"] = new("`Product` t JOIN `Unit` u ON u.UnitId = t.UnitId", "t.ProductId", "t.ProductCode", "t.Name",
            "u.Code", Parent: "t.ProductCategoryId = @ParentId"),
        ["items"] = new("`InventoryItem` t JOIN `Unit` u ON u.UnitId = t.UnitId", "t.InventoryItemId", "t.ItemCode", "t.Name",
            "u.Code", Parent: "t.ItemCategoryId = @ParentId"),
        ["routes"] = new("`Route` t", "t.RouteId", "t.Code", "t.Name", "NULL", Parent: "t.BranchId = @ParentId"),
        ["vehicles"] = new("`Vehicle` t", "t.VehicleId", "t.RegistrationNo",
            "CONCAT(t.RegistrationNo, ' (', t.VehicleType, ')')", "t.Status",
            Parent: "t.BranchId = @ParentId", Status: "t.Status"),
        ["employees"] = new("`Employee` t JOIN `Designation` d ON d.DesignationId = t.DesignationId", "t.EmployeeId",
            "t.EmployeeCode", "t.FullName", "d.Name", Parent: "t.DesignationId = @ParentId", Status: "t.Status"),
        ["designations"] = new("`Designation` t", "t.DesignationId", "t.Name", "t.Name", "t.Department"),
        ["roles"] = new("`Role` t", "t.RoleId", "t.Name", "t.Name", "t.Description"),
        ["expense-categories"] = new("`ExpenseCategory` t", "t.ExpenseCategoryId", "t.Code", "t.Name"),
        ["item-categories"] = new("`ItemCategory` t", "t.ItemCategoryId", "t.Code", "t.Name"),
        ["suppliers"] = new("`Supplier` t", "t.SupplierId", "t.SupplierCode", "t.Name", "t.Mobile"),
        ["product-categories"] = new("`ProductCategory` t", "t.ProductCategoryId", "t.Code", "t.Name"),
        ["subscription-plans"] = new("`SubscriptionPlan` t", "t.SubscriptionPlanId", "t.PlanCode", "t.Name", "t.Frequency")
    };

    private static readonly Dictionary<string, string> SqlByName = Defs.ToDictionary(
        kv => kv.Key,
        kv => BuildSql(kv.Value),
        StringComparer.OrdinalIgnoreCase);

    private readonly IDbConnectionFactory _db;

    public LookupRepository(IDbConnectionFactory db) => _db = db;

    public async Task<List<LookupItemDto>?> GetAsync(string name, string? searchPattern, long? parentId, string? status, long? ownerId, CancellationToken ct = default)
    {
        if (!SqlByName.TryGetValue(name, out var sql))
            return null;

        await using var conn = await _db.OpenAsync(ct);
        return await conn.QueryListAsync<LookupItemDto>(
            sql,
            new { Search = searchPattern, ParentId = parentId, Status = status, OwnerId = ownerId },
            ct);
    }

    private static string BuildSql(Def d)
    {
        var sql = $"SELECT {d.Id} AS Id, {d.Code} AS Code, {d.Name} AS Name, {d.Extra} AS Extra FROM {d.From} " +
                  $"WHERE t.IsActive = 1 AND (@Search IS NULL OR {d.Code} LIKE @Search OR {d.Name} LIKE @Search)";

        if (d.Parent is not null) sql += $" AND (@ParentId IS NULL OR {d.Parent})";
        if (d.Status is not null) sql += $" AND (@Status IS NULL OR {d.Status} = @Status)";
        if (d.Owner is not null) sql += $" AND (@OwnerId IS NULL OR {d.Owner} = @OwnerId)";

        sql += $" ORDER BY {d.Order ?? d.Name} LIMIT 1000";
        return sql;
    }
}
