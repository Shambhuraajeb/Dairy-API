using Dairy.Application.BusinessRules;
using Dairy.Application.Common;
using Dairy.Application.DTOs.Subscriptions;
using Dairy.Application.Interfaces;
using Dairy.Application.Interfaces.Subscriptions;
using Dairy.Application.Requests.Subscriptions;
using Dairy.Domain.Constants;

namespace Dairy.Application.Services.Subscriptions;

public class SubscriptionPlanService : ISubscriptionPlanService
{
    private readonly ISubscriptionPlanRepository _repo;
    private readonly IAuditService _audit;
    private readonly ICurrentUser _user;

    public SubscriptionPlanService(ISubscriptionPlanRepository repo, IAuditService audit, ICurrentUser user)
    {
        _repo = repo;
        _audit = audit;
        _user = user;
    }

    public Task<PagedResult<SubscriptionPlanDto>> GetPagedAsync(SubscriptionPlanQuery query, CancellationToken ct = default) =>
        _repo.GetPagedAsync(query, ct);

    public async Task<SubscriptionPlanDto> GetByIdAsync(long id, CancellationToken ct = default) =>
        await _repo.GetByIdAsync(id, ct) ?? throw new NotFoundException("Subscription plan", id);

    public async Task<SubscriptionPlanDto> CreateAsync(CreateSubscriptionPlanRequest request, CancellationToken ct = default)
    {
        var code = SalesValues.Clean(request.PlanCode)?.ToUpperInvariant();
        if (code is not null && await _repo.CodeExistsAsync(code, null, ct))
            throw new ConflictException($"A plan with code '{code}' already exists.");
        await EnsureProductAsync(request.ProductId, ct);

        var created = await _repo.CreateAsync(code, request, _user.UserId, ct);
        var dto = await _repo.GetByIdAsync(created.Id, ct) ?? throw new NotFoundException("Subscription plan", created.Id);

        await _audit.LogAsync(AuditActions.Create, "SubscriptionPlan", created.Id, null, dto, ct);
        return dto;
    }

    public async Task<SubscriptionPlanDto> UpdateAsync(long id, UpdateSubscriptionPlanRequest request, CancellationToken ct = default)
    {
        var existing = await _repo.GetByIdAsync(id, ct) ?? throw new NotFoundException("Subscription plan", id);
        await EnsureProductAsync(request.ProductId, ct);

        var isActive = request.IsActive ?? existing.IsActive;
        await _repo.UpdateAsync(id, request, isActive, _user.UserId, ct);
        var dto = await _repo.GetByIdAsync(id, ct) ?? throw new NotFoundException("Subscription plan", id);

        var action = existing.IsActive && !isActive ? AuditActions.Deactivate : AuditActions.Update;
        await _audit.LogAsync(action, "SubscriptionPlan", id, existing, dto, ct);
        return dto;
    }

    /// <summary>Soft delete: deactivates the plan (not allowed while customers are subscribed to it).</summary>
    public async Task DeleteAsync(long id, CancellationToken ct = default)
    {
        var existing = await _repo.GetByIdAsync(id, ct) ?? throw new NotFoundException("Subscription plan", id);
        var open = await _repo.CountOpenSubscriptionsAsync(id, ct);
        if (open > 0)
            throw new BusinessRuleException($"This plan has {open} active subscription(s). Cancel them before deactivating the plan.");

        await _repo.SetActiveAsync(id, false, _user.UserId, ct);
        await _audit.LogAsync(AuditActions.Deactivate, "SubscriptionPlan", id, existing, null, ct);
    }

    private async Task EnsureProductAsync(long? productId, CancellationToken ct)
    {
        if (productId.HasValue && !await _repo.ProductExistsAsync(productId.Value, ct))
            throw new BusinessRuleException("The selected product does not exist or is inactive.");
    }
}
