using Dairy.Application.DTOs.Collection;
using Dairy.Application.Interfaces;
using Dairy.Application.Interfaces.Collection;

namespace Dairy.Infrastructure.Data.Repositories.Collection;

public class CollectionShiftRepository : ICollectionShiftRepository
{
    // Liters received = accepted (current quantity of non-rejected rows) + rejected (whole rows, or partial rejections).
    private const string AcceptedExpr = "CASE WHEN mc.Status <> 'Rejected' THEN mc.QuantityLiters ELSE 0 END";

    private const string RejectedExpr =
        "CASE WHEN mc.Status = 'Rejected' THEN mc.QuantityLiters " +
        "ELSE COALESCE((SELECT SUM(r.QuantityLiters) FROM `MilkRejection` r WHERE r.MilkCollectionId = mc.MilkCollectionId AND r.IsActive = 1), 0) END";

    private const string Metrics =
        "CAST(COUNT(DISTINCT mc.FarmerId) AS SIGNED) AS FarmersServed, " +
        "CAST(COUNT(mc.MilkCollectionId) AS SIGNED) AS TotalCollections, " +
        "COALESCE(SUM(" + AcceptedExpr + "), 0) + COALESCE(SUM(" + RejectedExpr + "), 0) AS TotalLiters, " +
        "COALESCE(SUM(" + AcceptedExpr + "), 0) AS AcceptedLiters, " +
        "COALESCE(SUM(" + RejectedExpr + "), 0) AS RejectedLiters, " +
        "COALESCE(ROUND(SUM(CASE WHEN mc.Status <> 'Rejected' THEN mc.Fat * mc.QuantityLiters END) / NULLIF(SUM(" + AcceptedExpr + "), 0), 2), 0) AS AvgFat, " +
        "COALESCE(ROUND(SUM(CASE WHEN mc.Status <> 'Rejected' THEN mc.Snf * mc.QuantityLiters END) / NULLIF(SUM(" + AcceptedExpr + "), 0), 2), 0) AS AvgSnf, " +
        "CAST(COALESCE(SUM(CASE WHEN mc.Status <> 'Rejected' THEN 1 ELSE 0 END), 0) AS SIGNED) AS AcceptedCount, " +
        "CAST(COALESCE(SUM(CASE WHEN mc.Status = 'Rejected' THEN 1 ELSE 0 END), 0) AS SIGNED) AS RejectedCount, " +
        "CAST(COALESCE(SUM(CASE WHEN mc.Status = 'Approved' THEN 1 ELSE 0 END), 0) AS SIGNED) AS ApprovedCount, " +
        "CAST(COALESCE(SUM(CASE WHEN mc.Status = 'Pending' THEN 1 ELSE 0 END), 0) AS SIGNED) AS PendingApprovals, " +
        "COALESCE(SUM(mc.Amount), 0) AS TotalAmount";

    private readonly IDbConnectionFactory _db;

    public CollectionShiftRepository(IDbConnectionFactory db) => _db = db;

    public async Task<List<CollectionShiftDto>> GetShiftsAsync(long? centerId, bool? isActive, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        return await conn.QueryListAsync<CollectionShiftDto>(
            "SELECT s.CollectionShiftId AS Id, s.CollectionCenterId, cc.Name AS CollectionCenterName, s.ShiftName, " +
            "s.StartTime, s.EndTime, s.Description, s.IsActive " +
            "FROM `CollectionShift` s " +
            "LEFT JOIN `CollectionCenter` cc ON cc.CollectionCenterId = s.CollectionCenterId " +
            "WHERE (@CenterId IS NULL OR s.CollectionCenterId IS NULL OR s.CollectionCenterId = @CenterId) " +
            "AND (@IsActive IS NULL OR s.IsActive = @IsActive) " +
            "ORDER BY s.StartTime, s.ShiftName, s.CollectionShiftId",
            new { CenterId = centerId, IsActive = isActive }, ct);
    }

    public async Task<ShiftSummaryDto> GetSummaryAsync(DateTime date, long? centerId, string? shiftName, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        var row = await conn.QueryFirstOrNullAsync<ShiftSummaryDto>(
            "SELECT " + Metrics + " FROM `MilkCollection` mc " +
            "WHERE mc.IsActive = 1 AND mc.CollectionDate = @OnDate " +
            "AND (@CenterId IS NULL OR mc.CollectionCenterId = @CenterId) " +
            "AND (@ShiftName IS NULL OR mc.ShiftId IN (SELECT s.CollectionShiftId FROM `CollectionShift` s WHERE s.ShiftName = @ShiftName))",
            new { OnDate = date.Date, CenterId = centerId, ShiftName = shiftName }, ct);
        return row ?? new ShiftSummaryDto();
    }

    public async Task<List<ShiftSummaryDto>> GetBreakdownAsync(DateTime date, long? centerId, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        return await conn.QueryListAsync<ShiftSummaryDto>(
            "SELECT s.CollectionShiftId AS ShiftId, s.ShiftName, s.StartTime, s.EndTime, " +
            "cc.CollectionCenterId, cc.Name AS CollectionCenterName, " + Metrics + " " +
            "FROM `CollectionShift` s " +
            "JOIN `CollectionCenter` cc ON cc.IsActive = 1 AND (s.CollectionCenterId IS NULL OR s.CollectionCenterId = cc.CollectionCenterId) " +
            "LEFT JOIN `MilkCollection` mc ON mc.ShiftId = s.CollectionShiftId AND mc.CollectionCenterId = cc.CollectionCenterId " +
            "AND mc.CollectionDate = @OnDate AND mc.IsActive = 1 " +
            "WHERE s.IsActive = 1 AND (@CenterId IS NULL OR cc.CollectionCenterId = @CenterId) " +
            "GROUP BY s.CollectionShiftId, s.ShiftName, s.StartTime, s.EndTime, cc.CollectionCenterId, cc.Name " +
            "ORDER BY cc.Name, s.StartTime",
            new { OnDate = date.Date, CenterId = centerId }, ct);
    }

    public async Task<int> GetPendingApprovalCountAsync(long? centerId, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        return await conn.ScalarAsync<int>(
            "SELECT COUNT(*) FROM `MilkCollection` mc WHERE mc.Status = 'Pending' AND mc.IsActive = 1 " +
            "AND (@CenterId IS NULL OR mc.CollectionCenterId = @CenterId)",
            new { CenterId = centerId }, ct);
    }
}
