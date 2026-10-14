using Dairy.Application.Common;
using Dairy.Application.DTOs.Billing;
using Dairy.Application.Interfaces;
using Dairy.Application.Interfaces.Billing;
using Dairy.Domain.Constants;

namespace Dairy.Application.Services.Billing;

public class BillingService : IBillingService
{
    private readonly IBillingRepository _repo;
    private readonly ICurrentUser _user;

    public BillingService(IBillingRepository repo, ICurrentUser user)
    {
        _repo = repo;
        _user = user;
    }

    private bool IsCustomerUser => Roles.IsCustomerRole(_user.Role);

    public Task<List<CustomerOutstandingDto>> GetOutstandingAsync(long? customerId, CancellationToken ct = default)
    {
        if (IsCustomerUser) customerId = _user.CustomerId ?? throw new ForbiddenException();
        return _repo.GetOutstandingAsync(customerId, ct);
    }

    public Task<BillingSummaryDto> GetSummaryAsync(CancellationToken ct = default)
    {
        long? customerId = IsCustomerUser ? _user.CustomerId ?? throw new ForbiddenException() : null;
        return _repo.GetSummaryAsync(customerId, ct);
    }
}
