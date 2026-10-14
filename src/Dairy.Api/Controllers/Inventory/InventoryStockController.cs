using Dairy.Api.Authorization;
using Dairy.Application.Interfaces.Inventory;
using Dairy.Application.Requests.Inventory;
using Dairy.Domain.Constants;
using Microsoft.AspNetCore.Mvc;

namespace Dairy.Api.Controllers.Inventory;

/// <summary>Batches, consumption, adjustments, stock ledger, alerts and summary under /api/inventory.</summary>
[Route("api/inventory")]
public class InventoryStockController : ApiControllerBase
{
    private readonly IInventoryStockService _stock;

    public InventoryStockController(IInventoryStockService stock) => _stock = stock;

    [HttpGet("batches")]
    [HasPermission(Permissions.InventoryView)]
    public async Task<IActionResult> Batches([FromQuery] BatchQuery query, CancellationToken ct) =>
        Paged(await _stock.GetBatchesPagedAsync(query, ct));

    [HttpGet("stock-transactions")]
    [HasPermission(Permissions.InventoryView)]
    public async Task<IActionResult> Ledger([FromQuery] StockTransactionQuery query, CancellationToken ct) =>
        Paged(await _stock.GetLedgerPagedAsync(query, ct));

    [HttpGet("consumption")]
    [HasPermission(Permissions.InventoryView)]
    public async Task<IActionResult> Consumptions([FromQuery] ConsumptionQuery query, CancellationToken ct) =>
        Paged(await _stock.GetConsumptionsPagedAsync(query, ct));

    /// <summary>Consumes stock (FEFO when no batch given). expiredOverride is honoured only for users with inventory.adjust.</summary>
    [HttpPost("consumption")]
    [HasPermission(Permissions.InventoryCreate)]
    public async Task<IActionResult> RecordConsumption([FromBody] RecordConsumptionRequest request, CancellationToken ct) =>
        Created(await _stock.RecordConsumptionAsync(request, ct), "Consumption recorded.");

    [HttpGet("adjustments")]
    [HasPermission(Permissions.InventoryView)]
    public async Task<IActionResult> Adjustments([FromQuery] StockTransactionQuery query, CancellationToken ct) =>
        Paged(await _stock.GetAdjustmentsPagedAsync(query, ct));

    [HttpPost("adjustments")]
    [HasPermission(Permissions.InventoryAdjust)]
    public async Task<IActionResult> AdjustStock([FromBody] AdjustStockRequest request, CancellationToken ct) =>
        Created(await _stock.AdjustStockAsync(request, ct), "Stock adjusted.");

    [HttpGet("alerts/low-stock")]
    [HasPermission(Permissions.InventoryView)]
    public async Task<IActionResult> LowStock(CancellationToken ct) =>
        Ok(await _stock.GetLowStockAsync(ct));

    [HttpGet("alerts/expiring")]
    [HasPermission(Permissions.InventoryView)]
    public async Task<IActionResult> Expiring([FromQuery] int? days, CancellationToken ct) =>
        Ok(await _stock.GetExpiringAsync(days, ct));

    [HttpGet("summary")]
    [HasPermission(Permissions.InventoryView)]
    public async Task<IActionResult> Summary(CancellationToken ct) =>
        Ok(await _stock.GetSummaryAsync(ct));
}
