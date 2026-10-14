using Dairy.Application.DTOs.Reports;
using Dairy.Application.Interfaces;
using Dairy.Application.Interfaces.Reports;
using Dairy.Application.Requests.Reports;
using MySqlConnector;

namespace Dairy.Infrastructure.Data.Repositories.Reports;

public class ReportRepository : IReportRepository
{
    private const string AnimalHealthSql =
        "SELECT h.AnimalHealthRecordId AS Id, h.RecordDate, a.TagNumber, a.Name AS AnimalName, a.AnimalType, " +
        "f.FarmerCode, f.FullName AS FarmerName, h.HealthCondition, h.Severity, h.Diagnosis, h.Treatment, " +
        "h.VetName, h.Cost, h.Status, h.FollowUpDate, h.RecoveredDate " +
        "FROM `AnimalHealthRecord` h " +
        "JOIN `Animal` a ON a.AnimalId = h.AnimalId " +
        "JOIN `Farmer` f ON f.FarmerId = a.FarmerId " +
        "WHERE h.IsActive = 1 " +
        "AND (@FromDate IS NULL OR h.RecordDate >= @FromDate) " +
        "AND (@ToDate IS NULL OR h.RecordDate <= @ToDate) " +
        "AND (@FarmerId IS NULL OR a.FarmerId = @FarmerId) " +
        "AND (@BranchId IS NULL OR f.BranchId = @BranchId) " +
        "AND (@CollectionCenterId IS NULL OR f.CollectionCenterId = @CollectionCenterId) " +
        "AND (@Status IS NULL OR h.Status = @Status) " +
        "ORDER BY h.RecordDate DESC, h.AnimalHealthRecordId DESC";

    // Latest completed dose per animal+vaccine whose next due date falls on/before @ToDate (overdue included).
    private const string VaccinationDueSql =
        "SELECT v.VaccinationId AS Id, a.TagNumber, a.Name AS AnimalName, a.AnimalType, f.FarmerCode, f.FullName AS FarmerName, " +
        "v.VaccineName, v.VaccinationDate AS LastVaccinationDate, v.NextDueDate, DATEDIFF(v.NextDueDate, CURDATE()) AS DaysToDue, " +
        "IF(v.NextDueDate < CURDATE(), 'Overdue', 'Due') AS DueStatus " +
        "FROM `Vaccination` v " +
        "JOIN `Animal` a ON a.AnimalId = v.AnimalId " +
        "JOIN `Farmer` f ON f.FarmerId = a.FarmerId " +
        "WHERE v.IsActive = 1 AND v.Status = 'Completed' AND v.NextDueDate IS NOT NULL " +
        "AND a.IsActive = 1 AND a.Status IN ('Active', 'Sick') " +
        "AND v.NextDueDate <= @ToDate " +
        "AND (@FromDate IS NULL OR v.NextDueDate >= @FromDate) " +
        "AND (@FarmerId IS NULL OR a.FarmerId = @FarmerId) " +
        "AND (@BranchId IS NULL OR f.BranchId = @BranchId) " +
        "AND (@CollectionCenterId IS NULL OR f.CollectionCenterId = @CollectionCenterId) " +
        "AND (@Status IS NULL OR IF(v.NextDueDate < CURDATE(), 'Overdue', 'Due') = @Status) " +
        "AND NOT EXISTS (SELECT 1 FROM `Vaccination` v2 WHERE v2.AnimalId = v.AnimalId AND v2.VaccineName = v.VaccineName " +
        "AND v2.IsActive = 1 AND v2.VaccinationDate > v.VaccinationDate) " +
        "ORDER BY v.NextDueDate, a.TagNumber";

    private readonly IDbConnectionFactory _db;

    public ReportRepository(IDbConnectionFactory db) => _db = db;

    public async Task<ReportData> RunAsync(string code, ReportQuery q, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);

        DateTime? from = q.From?.Date;
        DateTime? to = q.To?.Date;
        string? status = string.IsNullOrWhiteSpace(q.Status) ? null : q.Status.Trim();

