using Dairy.Application.BusinessRules;
using Dairy.Application.Common;
using Dairy.Application.DTOs.Billing;
using Dairy.Application.DTOs.Customers;
using Dairy.Application.DTOs.Orders;
using Dairy.Application.DTOs.Subscriptions;
using Dairy.Application.Interfaces;
using Dairy.Application.Interfaces.Billing;
using Dairy.Application.Interfaces.Customers;
using Dairy.Application.Interfaces.Orders;
using Dairy.Application.Interfaces.Subscriptions;
using Dairy.Application.Requests.Billing;
using Dairy.Application.Requests.Customers;
using Dairy.Application.Requests.Orders;
using Dairy.Application.Requests.Subscriptions;
using Dairy.Domain.Constants;

namespace Dairy.Application.Services.Customers;

public class CustomerService : ICustomerService
{
    private readonly ICustomerRepository _repo;
    private readonly IOrderService _orders;
    private readonly IInvoiceService _invoices;
    private readonly ICustomerPaymentService _payments;
    private readonly ISubscriptionService _subscriptions;
    private readonly IComplaintService _complaints;
    private readonly IAuditService _audit;
    private readonly ICurrentUser _user;

    public CustomerService(
        ICustomerRepository repo,
        IOrderService orders,
        IInvoiceService invoices,
        ICustomerPaymentService payments,
        ISubscriptionService subscriptions,
        IComplaintService complaints,
        IAuditService audit,
        ICurrentUser user)
    {
        _repo = repo;
        _orders = orders;
        _invoices = invoices;
        _payments = payments;
        _subscriptions = subscriptions;
        _complaints = complaints;
        _audit = audit;
        _user = user;
    }

    private bool IsCustomerUser => Roles.IsCustomerRole(_user.Role);

    /// <summary>Customer/Distributor users may only touch their own customer record.</summary>
    private void EnsureAccess(long customerId)
    {
        if (!IsCustomerUser) return;
        var own = _user.CustomerId ?? throw new ForbiddenException();
        if (own != customerId) throw new NotFoundException("Customer", customerId);
    }

    public Task<PagedResult<CustomerDto>> GetPagedAsync(CustomerQuery query, CancellationToken ct = default)
    {
        if (IsCustomerUser) query.CustomerId = _user.CustomerId ?? throw new ForbiddenException();
        return _repo.GetPagedAsync(query, ct);
    }

    public async Task<CustomerDto> GetByIdAsync(long id, CancellationToken ct = default)
    {
        EnsureAccess(id);
        return await _repo.GetByIdAsync(id, ct) ?? throw new NotFoundException("Customer", id);
    }

    public async Task<CustomerDto> CreateAsync(CreateCustomerRequest request, CancellationToken ct = default)
    {
        var code = SalesValues.Clean(request.CustomerCode)?.ToUpperInvariant();
        if (code is not null && await _repo.CodeExistsAsync(code, null, ct))
            throw new ConflictException($"A customer with code '{code}' already exists.");

        var created = await _repo.CreateAsync(code, request, request.JoinDate ?? DateTime.Today, _user.UserId, ct);
        var dto = await _repo.GetByIdAsync(created.Id, ct) ?? throw new NotFoundException("Customer", created.Id);

        await _audit.LogAsync(AuditActions.Create, "Customer", created.Id, null, dto, ct);
        return dto;
    }

    public async Task<CustomerDto> UpdateAsync(long id, UpdateCustomerRequest request, CancellationToken ct = default)
    {
        var existing = await _repo.GetByIdAsync(id, ct) ?? throw new NotFoundException("Customer", id);

        await _repo.UpdateAsync(id, request, _user.UserId, ct);
        var dto = await _repo.GetByIdAsync(id, ct) ?? throw new NotFoundException("Customer", id);

        await _audit.LogAsync(AuditActions.Update, "Customer", id, existing, dto, ct);
        return dto;
    }

    public async Task<CustomerDto> ActivateAsync(long id, CancellationToken ct = default)
    {
        var existing = await _repo.GetByIdAsync(id, ct) ?? throw new NotFoundException("Customer", id);
        if (existing.Status == "Active" && existing.IsActive) return existing;

        await _repo.SetStatusAsync(id, "Active", true, _user.UserId, ct);
        var dto = await _repo.GetByIdAsync(id, ct) ?? throw new NotFoundException("Customer", id);

        await _audit.LogAsync(AuditActions.Activate, "Customer", id, existing, dto, ct);
        return dto;
    }

