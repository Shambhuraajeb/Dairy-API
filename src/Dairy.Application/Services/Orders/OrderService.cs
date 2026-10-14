using Dairy.Application.Common;
using Dairy.Application.DTOs.Orders;
using Dairy.Application.Interfaces;
using Dairy.Application.Interfaces.Orders;
using Dairy.Application.Requests.Orders;
using Dairy.Domain.Constants;

namespace Dairy.Application.Services.Orders;

public class OrderService : IOrderService
{
    private readonly IOrderRepository _repo;
    private readonly IAuditService _audit;
    private readonly ICurrentUser _user;

    public OrderService(IOrderRepository repo, IAuditService audit, ICurrentUser user)
    {
        _repo = repo;
        _audit = audit;
        _user = user;
    }

    private bool IsCustomerUser => Roles.IsCustomerRole(_user.Role);

    public Task<PagedResult<OrderListDto>> GetPagedAsync(OrderQuery query, CancellationToken ct = default)
    {
        if (IsCustomerUser) query.CustomerId = _user.CustomerId ?? throw new ForbiddenException();
        return _repo.GetPagedAsync(query, ct);
    }

    public Task<PagedResult<OrderListDto>> GetAwaitingSchedulingAsync(OrderQuery query, CancellationToken ct = default)
    {
        if (IsCustomerUser || _user.IsInRole(Roles.Driver)) throw new ForbiddenException();
        return _repo.GetAwaitingSchedulingAsync(query, ct);
    }

    public async Task<OrderDetailDto> GetByIdAsync(long id, CancellationToken ct = default)
    {
        var dto = await _repo.GetByIdAsync(id, ct) ?? throw new NotFoundException("Order", id);
        if (IsCustomerUser && dto.CustomerId != _user.CustomerId) throw new NotFoundException("Order", id);
        return dto;
    }

    public async Task<OrderDetailDto> CreateAsync(CreateOrderRequest request, CancellationToken ct = default)
    {
        long customerId;
        if (IsCustomerUser)
            customerId = _user.CustomerId ?? throw new ForbiddenException();
        else
            customerId = request.CustomerId ?? throw new RequestValidationException("customerId", "Customer is required.");

        var created = await _repo.CreateAsync(customerId, request, allowPriceOverride: !IsCustomerUser, _user.UserId, ct);
        var dto = await _repo.GetByIdAsync(created.Id, ct) ?? throw new NotFoundException("Order", created.Id);

        await _audit.LogAsync(AuditActions.Create, "SalesOrder", created.Id, null, dto, ct);
        return dto;
    }

    public async Task<OrderDetailDto> UpdateAsync(long id, UpdateOrderRequest request, CancellationToken ct = default)
    {
        var existing = await GetByIdAsync(id, ct);
        if (existing.Status != OrderStatus.Pending && existing.Status != OrderStatus.Confirmed)
            throw new BusinessRuleException($"An order that is {existing.Status} can no longer be edited. Only Pending or Confirmed orders can be changed.");
        if (existing.InvoiceId.HasValue)
            throw new BusinessRuleException("An invoiced order cannot be edited.");
        if (request.CustomerAddressId.HasValue &&
            !await _repo.AddressBelongsToCustomerAsync(request.CustomerAddressId.Value, existing.CustomerId, ct))
            throw new BusinessRuleException("Delivery address does not belong to this customer.");

        await _repo.UpdateAsync(id, request, _user.UserId, ct);
        var dto = await GetByIdAsync(id, ct);

        await _audit.LogAsync(AuditActions.Update, "SalesOrder", id, existing, dto, ct);
        return dto;
    }

    public async Task<OrderDetailDto> CancelAsync(long id, CancelOrderRequest request, CancellationToken ct = default)
    {
        var existing = await GetByIdAsync(id, ct); // ownership for customer roles

        if (existing.Status == OrderStatus.Cancelled)
            throw new BusinessRuleException("The order is already cancelled.");
        if (existing.InvoiceId.HasValue)
            throw new BusinessRuleException("An invoiced order cannot be cancelled. Cancel the invoice first.");

        var cancellable = IsCustomerUser
            ? new[] { OrderStatus.Pending, OrderStatus.Confirmed }
            : new[] { OrderStatus.Pending, OrderStatus.Confirmed, OrderStatus.Scheduled, "Failed" };
        if (!cancellable.Contains(existing.Status))
            throw new BusinessRuleException($"An order that is {existing.Status} cannot be cancelled" +
                                            (IsCustomerUser ? ". Please contact the dairy." : "."));

        if (!await _repo.CancelAsync(id, request.Reason, _user.UserId, ct))
            throw new BusinessRuleException("The order state changed and it can no longer be cancelled. Please refresh.");

        var dto = await GetByIdAsync(id, ct);
        await _audit.LogAsync("Cancel", "SalesOrder", id, existing, dto, ct);
        return dto;
    }
}
