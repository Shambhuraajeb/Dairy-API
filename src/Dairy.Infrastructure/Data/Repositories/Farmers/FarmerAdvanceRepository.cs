using Dairy.Application.Common;
using Dairy.Application.DTOs.Farmers;
using Dairy.Application.Interfaces;
using Dairy.Application.Interfaces.Farmers;
using Dairy.Application.Requests.Farmers;

namespace Dairy.Infrastructure.Data.Repositories.Farmers;

public class FarmerAdvanceRepository : IFarmerAdvanceRepository
{
    private static readonly Dictionary<string, string> SortColumns = new(StringComparer.OrdinalIgnoreCase)
    {
        ["id"] = "a.FarmerAdvanceId",
        ["advanceNumber"] = "a.AdvanceNumber",
        ["advanceDate"] = "a.AdvanceDate",
        ["farmerName"] = "f.FullName",
        ["amount"] = "a.Amount",
        ["balanceAmount"] = "a.BalanceAmount",
        ["status"] = "a.Status"
    };

    private const string From =
        " FROM `FarmerAdvance` a JOIN `Farmer` f ON f.FarmerId = a.FarmerId " +
        "LEFT JOIN `PaymentMode` pm ON pm.PaymentModeId = a.PaymentModeId";

    private const string Select =
        "SELECT a.FarmerAdvanceId AS Id, a.AdvanceNumber, a.FarmerId, f.FarmerCode, f.FullName AS FarmerName, a.AdvanceDate, " +
        "a.Amount, a.RecoveredAmount, a.BalanceAmount, a.RecoveryPerSettlement, a.Purpose, a.PaymentModeId, " +
        "pm.Name AS PaymentModeName, a.ReferenceNo, a.Status, a.ApprovedDate, a.Remarks, a.CreatedDate" + From;

    private const string Where =
        " WHERE a.IsActive = 1" +
        " AND (@Search IS NULL OR a.AdvanceNumber LIKE @Search OR f.FullName LIKE @Search OR f.FarmerCode LIKE @Search)" +
        " AND (@FarmerId IS NULL OR a.FarmerId = @FarmerId)" +
        " AND (@Status IS NULL OR a.Status = @Status)" +
        " AND (@DateFrom IS NULL OR a.AdvanceDate >= @DateFrom)" +
        " AND (@DateTo IS NULL OR a.AdvanceDate <= @DateTo)";

    private readonly IDbConnectionFactory _db;

    public FarmerAdvanceRepository(IDbConnectionFactory db) => _db = db;

    public async Task<PagedResult<FarmerAdvanceDto>> GetPagedAsync(FarmerAdvanceQuery query, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        var param = new
        {
            Search = query.SearchPattern,
            query.FarmerId,
            Status = string.IsNullOrWhiteSpace(query.Status) ? null : query.Status.Trim(),
            DateFrom = query.DateFrom?.Date,
            DateTo = query.DateTo?.Date
        };
        return await conn.QueryPagedAsync<FarmerAdvanceDto>(
            Select + Where,
            "SELECT COUNT(*)" + From + Where,
            param, query, SortColumns, "a.AdvanceDate DESC, a.FarmerAdvanceId DESC", ct);
    }

    public async Task<FarmerAdvanceDto?> GetByIdAsync(long id, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        return await conn.QueryFirstOrNullAsync<FarmerAdvanceDto>(
            Select + " WHERE a.FarmerAdvanceId = @Id AND a.IsActive = 1", new { Id = id }, ct);
    }

    public async Task<long> CreateAsync(CreateFarmerAdvanceRequest request, DateTime advanceDate, long userId, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);

        // sp_CreateFarmerAdvance also posts the cash-book entry and writes the AuditLog row.
        var created = await conn.QueryCreatedSpAsync("sp_CreateFarmerAdvance", new
        {
            p_FarmerId = request.FarmerId,
            p_Amount = request.Amount,
            p_AdvanceDate = advanceDate.Date,
            p_RecoveryPerSettlement = request.RecoveryPerSettlement,
            p_Purpose = Clean(request.Purpose),
            p_PaymentModeId = request.PaymentModeId,
            p_ReferenceNo = Clean(request.ReferenceNo),
            p_Remarks = Clean(request.Remarks),
            p_UserId = userId
        }, ct);
        return created.Id;
    }

    private static string? Clean(string? value)
    {
        var v = value?.Trim();
        return string.IsNullOrEmpty(v) ? null : v;
    }
}
