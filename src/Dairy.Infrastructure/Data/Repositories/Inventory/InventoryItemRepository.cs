using Dairy.Application.Common;
using Dairy.Application.DTOs.Inventory;
using Dairy.Application.Interfaces;
using Dairy.Application.Interfaces.Inventory;
using Dairy.Application.Requests.Inventory;

namespace Dairy.Infrastructure.Data.Repositories.Inventory;

public class InventoryItemRepository : IInventoryItemRepository
{
    private static readonly Dictionary<string, string> SortColumns = new(StringComparer.OrdinalIgnoreCase)
    {
        ["id"] = "i.InventoryItemId",
        ["itemCode"] = "i.ItemCode",
        ["name"] = "i.Name",
        ["categoryName"] = "c.Name",
        ["currentStock"] = "i.CurrentStock",
        ["reorderLevel"] = "i.ReorderLevel",
        ["unitCost"] = "i.UnitCost",
        ["stockValue"] = "(i.CurrentStock * i.UnitCost)",
        ["isActive"] = "i.IsActive"
    };

    private const string From =
        " FROM `InventoryItem` i" +
        " JOIN `ItemCategory` c ON c.ItemCategoryId = i.ItemCategoryId" +
        " JOIN `Unit` u ON u.UnitId = i.UnitId" +
        " LEFT JOIN `Supplier` s ON s.SupplierId = i.PreferredSupplierId";

    private const string Columns =
        "SELECT i.InventoryItemId AS Id, i.ItemCode, i.Name, i.ItemCategoryId, c.Name AS CategoryName," +
        " i.UnitId, u.Name AS UnitName, i.Description, i.ReorderLevel, i.MaxStockLevel, i.CurrentStock, i.UnitCost," +
        " ROUND(i.CurrentStock * i.UnitCost, 2) AS StockValue, i.TaxPercent, i.IsPerishable, i.HasExpiry, i.ShelfLifeDays," +
        " i.PreferredSupplierId, s.Name AS PreferredSupplierName, i.StorageLocation, i.Barcode, i.IsActive," +
        " CASE WHEN i.CurrentStock <= 0 THEN 'OutOfStock'" +
        "      WHEN i.ReorderLevel > 0 AND i.CurrentStock <= i.ReorderLevel THEN 'Low'" +
        "      ELSE 'OK' END AS StockStatus";

    private const string Where =
        " WHERE (@Search IS NULL OR i.ItemCode LIKE @Search OR i.Name LIKE @Search OR i.Barcode LIKE @Search OR c.Name LIKE @Search)" +
        " AND (@CategoryId IS NULL OR i.ItemCategoryId = @CategoryId)" +
        " AND (@IsActive IS NULL OR i.IsActive = @IsActive)" +
        " AND (@LowStockOnly IS NULL OR @LowStockOnly = 0 OR (i.ReorderLevel > 0 AND i.CurrentStock <= i.ReorderLevel))";

    private readonly IDbConnectionFactory _db;

    public InventoryItemRepository(IDbConnectionFactory db) => _db = db;

    public async Task<PagedResult<InventoryItemDto>> GetPagedAsync(InventoryItemQuery query, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        var param = new
        {
            Search = query.SearchPattern,
            query.CategoryId,
            query.IsActive,
            query.LowStockOnly
        };
        return await conn.QueryPagedAsync<InventoryItemDto>(
            Columns + From + Where,
            "SELECT COUNT(*)" + From + Where,
            param, query, SortColumns, "i.Name ASC", ct);
    }

    public async Task<InventoryItemDetailDto?> GetByIdAsync(long id, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        return await conn.QueryFirstOrNullAsync<InventoryItemDetailDto>(
            Columns + From + " WHERE i.InventoryItemId = @Id", new { Id = id }, ct);
    }

    public async Task<bool> ItemCodeExistsAsync(string code, long? excludeId, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        var count = await conn.ScalarAsync<int>(
            "SELECT COUNT(1) FROM `InventoryItem` WHERE ItemCode = @Code AND (@ExcludeId IS NULL OR InventoryItemId <> @ExcludeId)",
            new { Code = code, ExcludeId = excludeId }, ct);
        return count > 0;
    }

    public async Task<bool> CategoryExistsAsync(long id, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        return await conn.ScalarAsync<int>("SELECT COUNT(1) FROM `ItemCategory` WHERE ItemCategoryId = @Id", new { Id = id }, ct) > 0;
    }

    public async Task<bool> UnitExistsAsync(long id, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        return await conn.ScalarAsync<int>("SELECT COUNT(1) FROM `Unit` WHERE UnitId = @Id", new { Id = id }, ct) > 0;
    }

    public async Task<bool> SupplierExistsAsync(long id, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        return await conn.ScalarAsync<int>("SELECT COUNT(1) FROM `Supplier` WHERE SupplierId = @Id", new { Id = id }, ct) > 0;
    }

    public async Task<long> CreateAsync(CreateInventoryItemRequest r, long userId, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        return await conn.InsertAsync(
            "INSERT INTO `InventoryItem` (ItemCode, Name, ItemCategoryId, UnitId, Description, ReorderLevel, MaxStockLevel, UnitCost," +
            " TaxPercent, IsPerishable, HasExpiry, ShelfLifeDays, PreferredSupplierId, StorageLocation, Barcode, CurrentStock, IsActive, CreatedBy)" +
            " VALUES (@ItemCode, @Name, @ItemCategoryId, @UnitId, @Description, @ReorderLevel, @MaxStockLevel, @UnitCost," +
            " @TaxPercent, @IsPerishable, @HasExpiry, @ShelfLifeDays, @PreferredSupplierId, @StorageLocation, @Barcode, 0, 1, @UserId)",
            new
            {
                r.ItemCode,
                r.Name,
                r.ItemCategoryId,
                r.UnitId,
                r.Description,
                r.ReorderLevel,
                r.MaxStockLevel,
                r.UnitCost,
                r.TaxPercent,
                r.IsPerishable,
                r.HasExpiry,
                r.ShelfLifeDays,
                r.PreferredSupplierId,
                r.StorageLocation,
                r.Barcode,
                UserId = userId
            }, ct);
    }

    public async Task UpdateAsync(long id, UpdateInventoryItemRequest r, bool isActive, long userId, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        await conn.ExecAsync(
            "UPDATE `InventoryItem` SET ItemCode = @ItemCode, Name = @Name, ItemCategoryId = @ItemCategoryId, UnitId = @UnitId," +
            " Description = @Description, ReorderLevel = @ReorderLevel, MaxStockLevel = @MaxStockLevel, UnitCost = @UnitCost," +
            " TaxPercent = @TaxPercent, IsPerishable = @IsPerishable, HasExpiry = @HasExpiry, ShelfLifeDays = @ShelfLifeDays," +
            " PreferredSupplierId = @PreferredSupplierId, StorageLocation = @StorageLocation, Barcode = @Barcode," +
            " IsActive = @IsActive, ModifiedBy = @UserId WHERE InventoryItemId = @Id",
            new
            {
                Id = id,
                r.ItemCode,
                r.Name,
                r.ItemCategoryId,
                r.UnitId,
                r.Description,
                r.ReorderLevel,
                r.MaxStockLevel,
                r.UnitCost,
                r.TaxPercent,
                r.IsPerishable,
                r.HasExpiry,
                r.ShelfLifeDays,
                r.PreferredSupplierId,
                r.StorageLocation,
                r.Barcode,
                IsActive = isActive,
                UserId = userId
            }, ct);
    }
}
