using Dairy.Api.Authorization;
using Dairy.Application.Interfaces.Finance;
using Dairy.Application.Requests.Finance;
using Dairy.Domain.Constants;
using Microsoft.AspNetCore.Mvc;

namespace Dairy.Api.Controllers.Finance;

[Route("api/cash-transactions")]
public class CashTransactionsController : ApiControllerBase
{
    private readonly ICashTransactionService _cash;

    public CashTransactionsController(ICashTransactionService cash) => _cash = cash;

    [HttpGet]
    [HasPermission(Permissions.CashView)]
    public async Task<IActionResult> List([FromQuery] CashTransactionQuery query, CancellationToken ct) =>
        Paged(await _cash.GetPagedAsync(query, ct));

    /// <summary>Opening balance, total in/out, closing balance and per-category totals for the period.</summary>
    [HttpGet("summary")]
    [HasPermission(Permissions.CashView)]
    public async Task<IActionResult> Summary([FromQuery] DateTime? from, [FromQuery] DateTime? to, CancellationToken ct) =>
        Ok(await _cash.GetSummaryAsync(from, to, ct));
}
