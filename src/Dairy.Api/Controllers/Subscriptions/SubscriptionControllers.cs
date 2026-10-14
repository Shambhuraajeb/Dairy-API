using Dairy.Api.Authorization;
using Dairy.Application.Interfaces.Subscriptions;
using Dairy.Application.Requests.Subscriptions;
using Dairy.Domain.Constants;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Dairy.Api.Controllers.Subscriptions;

[Route("api/subscription-plans")]
public class SubscriptionPlansController : ApiControllerBase
{
    private readonly ISubscriptionPlanService _plans;

    public SubscriptionPlansController(ISubscriptionPlanService plans) => _plans = plans;

    [HttpGet]
    [HasPermission(Permissions.SubscriptionView)]
    public async Task<IActionResult> List([FromQuery] SubscriptionPlanQuery query, CancellationToken ct) =>
        Paged(await _plans.GetPagedAsync(query, ct));

    [HttpGet("{id:long}")]
    [HasPermission(Permissions.SubscriptionView)]
    public async Task<IActionResult> Get(long id, CancellationToken ct) =>
        Ok(await _plans.GetByIdAsync(id, ct));

    [HttpPost]
    [HasPermission(Permissions.SubscriptionCreate)]
    public async Task<IActionResult> Create([FromBody] CreateSubscriptionPlanRequest request, CancellationToken ct) =>
        Created(await _plans.CreateAsync(request, ct), "Subscription plan created.");

    [HttpPut("{id:long}")]
    [HasPermission(Permissions.SubscriptionUpdate)]
    public async Task<IActionResult> Update(long id, [FromBody] UpdateSubscriptionPlanRequest request, CancellationToken ct) =>
        Ok(await _plans.UpdateAsync(id, request, ct), "Subscription plan updated.");

    /// <summary>Soft delete (deactivates the plan).</summary>
    [HttpDelete("{id:long}")]
    [HasPermission(Permissions.SubscriptionUpdate)]
    public async Task<IActionResult> Delete(long id, CancellationToken ct)
    {
        await _plans.DeleteAsync(id, ct);
        return Message("Subscription plan deactivated.");
    }
}

[Route("api/subscriptions")]
public class SubscriptionsController : ApiControllerBase
{
    private readonly ISubscriptionService _subscriptions;

    public SubscriptionsController(ISubscriptionService subscriptions) => _subscriptions = subscriptions;

    [HttpGet]
    [HasPermission(Permissions.SubscriptionView)]
    public async Task<IActionResult> List([FromQuery] SubscriptionQuery query, CancellationToken ct) =>
        Paged(await _subscriptions.GetPagedAsync(query, ct));

    [HttpGet("{id:long}")]
    [HasPermission(Permissions.SubscriptionView)]
    public async Task<IActionResult> Get(long id, CancellationToken ct) =>
        Ok(await _subscriptions.GetByIdAsync(id, ct));

    [HttpPost]
    [HasPermission(Permissions.SubscriptionCreate)]
    public async Task<IActionResult> Create([FromBody] CreateSubscriptionRequest request, CancellationToken ct) =>
        Created(await _subscriptions.CreateAsync(request, ct), "Subscription created.");

    [HttpPut("{id:long}")]
    [HasPermission(Permissions.SubscriptionUpdate)]
    public async Task<IActionResult> Update(long id, [FromBody] UpdateSubscriptionRequest request, CancellationToken ct) =>
        Ok(await _subscriptions.UpdateAsync(id, request, ct), "Subscription updated.");

    [HttpPost("{id:long}/pause")]
    [HasPermission(Permissions.SubscriptionUpdate)]
    public async Task<IActionResult> Pause(
        long id,
        [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] PauseSubscriptionRequest? request,
        CancellationToken ct) =>
        Ok(await _subscriptions.PauseAsync(id, request ?? new PauseSubscriptionRequest(), ct), "Subscription paused.");

    [HttpPost("{id:long}/resume")]
    [HasPermission(Permissions.SubscriptionUpdate)]
    public async Task<IActionResult> Resume(long id, CancellationToken ct) =>
        Ok(await _subscriptions.ResumeAsync(id, ct), "Subscription resumed.");

    [HttpPost("{id:long}/cancel")]
    [HasPermission(Permissions.SubscriptionUpdate)]
    public async Task<IActionResult> Cancel(
        long id,
        [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] CancelSubscriptionRequest? request,
        CancellationToken ct) =>
        Ok(await _subscriptions.CancelAsync(id, request ?? new CancelSubscriptionRequest(), ct), "Subscription cancelled.");

    /// <summary>Generates delivery orders for the given date from all due subscriptions (body optional: forDate defaults to today).</summary>
    [HttpPost("generate-orders")]
    [HasPermission(Permissions.SubscriptionUpdate)]
    public async Task<IActionResult> GenerateOrders(
        [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] GenerateSubscriptionOrdersRequest? request,
        CancellationToken ct)
    {
        var result = await _subscriptions.GenerateOrdersAsync(request ?? new GenerateSubscriptionOrdersRequest(), ct);
        return Ok(result, $"{result.OrdersCreated} order(s) generated.");
    }
}
