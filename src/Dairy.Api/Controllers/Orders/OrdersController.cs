using Dairy.Api.Authorization;
using Dairy.Application.Interfaces.Orders;
using Dairy.Application.Requests.Orders;
using Dairy.Domain.Constants;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Dairy.Api.Controllers.Orders;

[Route("api/orders")]
public class OrdersController : ApiControllerBase
{
    private readonly IOrderService _orders;

    public OrdersController(IOrderService orders) => _orders = orders;

    [HttpGet]
    [HasPermission(Permissions.OrderView)]
    public async Task<IActionResult> List([FromQuery] OrderQuery query, CancellationToken ct) =>
        Paged(await _orders.GetPagedAsync(query, ct));

    [HttpGet("{id:long}")]
    [HasPermission(Permissions.OrderView)]
    public async Task<IActionResult> Get(long id, CancellationToken ct) =>
        Ok(await _orders.GetByIdAsync(id, ct));

    /// <summary>Customers order for themselves (customerId ignored); staff pass customerId.</summary>
    [HttpPost]
    [HasPermission(Permissions.OrderCreate)]
    public async Task<IActionResult> Create([FromBody] CreateOrderRequest request, CancellationToken ct) =>
        Created(await _orders.CreateAsync(request, ct), "Order placed.");

    [HttpPut("{id:long}")]
    [HasPermission(Permissions.OrderUpdate)]
    public async Task<IActionResult> Update(long id, [FromBody] UpdateOrderRequest request, CancellationToken ct) =>
        Ok(await _orders.UpdateAsync(id, request, ct), "Order updated.");

    /// <summary>Staff (order.update) or the owning customer (order.create); body optional.</summary>
    [HttpPost("{id:long}/cancel")]
    [HasPermission(Permissions.OrderUpdate, Permissions.OrderCreate)]
    public async Task<IActionResult> Cancel(
        long id,
        [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] CancelOrderRequest? request,
        CancellationToken ct) =>
        Ok(await _orders.CancelAsync(id, request ?? new CancelOrderRequest(), ct), "Order cancelled.");
}
