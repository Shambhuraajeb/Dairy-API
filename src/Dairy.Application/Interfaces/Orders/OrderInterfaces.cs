using Dairy.Application.Common;
using Dairy.Application.DTOs.Orders;
using Dairy.Application.Requests.Orders;

namespace Dairy.Application.Interfaces.Orders;

public interface IOrderRepository
{
    Task<PagedResult<OrderListDto>> GetPagedAsync(OrderQuery query, CancellationToken ct = default);

    /// <summary>Pending/Confirmed/Failed orders that have no live delivery yet.</summary>
    Task<PagedResult<OrderListDto>> GetAwaitingSchedulingAsync(OrderQuery query, CancellationToken ct = default);
    Task<OrderDetailDto?> GetByIdAsync(long id, CancellationToken ct = default);
    Task<CreatedResult> CreateAsync(long customerId, CreateOrderRequest request, bool allowPriceOverride, long userId, CancellationToken ct = default);
    Task<bool> AddressBelongsToCustomerAsync(long addressId, long customerId, CancellationToken ct = default);
    Task UpdateAsync(long id, UpdateOrderRequest request, long userId, CancellationToken ct = default);
    Task<bool> CancelAsync(long id, string? reason, long userId, CancellationToken ct = default);
}

public interface IOrderService
{
    Task<PagedResult<OrderListDto>> GetPagedAsync(OrderQuery query, CancellationToken ct = default);
    Task<PagedResult<OrderListDto>> GetAwaitingSchedulingAsync(OrderQuery query, CancellationToken ct = default);
    Task<OrderDetailDto> GetByIdAsync(long id, CancellationToken ct = default);
    Task<OrderDetailDto> CreateAsync(CreateOrderRequest request, CancellationToken ct = default);
    Task<OrderDetailDto> UpdateAsync(long id, UpdateOrderRequest request, CancellationToken ct = default);
    Task<OrderDetailDto> CancelAsync(long id, CancelOrderRequest request, CancellationToken ct = default);
}
