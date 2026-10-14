using Dairy.Api.Authorization;
using Dairy.Application.Interfaces.Settlement;
using Dairy.Application.Requests.Settlement;
using Dairy.Domain.Constants;
using Microsoft.AspNetCore.Mvc;

namespace Dairy.Api.Controllers.Settlement;

[Route("api/farmer-payments")]
public class FarmerPaymentsController : ApiControllerBase
{
    private readonly IFarmerSettlementService _settlements;

    public FarmerPaymentsController(IFarmerSettlementService settlements) => _settlements = settlements;

    [HttpGet]
    [HasPermission(Permissions.PaymentView)]
    public async Task<IActionResult> List([FromQuery] FarmerPaymentQuery query, CancellationToken ct) =>
        Paged(await _settlements.GetPaymentsAsync(query, ct));

    [HttpGet("{id:long}")]
    [HasPermission(Permissions.PaymentView)]
    public async Task<IActionResult> Get(long id, CancellationToken ct) =>
        Ok(await _settlements.GetPaymentByIdAsync(id, ct));
}