        switch (code)
        {
            case "daily-collection": return Rows(await CollectionAsync(conn, "Daily", from, to, q, status, ct));
            case "shift-collection": return Rows(await CollectionAsync(conn, "Shift", from, to, q, status, ct));
            case "farmer-collection": return Rows(await CollectionAsync(conn, "Farmer", from, to, q, status, ct));
            case "milk-quality": return Rows(await CollectionAsync(conn, "Quality", from, to, q, status, ct));
            case "rejected-milk": return Rows(await CollectionAsync(conn, "Rejected", from, to, q, status, ct));

            case "farmer-settlement": return Rows(await FarmerPaymentAsync(conn, "Settlement", from, to, q, status, ct));
            case "farmer-outstanding": return Rows(await FarmerPaymentAsync(conn, "Outstanding", from, to, q, null, ct));

            case "animal-health":
                return Rows(await SqlRowsAsync(conn, AnimalHealthSql,
                    new { FromDate = from, ToDate = to, q.FarmerId, q.BranchId, q.CollectionCenterId, Status = status }, ct));

            case "vaccination-due":
            {
                DateTime dueUntil = to ?? DateTime.Today.AddDays(30);
                return Rows(await SqlRowsAsync(conn, VaccinationDueSql,
                    new { FromDate = from, ToDate = dueUntil, q.FarmerId, q.BranchId, q.CollectionCenterId, Status = status }, ct));
            }

            case "inventory-stock": return Rows(await InventoryAsync(conn, "Stock", from, to, q, ct));
            case "low-stock": return Rows(await InventoryAsync(conn, "LowStock", from, to, q, ct));
            case "expiring-batches": return Rows(await InventoryAsync(conn, "Expiring", from, to, q, ct));
            case "purchase": return Rows(await InventoryAsync(conn, "Purchase", from, to, q, ct));

            case "sales": return Rows(await SalesAsync(conn, "Sales", from, to, q, status, ct));
            case "customer-outstanding": return Rows(await SalesAsync(conn, "CustomerOutstanding", from, to, q, status, ct));
            case "delivery-performance": return Rows(await SalesAsync(conn, "DeliveryPerformance", from, to, q, status, ct));
            case "revenue": return Rows(await SalesAsync(conn, "Revenue", from, to, q, status, ct));

            case "expenses":
                return Rows(await SpRowsAsync(conn, "sp_GetExpenseReport",
                    new { p_FromDate = from, p_ToDate = to, p_ExpenseCategoryId = q.CategoryId, p_Status = status }, ct));

            case "profit-loss":
                return await ProfitLossAsync(conn, from, to, q.BranchId, ct);

            default:
                throw new ArgumentOutOfRangeException(nameof(code), code, "Unknown report code.");
        }
    }

    // ---------- stored-procedure wrappers ----------

    private static Task<List<Dictionary<string, object?>>> CollectionAsync(
        MySqlConnection conn, string groupBy, DateTime? from, DateTime? to, ReportQuery q, string? status, CancellationToken ct) =>
        SpRowsAsync(conn, "sp_GetCollectionReport", new
        {
            p_FromDate = from,
            p_ToDate = to,
            p_BranchId = q.BranchId,
            p_CollectionCenterId = q.CollectionCenterId,
            p_FarmerId = q.FarmerId,
            p_ShiftId = q.ShiftId,
            p_Status = status,
            p_GroupBy = groupBy
        }, ct);

    private static Task<List<Dictionary<string, object?>>> FarmerPaymentAsync(
        MySqlConnection conn, string mode, DateTime? from, DateTime? to, ReportQuery q, string? status, CancellationToken ct) =>
        SpRowsAsync(conn, "sp_GetFarmerPaymentReport", new
        {
            p_FromDate = from,
            p_ToDate = to,
            p_FarmerId = q.FarmerId,
            p_Status = status,
            p_Mode = mode
        }, ct);

    private static Task<List<Dictionary<string, object?>>> InventoryAsync(
        MySqlConnection conn, string mode, DateTime? from, DateTime? to, ReportQuery q, CancellationToken ct) =>
        SpRowsAsync(conn, "sp_GetInventoryReport", new
        {
            p_FromDate = from,
            p_ToDate = to,
            p_ItemCategoryId = q.CategoryId,
            p_InventoryItemId = q.ItemId,
            p_Mode = mode
        }, ct);

    private static Task<List<Dictionary<string, object?>>> SalesAsync(
        MySqlConnection conn, string mode, DateTime? from, DateTime? to, ReportQuery q, string? status, CancellationToken ct) =>
        SpRowsAsync(conn, "sp_GetSalesReport", new
        {
            p_FromDate = from,
            p_ToDate = to,
            p_CustomerId = q.CustomerId,
            p_ProductId = q.ProductId,
            p_Status = status,
            p_Mode = mode
        }, ct);

    private static async Task<ReportData> ProfitLossAsync(MySqlConnection conn, DateTime? from, DateTime? to, long? branchId, CancellationToken ct)
    {
        using var grid = await conn.QueryMultipleSpAsync("sp_GetProfitLossSummary",
            new { p_FromDate = from, p_ToDate = to, p_BranchId = branchId }, ct);

        var summaryRows = (await grid.ReadAsync()).ToList();
        var byCategory = (await grid.ReadAsync()).Select(r => ToDictionary((object)r)).ToList();
        var lines = (await grid.ReadAsync()).Select(r => ToDictionary((object)r)).ToList();

        return new ReportData
        {
            Rows = lines,
            Summary = summaryRows.Count > 0 ? ToDictionary((object)summaryRows[0]) : null,
            Extra = new Dictionary<string, List<Dictionary<string, object?>>> { ["expenseByCategory"] = byCategory }
        };
    }

    // ---------- helpers ----------

    private static ReportData Rows(List<Dictionary<string, object?>> rows) => new() { Rows = rows };

    private static async Task<List<Dictionary<string, object?>>> SpRowsAsync(MySqlConnection conn, string procedure, object param, CancellationToken ct)
    {
        var rows = await conn.QuerySpAsync<dynamic>(procedure, param, ct);
        return rows.Select(r => ToDictionary((object)r)).ToList();
    }

    private static async Task<List<Dictionary<string, object?>>> SqlRowsAsync(MySqlConnection conn, string sql, object param, CancellationToken ct)
    {
        var rows = await conn.QueryListAsync<dynamic>(sql, param, ct);
        return rows.Select(r => ToDictionary((object)r)).ToList();
    }

    /// <summary>Dapper dynamic row -> dictionary with camelCase keys (Id -> id, FarmerName -> farmerName).</summary>
    private static Dictionary<string, object?> ToDictionary(object row)
    {
        var result = new Dictionary<string, object?>(StringComparer.Ordinal);
        foreach (var kv in (IDictionary<string, object>)row)
        {
            var key = kv.Key.Length == 0 ? kv.Key : char.ToLowerInvariant(kv.Key[0]) + kv.Key.Substring(1);
            result[key] = kv.Value is DBNull ? null : kv.Value;
        }
        return result;
    }
}
