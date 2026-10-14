using Dairy.Api.Authorization;
using Dairy.Application.Interfaces.RateCharts;
using Dairy.Application.Requests.Collection;
using Dairy.Application.Requests.RateCharts;
using Dairy.Domain.Constants;
using Microsoft.AspNetCore.Mvc;

namespace Dairy.Api.Controllers.RateCharts;

[Route("api/rate-charts")]
public class RateChartsController : ApiControllerBase
{
    private readonly IRateChartService _charts;

    public RateChartsController(IRateChartService charts) => _charts = charts;

    [HttpGet]
    [HasPermission(Permissions.RatechartView)]
    public async Task<IActionResult> List([FromQuery] RateChartQuery query, CancellationToken ct) =>
        Paged(await _charts.GetPagedAsync(query, ct));

    [HttpGet("{id:long}")]
    [HasPermission(Permissions.RatechartView)]
    public async Task<IActionResult> Get(long id, CancellationToken ct) =>
        Ok(await _charts.GetByIdAsync(id, ct));

    /// <summary>Same calculation as GET /api/milk-collections/rate-preview.</summary>
    [HttpGet("calculate")]
    [HasPermission(Permissions.RatechartView)]
    public async Task<IActionResult> Calculate([FromQuery] RatePreviewQuery query, CancellationToken ct) =>
        Ok(await _charts.PreviewRateAsync(query, ct));

    [HttpPost]
    [HasPermission(Permissions.RatechartManage)]
    public async Task<IActionResult> Create([FromBody] SaveRateChartRequest request, CancellationToken ct) =>
        Created(await _charts.CreateAsync(request, ct), "Rate chart created as draft.");

    [HttpPut("{id:long}")]
    [HasPermission(Permissions.RatechartManage)]
    public async Task<IActionResult> Update(long id, [FromBody] SaveRateChartRequest request, CancellationToken ct) =>
        Ok(await _charts.UpdateAsync(id, request, ct), "Rate chart updated.");

    [HttpPost("{id:long}/activate")]
    [HasPermission(Permissions.RatechartManage)]
    public async Task<IActionResult> Activate(long id, CancellationToken ct) =>
        Ok(await _charts.ActivateAsync(id, ct), "Rate chart activated.");

    /// <summary>Draft charts are deleted; Active/Expired charts are deactivated.</summary>
    [HttpDelete("{id:long}")]
    [HasPermission(Permissions.RatechartManage)]
    public async Task<IActionResult> Delete(long id, CancellationToken ct)
    {
        var deleted = await _charts.DeleteAsync(id, ct);
        return Message(deleted ? "Draft rate chart deleted." : "Rate chart deactivated.");
    }
}
