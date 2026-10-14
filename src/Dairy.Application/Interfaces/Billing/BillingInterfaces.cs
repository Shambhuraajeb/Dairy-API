using Dairy.Application.Common;
using Dairy.Application.DTOs.Billing;
using Dairy.Application.Requests.Billing;

namespace Dairy.Application.Interfaces.Billing;

public interface IInvoiceRepository
{
    Task<PagedResult<InvoiceListDto>> GetPagedAsync(InvoiceQuery query, CancellationToken ct = default);
    Task<InvoiceDetailDto?> GetByIdAsync(long id, CancellationToken ct = default);
    Task<CreatedResult> GenerateAsync(GenerateInvoiceRequest request, long userId, CancellationToken ct = default);
    Task<int> CountAllocationsAsync(long id, CancellationToken ct = default);

    /// <summary>Cancels an Issued invoice that has no payments, un-links its orders/deliveries and reverses the customer balance. False when the state no longer allows it.</summary>
    Task<bool> CancelAsync(long id, string? reason, long userId, CancellationToken ct = default);
}

public interface IInvoiceService
{
    Task<PagedResult<InvoiceListDto>> GetPagedAsync(InvoiceQuery query, CancellationToken ct = default);
    Task<InvoiceDetailDto> GetByIdAsync(long id, CancellationToken ct = default);
    Task<InvoiceDetailDto> GenerateAsync(GenerateInvoiceRequest request, CancellationToken ct = default);
    Task<InvoiceDetailDto> CancelAsync(long id, CancelInvoiceRequest request, CancellationToken ct = default);
}

public interface ICustomerPaymentRepository
{
    Task<PagedResult<CustomerPaymentListDto>> GetPagedAsync(CustomerPaymentQuery query, CancellationToken ct = default);
    Task<CustomerPaymentDetailDto?> GetByIdAsync(long id, CancellationToken ct = default);
    Task<CreatedResult> RecordAsync(RecordCustomerPaymentRequest request, long userId, CancellationToken ct = default);
}

public interface ICustomerPaymentService
{
    Task<PagedResult<CustomerPaymentListDto>> GetPagedAsync(CustomerPaymentQuery query, CancellationToken ct = default);
    Task<CustomerPaymentDetailDto> GetByIdAsync(long id, CancellationToken ct = default);
    Task<CustomerPaymentDetailDto> RecordAsync(RecordCustomerPaymentRequest request, CancellationToken ct = default);
}

public interface IBillingRepository
{
    Task<List<CustomerOutstandingDto>> GetOutstandingAsync(long? customerId, CancellationToken ct = default);
    Task<BillingSummaryDto> GetSummaryAsync(long? customerId, CancellationToken ct = default);
}

public interface IBillingService
{
    Task<List<CustomerOutstandingDto>> GetOutstandingAsync(long? customerId, CancellationToken ct = default);
    Task<BillingSummaryDto> GetSummaryAsync(CancellationToken ct = default);
}
