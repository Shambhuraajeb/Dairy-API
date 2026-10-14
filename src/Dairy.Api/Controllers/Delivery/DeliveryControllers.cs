using Dairy.Api.Authorization;
using Dairy.Application.Interfaces.Delivery;
using Dairy.Application.Requests.Delivery;
using Dairy.Application.Requests.Orders;
using Dairy.Domain.Constants;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Dairy.Api.Controllers.Delivery;

[Route("api/routes")]
public class RoutesController : ApiControllerBase
{
    private readonly IRouteService _routes;

    public RoutesController(IRouteService routes) => _routes = routes;

    [HttpGet]
    [HasPermission(Permissions.RouteView)]
    public async Task<IActionResult> List([FromQuery] RouteQuery query, CancellationToken ct) =>
        Paged(await _routes.GetPagedAsync(query, ct));

    [HttpGet("{id:long}")]
    [HasPermission(Permissions.RouteView)]
    public async Task<IActionResult> Get(long id, CancellationToken ct) =>
        Ok(await _routes.GetByIdAsync(id, ct));

    [HttpPost]
    [HasPermission(Permissions.RouteManage)]
    public async Task<IActionResult> Create([FromBody] CreateRouteRequest request, CancellationToken ct) =>
        Created(await _routes.CreateAsync(request, ct), "Route created.");

    [HttpPut("{id:long}")]
    [HasPermission(Permissions.RouteManage)]
    public async Task<IActionResult> Update(long id, [FromBody] UpdateRouteRequest request, CancellationToken ct) =>
        Ok(await _routes.UpdateAsync(id, request, ct), "Route updated.");

    /// <summary>Soft delete (deactivates the route).</summary>
    [HttpDelete("{id:long}")]
    [HasPermission(Permissions.RouteManage)]
    public async Task<IActionResult> Delete(long id, CancellationToken ct)
    {
        await _routes.DeleteAsync(id, ct);
        return Message("Route deactivated.");
    }
}

[Route("api/vehicles")]
public class VehiclesController : ApiControllerBase
{
    private readonly IVehicleService _vehicles;

    public VehiclesController(IVehicleService vehicles) => _vehicles = vehicles;

    [HttpGet]
    [HasPermission(Permissions.RouteView)]
    public async Task<IActionResult> List([FromQuery] VehicleQuery query, CancellationToken ct) =>
        Paged(await _vehicles.GetPagedAsync(query, ct));

    [HttpGet("{id:long}")]
    [HasPermission(Permissions.RouteView)]
    public async Task<IActionResult> Get(long id, CancellationToken ct) =>
        Ok(await _vehicles.GetByIdAsync(id, ct));

    [HttpPost]
    [HasPermission(Permissions.RouteManage)]
    public async Task<IActionResult> Create([FromBody] CreateVehicleRequest request, CancellationToken ct) =>
        Created(await _vehicles.CreateAsync(request, ct), "Vehicle added.");

    [HttpPut("{id:long}")]
    [HasPermission(Permissions.RouteManage)]
    public async Task<IActionResult> Update(long id, [FromBody] UpdateVehicleRequest request, CancellationToken ct) =>
        Ok(await _vehicles.UpdateAsync(id, request, ct), "Vehicle updated.");

    /// <summary>Soft delete (marks the vehicle inactive).</summary>
    [HttpDelete("{id:long}")]
    [HasPermission(Permissions.RouteManage)]
    public async Task<IActionResult> Delete(long id, CancellationToken ct)
    {
        await _vehicles.DeleteAsync(id, ct);
        return Message("Vehicle deactivated.");
    }
}

[Route("api/delivery-schedules")]
public class DeliverySchedulesController : ApiControllerBase
{
    private readonly IDeliveryScheduleService _schedules;

    public DeliverySchedulesController(IDeliveryScheduleService schedules) => _schedules = schedules;

    [HttpGet]
    [HasPermission(Permissions.RouteView)]
    public async Task<IActionResult> List([FromQuery] DeliveryScheduleQuery query, CancellationToken ct) =>
        Paged(await _schedules.GetPagedAsync(query, ct));

