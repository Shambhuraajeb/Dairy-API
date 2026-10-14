using Dairy.Application.Common;
using Dairy.Application.DTOs.Billing;
using Dairy.Application.DTOs.Customers;
using Dairy.Application.DTOs.Orders;
using Dairy.Application.DTOs.Subscriptions;
using Dairy.Application.Requests.Billing;
using Dairy.Application.Requests.Customers;
using Dairy.Application.Requests.Orders;
using Dairy.Application.Requests.Subscriptions;

namespace Dairy.Application.Interfaces.Customers;

public interface ICustomerRepository
{
    Task<PagedResult<CustomerDto>> GetPagedAsync(CustomerQuery query, CancellationToken ct = default);
    Task<CustomerDto?> GetByIdAsync(long id, CancellationToken ct = default);
    Task<bool> CodeExistsAsync(string code, long? excludeId, CancellationToken ct = default);
    Task<CreatedResult> CreateAsync(string? code, CreateCustomerRequest request, DateTime joinDate, long userId, CancellationToken ct = default);
    Task UpdateAsync(long id, UpdateCustomerRequest request, long userId, CancellationToken ct = default);
    Task SetStatusAsync(long id, string status, bool isActive, long userId, CancellationToken ct = default);
    Task<CustomerSummaryDto?> GetSummaryAsync(long id, CancellationToken ct = default);

    Task<List<CustomerAddressDto>> GetAddressesAsync(long customerId, CancellationToken ct = default);
    Task<CustomerAddressDto?> GetAddressAsync(long customerId, long addressId, CancellationToken ct = default);
    Task<long> CreateAddressAsync(long customerId, CustomerAddressRequest request, long userId, CancellationToken ct = default);
    Task UpdateAddressAsync(long customerId, long addressId, CustomerAddressRequest request, long userId, CancellationToken ct = default);
    Task<bool> AddressInUseAsync(long addressId, CancellationToken ct = default);
    Task DeactivateAddressAsync(long customerId, long addressId, long userId, CancellationToken ct = default);
}

public interface ICustomerService
{
    Task<PagedResult<CustomerDto>> GetPagedAsync(CustomerQuery query, CancellationToken ct = default);
    Task<CustomerDto> GetByIdAsync(long id, CancellationToken ct = default);
    Task<CustomerDto> CreateAsync(CreateCustomerRequest request, CancellationToken ct = default);
    Task<CustomerDto> UpdateAsync(long id, UpdateCustomerRequest request, CancellationToken ct = default);
    Task<CustomerDto> ActivateAsync(long id, CancellationToken ct = default);
    Task<CustomerDto> DeactivateAsync(long id, CancellationToken ct = default);
    Task<CustomerSummaryDto> GetSummaryAsync(long id, CancellationToken ct = default);

    Task<List<CustomerAddressDto>> GetAddressesAsync(long customerId, CancellationToken ct = default);
    Task<CustomerAddressDto> CreateAddressAsync(long customerId, CustomerAddressRequest request, CancellationToken ct = default);
    Task<CustomerAddressDto> UpdateAddressAsync(long customerId, long addressId, CustomerAddressRequest request, CancellationToken ct = default);
    Task DeleteAddressAsync(long customerId, long addressId, CancellationToken ct = default);

    Task<PagedResult<OrderListDto>> GetOrdersAsync(long customerId, OrderQuery query, CancellationToken ct = default);
    Task<PagedResult<InvoiceListDto>> GetInvoicesAsync(long customerId, InvoiceQuery query, CancellationToken ct = default);
    Task<PagedResult<CustomerPaymentListDto>> GetPaymentsAsync(long customerId, CustomerPaymentQuery query, CancellationToken ct = default);
    Task<PagedResult<SubscriptionDto>> GetSubscriptionsAsync(long customerId, SubscriptionQuery query, CancellationToken ct = default);
    Task<PagedResult<ComplaintDto>> GetComplaintsAsync(long customerId, ComplaintQuery query, CancellationToken ct = default);
}

public interface IComplaintRepository
{
    Task<PagedResult<ComplaintDto>> GetPagedAsync(ComplaintQuery query, CancellationToken ct = default);
    Task<ComplaintDto?> GetByIdAsync(long id, CancellationToken ct = default);
    Task<bool> CustomerExistsAsync(long customerId, CancellationToken ct = default);
    Task<bool> OrderBelongsToCustomerAsync(long salesOrderId, long customerId, CancellationToken ct = default);
    Task<bool> DeliveryBelongsToCustomerAsync(long deliveryId, long customerId, CancellationToken ct = default);
    Task<bool> UserExistsAsync(long userId, CancellationToken ct = default);
    Task<CreatedResult> CreateAsync(long customerId, CreateComplaintRequest request, long userId, CancellationToken ct = default);
    Task AssignAsync(long id, long assignedTo, long userId, CancellationToken ct = default);
    Task SetStatusAsync(long id, string status, long? assignedTo, long userId, CancellationToken ct = default);
    Task ResolveAsync(long id, string resolution, long userId, CancellationToken ct = default);
    Task CloseAsync(long id, long userId, CancellationToken ct = default);
    Task SaveFeedbackAsync(long id, int rating, string? comments, long userId, CancellationToken ct = default);
    Task AddNotificationAsync(long userId, string title, string message, string severity, string? link, CancellationToken ct = default);
}

public interface IComplaintService
{
    Task<PagedResult<ComplaintDto>> GetPagedAsync(ComplaintQuery query, CancellationToken ct = default);
    Task<ComplaintDto> GetByIdAsync(long id, CancellationToken ct = default);
    Task<ComplaintDto> CreateAsync(CreateComplaintRequest request, CancellationToken ct = default);
    Task<ComplaintDto> AssignAsync(long id, AssignComplaintRequest request, CancellationToken ct = default);
    Task<ComplaintDto> SetStatusAsync(long id, ComplaintStatusRequest request, CancellationToken ct = default);
    Task<ComplaintDto> ResolveAsync(long id, ResolveComplaintRequest request, CancellationToken ct = default);
    Task<ComplaintDto> CloseAsync(long id, CancellationToken ct = default);
    Task<ComplaintDto> FeedbackAsync(long id, ComplaintFeedbackRequest request, CancellationToken ct = default);
}
