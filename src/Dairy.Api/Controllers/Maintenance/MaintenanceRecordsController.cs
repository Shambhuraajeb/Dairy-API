using Dairy.Api.Authorization;
using Dairy.Application.Interfaces.Maintenance;
using Dairy.Application.Requests.Maintenance;
using Dairy.Domain.Constants;
using Microsoft.AspNetCore.Mvc;

namespace Dairy.Api.Controllers.Maintenance;

/// <summary>Maintenance history (records) and the due list under /api/maintenance.</summary>
[Route("api/maintenance")]
public class MaintenanceRecordsController : ApiControllerBase
{
    private readonly IMaintenanceRecordService _records;

    public MaintenanceRecordsController(IMaintenanceRecordService records) => _records = records;

    [HttpGet("records")]
    [HasPermission(Permissions.MaintenanceView)]
    public async Task<IActionResult> List([FromQuery] MaintenanceRecordQuery query, CancellationToken ct) =>
        Paged(await _records.GetPagedAsync(query, ct));

    [HttpGet("records/{id:long}")]
    [HasPermission(Permissions.MaintenanceView)]
    public async Task<IActionResult> Get(long id, CancellationToken ct) =>
        Ok(await _records.GetByIdAsync(id, ct));

    /// <summary>Records maintenance; with a scheduleId it advances that schedule's next due date.</summary>
    [HttpPost("records")]
    [HasPermission(Permissions.MaintenanceCreate)]
    public async Task<IActionResult> Create([FromBody] CreateMaintenanceRecordRequest request, CancellationToken ct) =>
        Created(await _records.CreateAsync(request, ct), "Maintenance recorded.");

    /// <summary>Active schedules due within <c>days</c> (default: each schedule's own alertBeforeDays), including overdue.</summary>
    [HttpGet("due")]
    [HasPermission(Permissions.MaintenanceView)]
    public async Task<IActionResult> Due([FromQuery] int? days, CancellationToken ct) =>
        Ok(await _records.GetDueAsync(days, ct));
}
