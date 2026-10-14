using Dairy.Api.Authorization;
using Dairy.Application.Interfaces.Dashboard;
using Dairy.Domain.Constants;
using Microsoft.AspNetCore.Mvc;

namespace Dairy.Api.Controllers.Dashboard;

[Route("api/dashboard")]
[HasPermission(Permissions.DashboardView)]
public class DashboardController : ApiControllerBase
{
    private readonly IDashboardService _dashboard;

    public DashboardController(IDashboardService dashboard) => _dashboard = dashboard;

    /// <summary>Owner/manager KPIs: flat object with 20 lowerCamel fields.</summary>
    [HttpGet("summary")]
    public async Task<IActionResult> Summary([FromQuery] long? branchId, [FromQuery] long? collectionCenterId, CancellationToken ct) =>
        Ok(await _dashboard.GetSummaryAsync(branchId, collectionCenterId, ct));

    /// <summary>Six chart series, each [{ label, value }]. days defaults to 30 (1..365).</summary>
    [HttpGet("charts")]
    public async Task<IActionResult> Charts([FromQuery] int? days, CancellationToken ct) =>
        Ok(await _dashboard.GetChartsAsync(days, ct));

    [HttpGet("collection")]
    public async Task<IActionResult> Collection([FromQuery] long? collectionCenterId, CancellationToken ct) =>
        Ok(await _dashboard.GetCollectionAsync(collectionCenterId, ct));

    [HttpGet("inventory")]
    public async Task<IActionResult> Inventory(CancellationToken ct) =>
        Ok(await _dashboard.GetInventoryAsync(ct));

    /// <summary>Customers get their own dashboard; staff need customer.view and ?customerId=.</summary>
    [HttpGet("customer")]
    public async Task<IActionResult> Customer([FromQuery] long? customerId, CancellationToken ct) =>
        Ok(await _dashboard.GetCustomerAsync(customerId, ct));
}
