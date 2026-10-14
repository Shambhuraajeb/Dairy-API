using Dairy.Api.Authorization;
using Dairy.Application.Interfaces.Inventory;
using Dairy.Application.Requests.Inventory;
using Dairy.Domain.Constants;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Dairy.Api.Controllers.Inventory;

[Route("api/inventory/purchases")]
public class PurchasesController : ApiControllerBase
{
    private readonly IPurchaseService _purchases;

    public PurchasesController(IPurchaseService purchases) => _purchases = purchases;

    [HttpGet]
    [HasPermission(Permissions.PurchaseView)]
    public async Task<IActionResult> List([FromQuery] PurchaseQuery query, CancellationToken ct) =>
        Paged(await _purchases.GetPagedAsync(query, ct));

    [HttpGet("{id:long}")]
    [HasPermission(Permissions.PurchaseView)]
    public async Task<IActionResult> Get(long id, CancellationToken ct) =>
        Ok(await _purchases.GetByIdAsync(id, ct));

    [HttpPost]
    [HasPermission(Permissions.PurchaseCreate)]
    public async Task<IActionResult> Create([FromBody] CreatePurchaseRequest request, CancellationToken ct) =>
        Created(await _purchases.CreateAsync(request, ct), "Purchase created.");

    [HttpPost("{id:long}/approve")]
    [HasPermission(Permissions.PurchaseApprove)]
    public async Task<IActionResult> Approve(long id, CancellationToken ct) =>
        Ok(await _purchases.ApproveAsync(id, ct), "Purchase approved.");

    /// <summary>Receives all pending lines: creates batches, increases stock, writes the stock ledger. Body optional.</summary>
    [HttpPost("{id:long}/receive")]
    [HasPermission(Permissions.PurchaseReceive)]
    public async Task<IActionResult> Receive(long id, [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] ReceivePurchaseRequest? request, CancellationToken ct) =>
        Ok(await _purchases.ReceiveAsync(id, request, ct), "Purchase received and stock updated.");

    [HttpPost("{id:long}/cancel")]
    [HasPermission(Permissions.PurchaseApprove, Permissions.PurchaseCreate)]
    public async Task<IActionResult> Cancel(long id, [FromBody] CancelPurchaseRequest request, CancellationToken ct) =>
        Ok(await _purchases.CancelAsync(id, request, ct), "Purchase cancelled.");
}
