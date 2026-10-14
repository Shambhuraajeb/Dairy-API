using Dairy.Application.BusinessRules;
using Dairy.Application.Common;
using Dairy.Application.DTOs.Subscriptions;
using Dairy.Application.Interfaces;
using Dairy.Application.Interfaces.Subscriptions;
using Dairy.Application.Requests.Subscriptions;
using Dairy.Domain.Constants;

namespace Dairy.Application.Services.Subscriptions;

public class SubscriptionService : ISubscriptionService
{
    private readonly ISubscriptionRepository _repo;
    private readonly IAuditService _audit;
    private readonly ICurrentUser _user;

    public SubscriptionService(ISubscriptionRepository repo, IAuditService audit, ICurrentUser user)
    {
        _repo = repo;
        _audit = audit;
        _user = user;
    }

    private bool IsCustomerUser => Roles.IsCustomerRole(_user.Role);

    public Task<PagedResult<SubscriptionDto>> GetPagedAsync(SubscriptionQuery query, CancellationToken ct = default)
    {
        if (IsCustomerUser) query.CustomerId = _user.CustomerId ?? throw new ForbiddenException();
        return _repo.GetPagedAsync(query, ct);
    }

    public async Task<SubscriptionDetailDto> GetByIdAsync(long id, CancellationToken ct = default)
    {
        var dto = await _repo.GetByIdAsync(id, ct) ?? throw new NotFoundException("Subscription", id);
        if (IsCustomerUser && dto.CustomerId != _user.CustomerId) throw new NotFoundException("Subscription", id);
        return dto;
    }

    public async Task<SubscriptionDetailDto> CreateAsync(CreateSubscriptionRequest request, CancellationToken ct = default)
    {
        if (IsCustomerUser && request.CustomerId != _user.CustomerId)
            throw new ForbiddenException();

        var created = await _repo.CreateAsync(request, _user.UserId, ct);
        var dto = await _repo.GetByIdAsync(created.Id, ct) ?? throw new NotFoundException("Subscription", created.Id);

        await _audit.LogAsync(AuditActions.Create, "CustomerSubscription", created.Id, null, dto, ct);
        return dto;
    }

    public async Task<SubscriptionDetailDto> UpdateAsync(long id, UpdateSubscriptionRequest request, CancellationToken ct = default)
    {
        var existing = await GetByIdAsync(id, ct);
        if (existing.Status != SubscriptionStatus.Active && existing.Status != SubscriptionStatus.Paused)
            throw new BusinessRuleException("Only an active or paused subscription can be edited.");
        if (request.EndDate.HasValue && request.EndDate.Value.Date < existing.StartDate.Date)
            throw new BusinessRuleException("End date cannot be before the start date.");
        if (request.CustomerAddressId.HasValue &&
            !await _repo.AddressBelongsToCustomerAsync(request.CustomerAddressId.Value, existing.CustomerId, ct))
            throw new BusinessRuleException("Delivery address does not belong to this customer.");

        await _repo.UpdateAsync(id, request, _user.UserId, ct);
        var dto = await GetByIdAsync(id, ct);

        await _audit.LogAsync(AuditActions.Update, "CustomerSubscription", id, existing, dto, ct);
        return dto;
    }

    public async Task<SubscriptionDetailDto> PauseAsync(long id, PauseSubscriptionRequest request, CancellationToken ct = default)
    {
        var existing = await GetByIdAsync(id, ct); // ownership check for customer roles

        await _repo.PauseAsync(id, request, _user.UserId, ct);
        var dto = await GetByIdAsync(id, ct);

        await _audit.LogAsync("Pause", "CustomerSubscription", id, existing, dto, ct);
        return dto;
    }

    public async Task<SubscriptionDetailDto> ResumeAsync(long id, CancellationToken ct = default)
    {
        var existing = await GetByIdAsync(id, ct);

        await _repo.ResumeAsync(id, _user.UserId, ct);
        var dto = await GetByIdAsync(id, ct);

        await _audit.LogAsync("Resume", "CustomerSubscription", id, existing, dto, ct);
        return dto;
    }

    public async Task<SubscriptionDetailDto> CancelAsync(long id, CancelSubscriptionRequest request, CancellationToken ct = default)
    {
        if (IsCustomerUser)
            throw new ForbiddenException("Please contact the dairy to cancel a subscription.");

        var existing = await GetByIdAsync(id, ct);
        if (existing.Status != SubscriptionStatus.Active && existing.Status != SubscriptionStatus.Paused)
            throw new BusinessRuleException("Only an active or paused subscription can be cancelled.");

        await _repo.CancelAsync(id, request.Reason, _user.UserId, ct);
        var dto = await GetByIdAsync(id, ct);

        await _audit.LogAsync("Cancel", "CustomerSubscription", id, existing, dto, ct);
        return dto;
    }

    public async Task<SubscriptionOrdersGeneratedDto> GenerateOrdersAsync(GenerateSubscriptionOrdersRequest request, CancellationToken ct = default)
    {
        var forDate = (request.ForDate ?? DateTime.Today).Date;
        var result = await _repo.GenerateOrdersAsync(forDate, _user.UserId, ct);

        await _audit.LogAsync(AuditActions.Create, "SalesOrder", $"subscription-orders:{forDate:yyyy-MM-dd}", null, result, ct);
        return result;
    }
}
