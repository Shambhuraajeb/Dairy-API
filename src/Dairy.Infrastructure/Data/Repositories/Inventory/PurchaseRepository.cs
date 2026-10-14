using Dairy.Application.Common;
using Dairy.Application.DTOs.Inventory;
using Dairy.Application.Interfaces;
using Dairy.Application.Interfaces.Inventory;
using Dairy.Application.Requests.Inventory;

namespace Dairy.Infrastructure.Data.Repositories.Inventory;

public class PurchaseRepository : IPurchaseRepository
{
    private static readonly Dictionary<string, string> SortColumns = new(StringComparer.OrdinalIgnoreCase)
    {
        ["id"] = "p.PurchaseId",
        ["purchaseNumber"] = "p.PurchaseNumber",
        ["purchaseDate"] = "p.PurchaseDate",
        ["supplierName"] = "s.Name",
        ["totalAmount"] = "p.TotalAmount",
        ["status"] = "p.Status",
        ["paymentStatus"] = "p.PaymentStatus",
        ["createdDate"] = "p.CreatedDate"
    };

    private const string Columns =
        "SELECT p.PurchaseId AS Id, p.PurchaseNumber, p.SupplierId, s.Name AS SupplierName, p.BranchId, p.PurchaseDate," +
        " p.ExpectedDate, p.SupplierInvoiceNo, p.SubTotal, p.TaxAmount, p.DiscountAmount, p.TotalAmount, p.PaidAmount," +
        " p.PaymentStatus, p.Status, p.ApprovedBy, au.DisplayName AS ApprovedByName, p.ApprovedDate," +
        " p.ReceivedBy, ru.DisplayName AS ReceivedByName, p.ReceivedDate, p.CancelledDate, p.CancelReason, p.Remarks," +
        " p.CreatedBy, cu.DisplayName AS CreatedByName, p.CreatedDate," +
        " (SELECT COUNT(*) FROM `PurchaseDetail` d WHERE d.PurchaseId = p.PurchaseId AND d.IsActive = 1) AS LineCount";

    private const string From =
        " FROM `Purchase` p" +
        " JOIN `Supplier` s ON s.SupplierId = p.SupplierId" +
        " LEFT JOIN `User` cu ON cu.UserId = p.CreatedBy" +
        " LEFT JOIN `User` au ON au.UserId = p.ApprovedBy" +
        " LEFT JOIN `User` ru ON ru.UserId = p.ReceivedBy";

    private const string Where =
        " WHERE p.IsActive = 1" +
        " AND (@Search IS NULL OR p.PurchaseNumber LIKE @Search OR s.Name LIKE @Search OR p.SupplierInvoiceNo LIKE @Search)" +
        " AND (@SupplierId IS NULL OR p.SupplierId = @SupplierId)" +
        " AND (@Status IS NULL OR p.Status = @Status)" +
        " AND (@PaymentStatus IS NULL OR p.PaymentStatus = @PaymentStatus)" +
        " AND (@From IS NULL OR p.PurchaseDate >= DATE(@From))" +
        " AND (@To IS NULL OR p.PurchaseDate <= DATE(@To))";

    private readonly IDbConnectionFactory _db;

    public PurchaseRepository(IDbConnectionFactory db) => _db = db;

    public async Task<PagedResult<PurchaseDto>> GetPagedAsync(PurchaseQuery query, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        var param = new
        {
            Search = query.SearchPattern,
            query.SupplierId,
            query.Status,
            query.PaymentStatus,
            query.From,
            query.To
        };
        return await conn.QueryPagedAsync<PurchaseDto>(
            Columns + From + Where,
            "SELECT COUNT(*)" + From + Where,
            param, query, SortColumns, "p.PurchaseDate DESC, p.PurchaseId DESC", ct);
    }

    public async Task<PurchaseDetailDto?> GetByIdAsync(long id, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        var header = await conn.QueryFirstOrNullAsync<PurchaseDetailDto>(
            Columns + From + " WHERE p.PurchaseId = @Id AND p.IsActive = 1", new { Id = id }, ct);
        if (header is null) return null;

        header.Lines = await conn.QueryListAsync<PurchaseLineDto>(
            "SELECT d.PurchaseDetailId AS Id, d.PurchaseId, d.InventoryItemId, i.ItemCode, i.Name AS ItemName, u.Name AS UnitName," +
            " d.Quantity, d.ReceivedQuantity, d.UnitCost, d.TaxPercent, d.TaxAmount, d.LineTotal, d.BatchNumber," +
            " d.ManufactureDate, d.ExpiryDate, d.Remarks" +
            " FROM `PurchaseDetail` d" +
            " JOIN `InventoryItem` i ON i.InventoryItemId = d.InventoryItemId" +
            " JOIN `Unit` u ON u.UnitId = i.UnitId" +
            " WHERE d.PurchaseId = @Id AND d.IsActive = 1 ORDER BY d.PurchaseDetailId",
            new { Id = id }, ct);
        return header;
    }

    public async Task<PurchaseCreatedDto> CreateAsync(long supplierId, DateTime? purchaseDate, string? remarks, string itemsJson, long userId, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        var row = await conn.QuerySpFirstOrDefaultAsync<PurchaseCreatedDto>("sp_CreatePurchase", new
        {
            p_SupplierId = supplierId,
            p_PurchaseDate = purchaseDate,
            p_Remarks = remarks,
            p_UserId = userId,
            p_ItemsJson = itemsJson
        }, ct);
        return row ?? throw new InvalidOperationException("sp_CreatePurchase did not return a row.");
    }

    public async Task ApproveAsync(long id, long userId, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        await conn.QuerySpFirstOrDefaultAsync<PurchaseCreatedDto>("sp_ApprovePurchase", new { p_PurchaseId = id, p_UserId = userId }, ct);
    }

    public async Task<PurchaseReceivedDto> ReceiveAsync(long id, DateTime? receivedDate, long userId, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        var row = await conn.QuerySpFirstOrDefaultAsync<PurchaseReceivedDto>("sp_ReceivePurchaseAndUpdateStock", new
        {
            p_PurchaseId = id,
            p_UserId = userId,
            p_ReceivedDate = receivedDate
        }, ct);
        return row ?? throw new InvalidOperationException("sp_ReceivePurchaseAndUpdateStock did not return a row.");
    }

    public async Task<int> CancelAsync(long id, string reason, long userId, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        return await conn.ExecAsync(
            "UPDATE `Purchase` p SET p.Status = 'Cancelled', p.CancelledBy = @UserId, p.CancelledDate = NOW()," +
            " p.CancelReason = @Reason, p.ModifiedBy = @UserId" +
            " WHERE p.PurchaseId = @Id AND p.Status IN ('Draft', 'Approved')" +
            " AND NOT EXISTS (SELECT 1 FROM `PurchaseDetail` d WHERE d.PurchaseId = p.PurchaseId AND d.ReceivedQuantity > 0)",
            new { Id = id, Reason = reason, UserId = userId }, ct);
    }
}
