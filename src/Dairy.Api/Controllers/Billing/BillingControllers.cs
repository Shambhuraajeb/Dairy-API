using Dairy.Api.Authorization;
using Dairy.Application.Interfaces.Billing;
using Dairy.Application.Requests.Billing;
using Dairy.Domain.Constants;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Dairy.Api.Controllers.Billing;

[Route("api/invoices")]
public class InvoicesController : ApiControllerBase
{
    private readonly IInvoiceService _invoices;

    public InvoicesController(IInvoiceService invoices) => _invoices = invoices;

    [HttpGet]
    [HasPermission(Permissions.InvoiceView)]
    public async Task<IActionResult> List([FromQuery] InvoiceQuery query, CancellationToken ct) =>
        Paged(await _invoices.GetPagedAsync(query, ct));

    [HttpGet("{id:long}")]
    [HasPermission(Permissions.InvoiceView)]
    public async Task<IActionResult> Get(long id, CancellationToken ct) =>
        Ok(await _invoices.GetByIdAsync(id, ct));

    /// <summary>Invoices the customer's delivered, un-invoiced orders for a period (or an explicit order list).</summary>
    [HttpPost]
    [HasPermission(Permissions.InvoiceCreate)]
    public async Task<IActionResult> Generate([FromBody] GenerateInvoiceRequest request, CancellationToken ct) =>
        Created(await _invoices.GenerateAsync(request, ct), "Invoice generated.");

    /// <summary>Cancels an issued invoice that has no payments; body optional.</summary>
    [HttpPost("{id:long}/cancel")]
    [HasPermission(Permissions.InvoiceCreate)]
    public async Task<IActionResult> Cancel(
        long id,
        [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] CancelInvoiceRequest? request,
        CancellationToken ct) =>
        Ok(await _invoices.CancelAsync(id, request ?? new CancelInvoiceRequest(), ct), "Invoice cancelled.");
}

[Route("api/payments")]
public class PaymentsController : ApiControllerBase
{
    private readonly ICustomerPaymentService _payments;

    public PaymentsController(ICustomerPaymentService payments) => _payments = payments;

    [HttpGet]
    [HasPermission(Permissions.CustpaymentView)]
    public async Task<IActionResult> List([FromQuery] CustomerPaymentQuery query, CancellationToken ct) =>
        Paged(await _payments.GetPagedAsync(query, ct));

    [HttpGet("{id:long}")]
    [HasPermission(Permissions.CustpaymentView)]
    public async Task<IActionResult> Get(long id, CancellationToken ct) =>
        Ok(await _payments.GetByIdAsync(id, ct));

    /// <summary>Records a customer payment and allocates it (oldest open invoices first, or the given invoice).</summary>
    [HttpPost]
    [HasPermission(Permissions.CustpaymentCreate)]
    public async Task<IActionResult> Record([FromBody] RecordCustomerPaymentRequest request, CancellationToken ct) =>
        Created(await _payments.RecordAsync(request, ct), "Payment recorded.");
}

[Route("api/billing")]
public class BillingController : ApiControllerBase
{
    private readonly IBillingService _billing;

    public BillingController(IBillingService billing) => _billing = billing;

    /// <summary>Aging of open invoices per customer (optional customerId; customers always get their own).</summary>
    [HttpGet("outstanding")]
    [HasPermission(Permissions.InvoiceView, Permissions.CustpaymentView)]
    public async Task<IActionResult> Outstanding([FromQuery] long? customerId, CancellationToken ct) =>
        Ok(await _billing.GetOutstandingAsync(customerId, ct));

    [HttpGet("summary")]
    [HasPermission(Permissions.InvoiceView, Permissions.CustpaymentView)]
    public async Task<IActionResult> Summary(CancellationToken ct) =>
        Ok(await _billing.GetSummaryAsync(ct));
}