    public async Task<CustomerDto> DeactivateAsync(long id, CancellationToken ct = default)
    {
        var existing = await _repo.GetByIdAsync(id, ct) ?? throw new NotFoundException("Customer", id);
        if (existing.Status == "Inactive" && !existing.IsActive) return existing;

        await _repo.SetStatusAsync(id, "Inactive", false, _user.UserId, ct);
        var dto = await _repo.GetByIdAsync(id, ct) ?? throw new NotFoundException("Customer", id);

        await _audit.LogAsync(AuditActions.Deactivate, "Customer", id, existing, dto, ct);
        return dto;
    }

    public async Task<CustomerSummaryDto> GetSummaryAsync(long id, CancellationToken ct = default)
    {
        EnsureAccess(id);
        var customer = await _repo.GetByIdAsync(id, ct) ?? throw new NotFoundException("Customer", id);
        var summary = await _repo.GetSummaryAsync(id, ct) ?? throw new NotFoundException("Customer", id);
        summary.Customer = customer;
        return summary;
    }

    // ---------- addresses

    public async Task<List<CustomerAddressDto>> GetAddressesAsync(long customerId, CancellationToken ct = default)
    {
        EnsureAccess(customerId);
        await EnsureCustomerExistsAsync(customerId, ct);
        return await _repo.GetAddressesAsync(customerId, ct);
    }

    public async Task<CustomerAddressDto> CreateAddressAsync(long customerId, CustomerAddressRequest request, CancellationToken ct = default)
    {
        EnsureAccess(customerId);
        await EnsureCustomerExistsAsync(customerId, ct);
        var id = await _repo.CreateAddressAsync(customerId, request, _user.UserId, ct);
        var dto = await _repo.GetAddressAsync(customerId, id, ct) ?? throw new NotFoundException("Customer address", id);

        await _audit.LogAsync(AuditActions.Create, "CustomerAddress", id, null, dto, ct);
        return dto;
    }

    public async Task<CustomerAddressDto> UpdateAddressAsync(long customerId, long addressId, CustomerAddressRequest request, CancellationToken ct = default)
    {
        EnsureAccess(customerId);
        var existing = await _repo.GetAddressAsync(customerId, addressId, ct) ?? throw new NotFoundException("Customer address", addressId);

        await _repo.UpdateAddressAsync(customerId, addressId, request, _user.UserId, ct);
        var dto = await _repo.GetAddressAsync(customerId, addressId, ct) ?? throw new NotFoundException("Customer address", addressId);

        await _audit.LogAsync(AuditActions.Update, "CustomerAddress", addressId, existing, dto, ct);
        return dto;
    }

    public async Task DeleteAddressAsync(long customerId, long addressId, CancellationToken ct = default)
    {
        EnsureAccess(customerId);
        var existing = await _repo.GetAddressAsync(customerId, addressId, ct) ?? throw new NotFoundException("Customer address", addressId);
        if (await _repo.AddressInUseAsync(addressId, ct))
            throw new BusinessRuleException("This address is used by an active subscription and cannot be removed.");

        await _repo.DeactivateAddressAsync(customerId, addressId, _user.UserId, ct);
        await _audit.LogAsync(AuditActions.Deactivate, "CustomerAddress", addressId, existing, null, ct);
    }

    // ---------- convenience lists (ownership checked here, filtering delegated)

    public Task<PagedResult<OrderListDto>> GetOrdersAsync(long customerId, OrderQuery query, CancellationToken ct = default)
    {
        EnsureAccess(customerId);
        query.CustomerId = customerId;
        return _orders.GetPagedAsync(query, ct);
    }

    public Task<PagedResult<InvoiceListDto>> GetInvoicesAsync(long customerId, InvoiceQuery query, CancellationToken ct = default)
    {
        EnsureAccess(customerId);
        query.CustomerId = customerId;
        return _invoices.GetPagedAsync(query, ct);
    }

    public Task<PagedResult<CustomerPaymentListDto>> GetPaymentsAsync(long customerId, CustomerPaymentQuery query, CancellationToken ct = default)
    {
        EnsureAccess(customerId);
        query.CustomerId = customerId;
        return _payments.GetPagedAsync(query, ct);
    }

    public Task<PagedResult<SubscriptionDto>> GetSubscriptionsAsync(long customerId, SubscriptionQuery query, CancellationToken ct = default)
    {
        EnsureAccess(customerId);
        query.CustomerId = customerId;
        return _subscriptions.GetPagedAsync(query, ct);
    }

    public Task<PagedResult<ComplaintDto>> GetComplaintsAsync(long customerId, ComplaintQuery query, CancellationToken ct = default)
    {
        EnsureAccess(customerId);
        query.CustomerId = customerId;
        return _complaints.GetPagedAsync(query, ct);
    }

    private async Task EnsureCustomerExistsAsync(long customerId, CancellationToken ct)
    {
        if (await _repo.GetByIdAsync(customerId, ct) is null)
            throw new NotFoundException("Customer", customerId);
    }
}
