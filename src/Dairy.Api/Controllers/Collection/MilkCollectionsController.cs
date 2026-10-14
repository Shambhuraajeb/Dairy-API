using Dairy.Api.Authorization;
using Dairy.Application.Interfaces.Collection;
using Dairy.Application.Interfaces.RateCharts;
using Dairy.Application.Requests.Collection;
using Dairy.Domain.Constants;
using Microsoft.AspNetCore.Mvc;

namespace Dairy.Api.Controllers.Collection;

[Route("api/milk-collections")]
public class MilkCollectionsController : ApiControllerBase
{
    private readonly IMilkCollectionService _collections;
    private readonly IRateChartService _rateCharts;

    public MilkCollectionsController(IMilkCollectionService collections, IRateChartService rateCharts)
    {
        _collections = collections;
        _rateCharts = rateCharts;
    }

    [HttpGet]
    [HasPermission(Permissions.CollectionView)]
    public async Task<IActionResult> List([FromQuery] MilkCollectionQuery query, CancellationToken ct) =>
        Paged(await _collections.GetPagedAsync(query, ct));

    [HttpGet("pending-approvals")]
    [HasPermission(Permissions.CollectionView)]
    public async Task<IActionResult> PendingApprovals([FromQuery] MilkCollectionQuery query, CancellationToken ct) =>
        Paged(await _collections.GetPendingApprovalsAsync(query, ct));

    [HttpGet("rate-preview")]
    [HasPermission(Permissions.CollectionView)]
    public async Task<IActionResult> RatePreview([FromQuery] RatePreviewQuery query, CancellationToken ct) =>
        Ok(await _rateCharts.PreviewRateAsync(query, ct));

    [HttpGet("{id:long}")]
    [HasPermission(Permissions.CollectionView)]
    public async Task<IActionResult> Get(long id, CancellationToken ct) =>
        Ok(await _collections.GetByIdAsync(id, ct));

    [HttpPost]
    [HasPermission(Permissions.CollectionCreate)]
    public async Task<IActionResult> Create([FromBody] CreateMilkCollectionRequest request, CancellationToken ct) =>
        Created(await _collections.CreateAsync(request, ct), "Milk collection recorded.");

    /// <summary>Adjusts quantity / FAT / SNF / CLR of a pending or approved, unsettled collection (originals are kept).</summary>
    [HttpPut("{id:long}")]
    [HasPermission(Permissions.CollectionUpdate)]
    public async Task<IActionResult> Adjust(long id, [FromBody] AdjustMilkCollectionRequest request, CancellationToken ct) =>
        Ok(await _collections.AdjustAsync(id, request, ct), "Milk collection adjusted.");

    [HttpPost("{id:long}/approve")]
    [HasPermission(Permissions.CollectionApprove)]
    public async Task<IActionResult> Approve(long id, CancellationToken ct) =>
        Ok(await _collections.ApproveAsync(id, ct), "Milk collection approved.");

    [HttpPost("{id:long}/reject")]
    [HasPermission(Permissions.CollectionReject)]
    public async Task<IActionResult> Reject(long id, [FromBody] RejectMilkCollectionRequest request, CancellationToken ct) =>
        Ok(await _collections.RejectAsync(id, request, ct), "Milk collection rejected.");

    [HttpPost("bulk-approve")]
    [HasPermission(Permissions.CollectionApprove)]
    public async Task<IActionResult> BulkApprove([FromBody] BulkApproveRequest request, CancellationToken ct)
    {
        var result = await _collections.BulkApproveAsync(request, ct);
        return Ok(result, $"{result.Approved} of {result.Total} collections approved.");
    }

    [HttpGet("{id:long}/quality")]
    [HasPermission(Permissions.CollectionView)]
    public async Task<IActionResult> QualityTests(long id, CancellationToken ct) =>
        Ok(await _collections.GetQualityTestsAsync(id, ct));

    [HttpPost("{id:long}/quality")]
    [HasPermission(Permissions.CollectionQualityRecord)]
    public async Task<IActionResult> RecordQuality(long id, [FromBody] RecordQualityRequest request, CancellationToken ct) =>
        Ok(await _collections.RecordQualityAsync(id, request, ct), "Quality test recorded.");
}
