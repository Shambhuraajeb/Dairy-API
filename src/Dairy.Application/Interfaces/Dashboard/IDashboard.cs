using Dairy.Application.DTOs.Dashboard;

namespace Dairy.Application.Interfaces.Dashboard;

public interface IDashboardRepository
{
    Task<DashboardSummaryDto> GetSummaryAsync(long? branchId, long? collectionCenterId, CancellationToken ct = default);
    Task<DashboardChartsDto> GetChartsAsync(int days, CancellationToken ct = default);
    Task<CollectionDashboardDto> GetCollectionAsync(long? collectionCenterId, CancellationToken ct = default);
    Task<InventoryDashboardDto> GetInventoryAsync(CancellationToken ct = default);
    Task<CustomerDashboardDto> GetCustomerAsync(long customerId, CancellationToken ct = default);
}

public interface IDashboardService
{
    Task<DashboardSummaryDto> GetSummaryAsync(long? branchId, long? collectionCenterId, CancellationToken ct = default);
    Task<DashboardChartsDto> GetChartsAsync(int? days, CancellationToken ct = default);
    Task<CollectionDashboardDto> GetCollectionAsync(long? collectionCenterId, CancellationToken ct = default);
    Task<InventoryDashboardDto> GetInventoryAsync(CancellationToken ct = default);
    Task<CustomerDashboardDto> GetCustomerAsync(long? customerId, CancellationToken ct = default);
}
