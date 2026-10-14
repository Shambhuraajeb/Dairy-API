using Dairy.Api.Authorization;
using Dairy.Application.Interfaces.Customers;
using Dairy.Application.Requests.Billing;
using Dairy.Application.Requests.Customers;
using Dairy.Application.Requests.Orders;
using Dairy.Application.Requests.Subscriptions;
using Dairy.Domain.Constants;
using Microsoft.AspNetCore.Mvc;

namespace Dairy.Api.Controllers.Customers;

[Route("api/customers")]
public class CustomersController : ApiControllerBase
{
    private readonly ICustomerService _customers;

    public CustomersController(ICustomerService customers) => _customers = customers;

    [HttpGet]
    [HasPermission(Permissions.CustomerView)]
    public async Task<IActionResult> List([FromQuery] CustomerQuery query, CancellationToken ct) =>
        Paged(await _customers.GetPagedAsync(query, ct));

    [HttpGet("{id:long}")]
    [HasPermission(Permissions.CustomerView)]
    public async Task<IActionResult> Get(long id, CancellationToken ct) =>
        Ok(await _customers.GetByIdAsync(id, ct));

    [HttpPost]
    [HasPermission(Permissions.CustomerCreate)]
    public async Task<IActionResult> Create([FromBody] CreateCustomerRequest request, CancellationToken ct) =>
        Created(await _customers.CreateAsync(request, ct), "Customer created.");

    [HttpPut("{id:long}")]
    [HasPermission(Permissions.CustomerUpdate)]
    public async Task<IActionResult> Update(long id, [FromBody] UpdateCustomerRequest request, CancellationToken ct) =>
        Ok(await _customers.UpdateAsync(id, request, ct), "Customer updated.");

    [HttpPost("{id:long}/activate")]
    [HasPermission(Permissions.CustomerUpdate)]
    public async Task<IActionResult> Activate(long id, CancellationToken ct) =>
        Ok(await _customers.ActivateAsync(id, ct), "Customer activated.");

    [HttpPost("{id:long}/deactivate")]
    [HasPermission(Permissions.CustomerUpdate)]
    public async Task<IActionResult> Deactivate(long id, CancellationToken ct) =>
        Ok(await _customers.DeactivateAsync(id, ct), "Customer deactivated.");

    [HttpGet("{id:long}/summary")]
    [HasPermission(Permissions.CustomerView)]
    public async Task<IActionResult> Summary(long id, CancellationToken ct) =>
        Ok(await _customers.GetSummaryAsync(id, ct));

    // ---- addresses

    [HttpGet("{id:long}/addresses")]
    [HasPermission(Permissions.CustomerView)]
    public async Task<IActionResult> Addresses(long id, CancellationToken ct) =>
        Ok(await _customers.GetAddressesAsync(id, ct));

    [HttpPost("{id:long}/addresses")]
    [HasPermission(Permissions.CustomerCreate, Permissions.CustomerUpdate)]
    public async Task<IActionResult> CreateAddress(long id, [FromBody] CustomerAddressRequest request, CancellationToken ct) =>
        Created(await _customers.CreateAddressAsync(id, request, ct), "Address added.");

    [HttpPut("{id:long}/addresses/{addressId:long}")]
    [HasPermission(Permissions.CustomerUpdate)]
    public async Task<IActionResult> UpdateAddress(long id, long addressId, [FromBody] CustomerAddressRequest request, CancellationToken ct) =>
        Ok(await _customers.UpdateAddressAsync(id, addressId, request, ct), "Address updated.");

    [HttpDelete("{id:long}/addresses/{addressId:long}")]
    [HasPermission(Permissions.CustomerUpdate)]
    public async Task<IActionResult> DeleteAddress(long id, long addressId, CancellationToken ct)
    {
        await _customers.DeleteAddressAsync(id, addressId, ct);
        return Message("Address removed.");
    }

    // ---- convenience lists (detail-page tabs)

    [HttpGet("{id:long}/orders")]
    [HasPermission(Permissions.OrderView)]
    public async Task<IActionResult> Orders(long id, [FromQuery] OrderQuery query, CancellationToken ct) =>
        Paged(await _customers.GetOrdersAsync(id, query, ct));

    [HttpGet("{id:long}/invoices")]
    [HasPermission(Permissions.InvoiceView)]
    public async Task<IActionResult> Invoices(long id, [FromQuery] InvoiceQuery query, CancellationToken ct) =>
        Paged(await _customers.GetInvoicesAsync(id, query, ct));

    [HttpGet("{id:long}/payments")]
    [HasPermission(Permissions.CustpaymentView)]
    public async Task<IActionResult> Payments(long id, [FromQuery] CustomerPaymentQuery query, CancellationToken ct) =>
        Paged(await _customers.GetPaymentsAsync(id, query, ct));

    [HttpGet("{id:long}/subscriptions")]
    [HasPermission(Permissions.SubscriptionView)]
    public async Task<IActionResult> Subscriptions(long id, [FromQuery] SubscriptionQuery query, CancellationToken ct) =>
        Paged(await _customers.GetSubscriptionsAsync(id, query, ct));

    [HttpGet("{id:long}/complaints")]
    [HasPermission(Permissions.ComplaintView)]
    public async Task<IActionResult> Complaints(long id, [FromQuery] ComplaintQuery query, CancellationToken ct) =>
        Paged(await _customers.GetComplaintsAsync(id, query, ct));
}