    [HttpGet("{id:long}")]
    [HasPermission(Permissions.RouteView)]
    public async Task<IActionResult> Get(long id, CancellationToken ct) =>
        Ok(await _schedules.GetByIdAsync(id, ct));

    [HttpPost]
    [HasPermission(Permissions.RouteManage)]
    public async Task<IActionResult> Create([FromBody] CreateDeliveryScheduleRequest request, CancellationToken ct) =>
        Created(await _schedules.CreateAsync(request, ct), "Delivery schedule created.");

    [HttpPut("{id:long}")]
    [HasPermission(Permissions.RouteManage)]
    public async Task<IActionResult> Update(long id, [FromBody] UpdateDeliveryScheduleRequest request, CancellationToken ct) =>
        Ok(await _schedules.UpdateAsync(id, request, ct), "Delivery schedule updated.");

    /// <summary>Soft delete (only schedules without live deliveries).</summary>
    [HttpDelete("{id:long}")]
    [HasPermission(Permissions.RouteManage)]
    public async Task<IActionResult> Delete(long id, CancellationToken ct)
    {
        await _schedules.DeleteAsync(id, ct);
        return Message("Delivery schedule removed.");
    }
}

[Route("api/deliveries")]
public class DeliveriesController : ApiControllerBase
{
    private readonly IDeliveryService _deliveries;

    public DeliveriesController(IDeliveryService deliveries) => _deliveries = deliveries;

    [HttpGet]
    [HasPermission(Permissions.DeliveryView)]
    public async Task<IActionResult> List([FromQuery] DeliveryQuery query, CancellationToken ct) =>
        Paged(await _deliveries.GetPagedAsync(query, ct));

    /// <summary>Driver view: my deliveries for a date (default today), ordered by slot and stop sequence.</summary>
    [HttpGet("my")]
    [HasPermission(Permissions.DeliveryView)]
    public async Task<IActionResult> My([FromQuery] DateTime? date, CancellationToken ct) =>
        Ok(await _deliveries.GetMyAsync(date, ct));

    /// <summary>Orders awaiting scheduling (Pending/Confirmed/Failed without a live delivery); filters from/to/slot/routeId on the delivery date.</summary>
    [HttpGet("pending-orders")]
    [HasPermission(Permissions.DeliveryView)]
    public async Task<IActionResult> PendingOrders([FromQuery] OrderQuery query, CancellationToken ct) =>
        Paged(await _deliveries.GetPendingOrdersAsync(query, ct));

    [HttpGet("{id:long}")]
    [HasPermission(Permissions.DeliveryView)]
    public async Task<IActionResult> Get(long id, CancellationToken ct) =>
        Ok(await _deliveries.GetByIdAsync(id, ct));

    [HttpPost]
    [HasPermission(Permissions.DeliveryUpdate)]
    public async Task<IActionResult> Create([FromBody] CreateDeliveryRequest request, CancellationToken ct) =>
        Created(await _deliveries.CreateAsync(request, ct), "Delivery created.");

    /// <summary>Assigns many orders to one schedule; per-order failures are reported in the result, not as an error.</summary>
    [HttpPost("bulk")]
    [HasPermission(Permissions.DeliveryUpdate)]
    public async Task<IActionResult> Bulk([FromBody] BulkDeliveryRequest request, CancellationToken ct)
    {
        var result = await _deliveries.CreateBulkAsync(request, ct);
        return Ok(result, $"{result.CreatedCount} of {result.RequestedCount} order(s) scheduled.");
    }

    [HttpPost("{id:long}/dispatch")]
    [HasPermission(Permissions.DeliveryUpdate)]
    public async Task<IActionResult> Dispatch(long id, CancellationToken ct) =>
        Ok(await _deliveries.DispatchAsync(id, ct), "Delivery dispatched.");

    /// <summary>Body optional (empty = delivered). Managers confirm any delivery; a driver only their assigned ones.</summary>
    [HttpPost("{id:long}/confirm")]
    [HasPermission(Permissions.DeliveryConfirm)]
    public async Task<IActionResult> Confirm(
        long id,
        [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] ConfirmDeliveryRequest? request,
        CancellationToken ct) =>
        Ok(await _deliveries.ConfirmAsync(id, request, ct), "Delivery confirmation recorded.");
}
