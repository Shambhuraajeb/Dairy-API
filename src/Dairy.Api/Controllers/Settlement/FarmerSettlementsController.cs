using Dairy.Api.Authorization;
using Dairy.Application.Interfaces.Settlement;
using Dairy.Application.Requests.Settlement;
using Dairy.Domain.Constants;
using Microsoft.AspNetCore.Mvc;

namespace Dairy.Api.Controllers.Settlement;

[Route("api/farmer-settlements")]
public class FarmerSettlementsController : ApiControllerBase
{
    private readonly IFarmerSettlementService _settlements;

    public FarmerSettlementsController(IFarmerSettlementService settlements) => _settlements = settlements;

    [HttpGet("preview")]
    [HasPermission(Permissions.PaymentView)]
    public async Task<IActionResult> Preview([FromQuery] SettlementPreviewQuery query, CancellationToken ct) =>
        Ok(await _settlements.PreviewAsync(query, ct));

    [HttpGet("pending-farmers")]
    [HasPermission(Permissions.PaymentView)]
    public async Task<IActionResult> PendingFarmers([FromQuery] PendingFarmersQuery query, CancellationToken ct) =>
        Ok(await _settlements.GetPendingFarmersAsync(query, ct));

    [HttpPost]
    [HasPermission(Permissions.PaymentCreate)]
    public async Task<IActionResult> Create([FromBody] CreateSettlementRequest request, CancellationToken ct) =>
        Created(await _settlements.CreateAsync(request, ct), "Settlement created.");

    [HttpPost("{id:long}/approve")]
    [HasPermission(Permissions.PaymentApprove)]
    public async Task<IActionResult> Approve(long id, CancellationToken ct) =>
        Ok(await _settlements.ApproveAsync(id, ct), "Settlement approved.");

    [HttpPost("{id:long}/pay")]
    [HasPermission(Permissions.PaymentCreate)]
    public async Task<IActionResult> Pay(long id, [FromBody] PaySettlementRequest request, CancellationToken ct) =>
        Ok(await _settlements.PayAsync(id, request, ct), "Payment recorded.");

    [HttpPost("{id:long}/cancel")]
    [HasPermission(Permissions.PaymentApprove)]
    public async Task<IActionResult> Cancel(long id, [FromBody] CancelSettlementRequest request, CancellationToken ct) =>
        Ok(await _settlements.CancelAsync(id, request, ct), "Settlement cancelled.");
}
