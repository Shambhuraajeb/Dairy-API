using Dairy.Application.Common;
using Dairy.Application.DTOs.Subscriptions;
using Dairy.Application.Requests.Subscriptions;

namespace Dairy.Application.Interfaces.Subscriptions;

public interface ISubscriptionPlanRepository
{
    Task<PagedResult<SubscriptionPlanDto>> GetPagedAsync(SubscriptionPlanQuery query, CancellationToken ct = default);
    Task<SubscriptionPlanDto?> GetByIdAsync(long id, CancellationToken ct = default);
    Task<bool> CodeExistsAsync(string code, long? excludeId, CancellationToken ct = default);
    Task<bool> ProductExistsAsync(long productId, CancellationToken ct = default);
    Task<CreatedResult> CreateAsync(string? code, CreateSubscriptionPlanRequest request, long userId, CancellationToken ct = default);
    Task UpdateAsync(long id, UpdateSubscriptionPlanRequest request, bool isActive, long userId, CancellationToken ct = default);
    Task<int> CountOpenSubscriptionsAsync(long id, CancellationToken ct = default);
    Task SetActiveAsync(long id, bool isActive, long userId, CancellationToken ct = default);
}

public interface ISubscriptionPlanService
{
    Task<PagedResult<SubscriptionPlanDto>> GetPagedAsync(SubscriptionPlanQuery query, CancellationToken ct = default);
    Task<SubscriptionPlanDto> GetByIdAsync(long id, CancellationToken ct = default);
    Task<SubscriptionPlanDto> CreateAsync(CreateSubscriptionPlanRequest request, CancellationToken ct = default);
    Task<SubscriptionPlanDto> UpdateAsync(long id, UpdateSubscriptionPlanRequest request, CancellationToken ct = default);
    Task DeleteAsync(long id, CancellationToken ct = default);
}

public interface ISubscriptionRepository
{
    Task<PagedResult<SubscriptionDto>> GetPagedAsync(SubscriptionQuery query, CancellationToken ct = default);
    Task<SubscriptionDetailDto?> GetByIdAsync(long id, CancellationToken ct = default);
    Task<CreatedResult> CreateAsync(CreateSubscriptionRequest request, long userId, CancellationToken ct = default);
    Task<bool> AddressBelongsToCustomerAsync(long addressId, long customerId, CancellationToken ct = default);
    Task UpdateAsync(long id, UpdateSubscriptionRequest request, long userId, CancellationToken ct = default);
    Task PauseAsync(long id, PauseSubscriptionRequest request, long userId, CancellationToken ct = default);
    Task ResumeAsync(long id, long userId, CancellationToken ct = default);
    Task CancelAsync(long id, string? reason, long userId, CancellationToken ct = default);
    Task<SubscriptionOrdersGeneratedDto> GenerateOrdersAsync(DateTime forDate, long userId, CancellationToken ct = default);
}

public interface ISubscriptionService
{
    Task<PagedResult<SubscriptionDto>> GetPagedAsync(SubscriptionQuery query, CancellationToken ct = default);
    Task<SubscriptionDetailDto> GetByIdAsync(long id, CancellationToken ct = default);
    Task<SubscriptionDetailDto> CreateAsync(CreateSubscriptionRequest request, CancellationToken ct = default);
    Task<SubscriptionDetailDto> UpdateAsync(long id, UpdateSubscriptionRequest request, CancellationToken ct = default);
    Task<SubscriptionDetailDto> PauseAsync(long id, PauseSubscriptionRequest request, CancellationToken ct = default);
    Task<SubscriptionDetailDto> ResumeAsync(long id, CancellationToken ct = default);
    Task<SubscriptionDetailDto> CancelAsync(long id, CancelSubscriptionRequest request, CancellationToken ct = default);
    Task<SubscriptionOrdersGeneratedDto> GenerateOrdersAsync(GenerateSubscriptionOrdersRequest request, CancellationToken ct = default);
}
