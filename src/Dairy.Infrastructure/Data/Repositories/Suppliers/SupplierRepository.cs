using Dairy.Application.Common;
using Dairy.Application.DTOs.Suppliers;
using Dairy.Application.Interfaces;
using Dairy.Application.Interfaces.Suppliers;
using Dairy.Application.Requests.Suppliers;

namespace Dairy.Infrastructure.Data.Repositories.Suppliers;

public class SupplierRepository : ISupplierRepository
{
    private static readonly Dictionary<string, string> SortColumns = new(StringComparer.OrdinalIgnoreCase)
    {
        ["id"] = "s.SupplierId",
        ["supplierCode"] = "s.SupplierCode",
        ["name"] = "s.Name",
        ["city"] = "s.City",
        ["isActive"] = "s.IsActive",
        ["createdDate"] = "s.CreatedDate"
    };

    private const string Select =
        "SELECT s.SupplierId AS Id, s.SupplierCode, s.Name, s.ContactPerson, s.Mobile, s.Email, s.AddressLine, s.City, s.State," +
        " s.Pincode, s.GstNumber, s.PanNo, s.PaymentTermsDays, s.IsActive, s.CreatedDate," +
        " (SELECT COUNT(*) FROM `Purchase` p WHERE p.SupplierId = s.SupplierId AND p.Status <> 'Cancelled') AS PurchaseCount," +
        " (SELECT COALESCE(SUM(p.TotalAmount), 0) FROM `Purchase` p WHERE p.SupplierId = s.SupplierId AND p.Status <> 'Cancelled') AS TotalPurchased" +
        " FROM `Supplier` s";

    private const string Where =
        " WHERE (@Search IS NULL OR s.SupplierCode LIKE @Search OR s.Name LIKE @Search OR s.ContactPerson LIKE @Search" +
        "        OR s.Mobile LIKE @Search OR s.City LIKE @Search)" +
        " AND (@IsActive IS NULL OR s.IsActive = @IsActive)";

    private readonly IDbConnectionFactory _db;

    public SupplierRepository(IDbConnectionFactory db) => _db = db;

    public async Task<PagedResult<SupplierDto>> GetPagedAsync(SupplierQuery query, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        var param = new { Search = query.SearchPattern, query.IsActive };
        return await conn.QueryPagedAsync<SupplierDto>(
            Select + Where,
            "SELECT COUNT(*) FROM `Supplier` s" + Where,
            param, query, SortColumns, "s.Name ASC", ct);
    }

    public async Task<SupplierDto?> GetByIdAsync(long id, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        return await conn.QueryFirstOrNullAsync<SupplierDto>(Select + " WHERE s.SupplierId = @Id", new { Id = id }, ct);
    }

    public async Task<bool> CodeExistsAsync(string code, long? excludeId, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        var count = await conn.ScalarAsync<int>(
            "SELECT COUNT(1) FROM `Supplier` WHERE SupplierCode = @Code AND (@ExcludeId IS NULL OR SupplierId <> @ExcludeId)",
            new { Code = code, ExcludeId = excludeId }, ct);
        return count > 0;
    }

    public async Task<long> CreateAsync(CreateSupplierRequest r, long userId, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        return await conn.InsertAsync(
            "INSERT INTO `Supplier` (SupplierCode, Name, ContactPerson, Mobile, Email, AddressLine, City, State, Pincode, GstNumber, PanNo," +
            " PaymentTermsDays, IsActive, CreatedBy)" +
            " VALUES (@SupplierCode, @Name, @ContactPerson, @Mobile, @Email, @AddressLine, @City, @State, @Pincode, @GstNumber, @PanNo," +
            " @PaymentTermsDays, 1, @UserId)",
            new
            {
                r.SupplierCode,
                r.Name,
                r.ContactPerson,
                r.Mobile,
                r.Email,
                r.AddressLine,
                r.City,
                r.State,
                r.Pincode,
                r.GstNumber,
                r.PanNo,
                r.PaymentTermsDays,
                UserId = userId
            }, ct);
    }

    public async Task UpdateAsync(long id, UpdateSupplierRequest r, bool isActive, long userId, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        await conn.ExecAsync(
            "UPDATE `Supplier` SET SupplierCode = @SupplierCode, Name = @Name, ContactPerson = @ContactPerson, Mobile = @Mobile," +
            " Email = @Email, AddressLine = @AddressLine, City = @City, State = @State, Pincode = @Pincode, GstNumber = @GstNumber," +
            " PanNo = @PanNo, PaymentTermsDays = @PaymentTermsDays, IsActive = @IsActive, ModifiedBy = @UserId" +
            " WHERE SupplierId = @Id",
            new
            {
                Id = id,
                r.SupplierCode,
                r.Name,
                r.ContactPerson,
                r.Mobile,
                r.Email,
                r.AddressLine,
                r.City,
                r.State,
                r.Pincode,
                r.GstNumber,
                r.PanNo,
                r.PaymentTermsDays,
                IsActive = isActive,
                UserId = userId
            }, ct);
    }
}
