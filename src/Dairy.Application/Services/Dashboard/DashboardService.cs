using Dairy.Application.Common;
using Dairy.Application.DTOs.Dashboard;
using Dairy.Application.Interfaces;
using Dairy.Application.Interfaces.Dashboard;
using Dairy.Domain.Constants;

namespace Dairy.Application.Services.Dashboard;

public class DashboardService : IDashboardService
{
    private readonly IDashboardRepository _repo;
    private readonly ICurrentUser _user;

    public DashboardService(IDashboardRepository repo, ICurrentUser user)
    {
        _repo = repo;
        _user = user;
    }

    public Task<DashboardSummaryDto> GetSummaryAsync(long? branchId, long? collectionCenterId, CancellationToken ct = default) =>
        _repo.GetSummaryAsync(branchId, collectionCenterId, ct);

    public Task<DashboardChartsDto> GetChartsAsync(int? days, CancellationToken ct = default) =>
        _repo.GetChartsAsync(Math.Clamp(days ?? 30, 1, 365), ct);

    public Task<CollectionDashboardDto> GetCollectionAsync(long? collectionCenterId, CancellationToken ct = default) =>
        _repo.GetCollectionAsync(collectionCenterId, ct);

    public Task<InventoryDashboardDto> GetInventoryAsync(CancellationToken ct = default) =>
        _repo.GetInventoryAsync(ct);

    public Task<CustomerDashboardDto> GetCustomerAsync(long? customerId, CancellationToken ct = default)
    {
        long id;
        if (Roles.IsCustomerRole(_user.Role))
        {
            // Customers only ever see their own dashboard, whatever they pass.
            id = _user.CustomerId ?? throw new ForbiddenException();
        }
        else
        {
            if (!_user.HasPermission(Permissions.CustomerView))
                throw new ForbiddenException();
            id = customerId ?? throw new RequestValidationException("customerId", "customerId is required.");
        }

        return _repo.GetCustomerAsync(id, ct);
    }
}
