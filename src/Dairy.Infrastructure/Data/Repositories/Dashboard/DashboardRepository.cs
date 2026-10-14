using Dairy.Application.DTOs.Dashboard;
using Dairy.Application.Interfaces;
using Dairy.Application.Interfaces.Dashboard;
using Dapper;

namespace Dairy.Infrastructure.Data.Repositories.Dashboard;

/// <summary>Thin wrapper over the dashboard stored procedures (result-set order is defined in database/_proc_partA.sql).</summary>
public class DashboardRepository : IDashboardRepository
{
    private readonly IDbConnectionFactory _db;

    public DashboardRepository(IDbConnectionFactory db) => _db = db;

    public async Task<DashboardSummaryDto> GetSummaryAsync(long? branchId, long? collectionCenterId, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        var row = await conn.QuerySpFirstOrDefaultAsync<DashboardSummaryDto>(
            "sp_GetDashboardSummary",
            new { p_BranchId = branchId, p_CollectionCenterId = collectionCenterId },
            ct);
        return row ?? new DashboardSummaryDto();
    }

    public async Task<DashboardChartsDto> GetChartsAsync(int days, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        using var grid = await conn.QueryMultipleSpAsync("sp_GetDashboardCharts", new { p_Days = days }, ct);

        var charts = new DashboardChartsDto();
        charts.MilkCollectionTrend = await ReadPointsAsync(grid);
        charts.SalesTrend = await ReadPointsAsync(grid);
        charts.CollectionByCenter = await ReadPointsAsync(grid);
        charts.TopFarmers = await ReadPointsAsync(grid);
        charts.ProductSales = await ReadPointsAsync(grid);
        charts.ExpenseCategories = await ReadPointsAsync(grid);
        return charts;
    }

    public async Task<CollectionDashboardDto> GetCollectionAsync(long? collectionCenterId, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        var row = await conn.QuerySpFirstOrDefaultAsync<CollectionDashboardDto>(
            "sp_GetCollectionDashboard",
            new { p_CollectionCenterId = collectionCenterId },
            ct);
        return row ?? new CollectionDashboardDto { CollectionDate = DateTime.Today };
    }

    public async Task<InventoryDashboardDto> GetInventoryAsync(CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        using var grid = await conn.QueryMultipleSpAsync("sp_GetInventoryDashboard", null, ct);

        var dto = new InventoryDashboardDto();
        dto.Summary = (await grid.ReadAsync<InventorySummaryDto>()).FirstOrDefault() ?? new InventorySummaryDto();
        dto.LowStockItems = (await grid.ReadAsync<LowStockItemDto>()).ToList();
        dto.ExpiringBatches = (await grid.ReadAsync<ExpiringBatchDto>()).ToList();
        dto.RecentPurchases = (await grid.ReadAsync<RecentPurchaseDto>()).ToList();
        dto.ConsumptionTrend = await ReadPointsAsync(grid);
        return dto;
    }

    public async Task<CustomerDashboardDto> GetCustomerAsync(long customerId, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        using var grid = await conn.QueryMultipleSpAsync("sp_GetCustomerDashboard", new { p_CustomerId = customerId }, ct);

        var dto = new CustomerDashboardDto { CustomerId = customerId };
        dto.Subscriptions = (await grid.ReadAsync<DashSubscriptionDto>()).ToList();
        dto.TodayDeliveries = (await grid.ReadAsync<DashTodayDeliveryDto>()).ToList();
        dto.CurrentBill = (await grid.ReadAsync<DashCurrentBillDto>()).FirstOrDefault();
        dto.Outstanding = (await grid.ReadAsync<DashOutstandingDto>()).FirstOrDefault();
        dto.RecentDeliveries = (await grid.ReadAsync<DashRecentDeliveryDto>()).ToList();
        dto.OpenComplaints = (await grid.ReadAsync<DashComplaintDto>()).ToList();
        return dto;
    }

    private static async Task<List<ChartPointDto>> ReadPointsAsync(SqlMapper.GridReader grid)
    {
        if (grid.IsConsumed) return new List<ChartPointDto>();
        return (await grid.ReadAsync<ChartPointDto>()).ToList();
    }
}
