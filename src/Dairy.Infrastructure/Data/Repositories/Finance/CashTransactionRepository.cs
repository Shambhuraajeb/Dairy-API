using Dairy.Application.Common;
using Dairy.Application.DTOs.Finance;
using Dairy.Application.Interfaces;
using Dairy.Application.Interfaces.Finance;
using Dairy.Application.Requests.Finance;

namespace Dairy.Infrastructure.Data.Repositories.Finance;

public class CashTransactionRepository : ICashTransactionRepository
{
    private static readonly Dictionary<string, string> SortColumns = new(StringComparer.OrdinalIgnoreCase)
    {
        ["id"] = "t.CashTransactionId",
        ["transactionDate"] = "t.TransactionDate",
        ["transactionType"] = "t.TransactionType",
        ["category"] = "t.Category",
        ["amount"] = "t.Amount",
        ["balanceAfter"] = "t.BalanceAfter"
    };

    private const string Columns =
        "SELECT t.CashTransactionId AS Id, t.TransactionDate, t.TransactionType, t.Category, t.Amount, t.BalanceAfter," +
        " t.PaymentModeId, pm.Name AS PaymentModeName, t.BranchId, b.Name AS BranchName, t.ReferenceType, t.ReferenceId," +
        " t.Remarks, t.CreatedBy, cu.DisplayName AS CreatedByName";

    private const string From =
        " FROM `CashTransaction` t" +
        " LEFT JOIN `PaymentMode` pm ON pm.PaymentModeId = t.PaymentModeId" +
        " LEFT JOIN `Branch` b ON b.BranchId = t.BranchId" +
        " LEFT JOIN `User` cu ON cu.UserId = t.CreatedBy";

    private const string Where =
        " WHERE (@Search IS NULL OR t.Category LIKE @Search OR t.Remarks LIKE @Search OR t.ReferenceType LIKE @Search)" +
        " AND (@Type IS NULL OR t.TransactionType = @Type)" +
        " AND (@Category IS NULL OR t.Category = @Category)" +
        " AND (@PaymentModeId IS NULL OR t.PaymentModeId = @PaymentModeId)" +
        " AND (@From IS NULL OR t.TransactionDate >= DATE(@From))" +
        " AND (@To IS NULL OR t.TransactionDate < DATE_ADD(DATE(@To), INTERVAL 1 DAY))";

    private const string PeriodWhere =
        " WHERE (@From IS NULL OR TransactionDate >= DATE(@From))" +
        " AND (@To IS NULL OR TransactionDate < DATE_ADD(DATE(@To), INTERVAL 1 DAY))";

    private readonly IDbConnectionFactory _db;

    public CashTransactionRepository(IDbConnectionFactory db) => _db = db;

    public async Task<PagedResult<CashTransactionDto>> GetPagedAsync(CashTransactionQuery query, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        var param = new
        {
            Search = query.SearchPattern,
            query.Type,
            query.Category,
            query.PaymentModeId,
            query.From,
            query.To
        };
        return await conn.QueryPagedAsync<CashTransactionDto>(
            Columns + From + Where,
            "SELECT COUNT(*)" + From + Where,
            param, query, SortColumns, "t.TransactionDate DESC, t.CashTransactionId DESC", ct);
    }

    public async Task<CashSummaryDto> GetSummaryAsync(DateTime? from, DateTime? to, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        var param = new { From = from, To = to };

        var totals = await conn.QueryFirstOrNullAsync<CashTotalsRow>(
            "SELECT COALESCE(SUM(CASE WHEN TransactionType = 'In' THEN Amount END), 0) AS TotalIn," +
            " COALESCE(SUM(CASE WHEN TransactionType = 'Out' THEN Amount END), 0) AS TotalOut," +
            " COUNT(*) AS TransactionCount" +
            " FROM `CashTransaction`" + PeriodWhere, param, ct) ?? new CashTotalsRow();

        var opening = await conn.ScalarAsync<decimal?>(
            "SELECT BalanceAfter FROM `CashTransaction` WHERE @From IS NOT NULL AND TransactionDate < DATE(@From)" +
            " ORDER BY TransactionDate DESC, CashTransactionId DESC LIMIT 1", param, ct) ?? 0m;

        var closing = await conn.ScalarAsync<decimal?>(
            "SELECT BalanceAfter FROM `CashTransaction`" +
            " WHERE (@To IS NULL OR TransactionDate < DATE_ADD(DATE(@To), INTERVAL 1 DAY))" +
            " ORDER BY TransactionDate DESC, CashTransactionId DESC LIMIT 1", param, ct) ?? opening;

        var byCategory = await conn.QueryListAsync<CashCategorySummaryDto>(
            "SELECT Category," +
            " COALESCE(SUM(CASE WHEN TransactionType = 'In' THEN Amount END), 0) AS TotalIn," +
            " COALESCE(SUM(CASE WHEN TransactionType = 'Out' THEN Amount END), 0) AS TotalOut" +
            " FROM `CashTransaction`" + PeriodWhere +
            " GROUP BY Category ORDER BY (COALESCE(SUM(CASE WHEN TransactionType = 'In' THEN Amount END), 0)" +
            " + COALESCE(SUM(CASE WHEN TransactionType = 'Out' THEN Amount END), 0)) DESC, Category", param, ct);

        return new CashSummaryDto
        {
            From = from,
            To = to,
            OpeningBalance = opening,
            TotalIn = totals.TotalIn,
            TotalOut = totals.TotalOut,
            NetChange = totals.TotalIn - totals.TotalOut,
            ClosingBalance = closing,
            TransactionCount = totals.TransactionCount,
            ByCategory = byCategory
        };
    }
}
