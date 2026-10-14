using Dairy.Api.Authorization;
using Dairy.Application.Interfaces.Maintenance;
using Dairy.Application.Requests.Maintenance;
using Dairy.Domain.Constants;
using Microsoft.AspNetCore.Mvc;

namespace Dairy.Api.Controllers.Maintenance;

[Route("api/maintenance/schedules")]
public class MaintenanceSchedulesController : ApiControllerBase
{
    private readonly IMaintenanceScheduleService _schedules;

    public MaintenanceSchedulesController(IMaintenanceScheduleService schedules) => _schedules = schedules;

    [HttpGet]
    [HasPermission(Permissions.MaintenanceView)]
    public async Task<IActionResult> List([FromQuery] MaintenanceScheduleQuery query, CancellationToken ct) =>
        Paged(await _schedules.GetPagedAsync(query, ct));

    [HttpGet("{id:long}")]
    [HasPermission(Permissions.MaintenanceView)]
    public async Task<IActionResult> Get(long id, CancellationToken ct) =>
        Ok(await _schedules.GetByIdAsync(id, ct));

    [HttpPost]
    [HasPermission(Permissions.MaintenanceCreate)]
    public async Task<IActionResult> Create([FromBody] CreateMaintenanceScheduleRequest request, CancellationToken ct) =>
        Created(await _schedules.CreateAsync(request, ct), "Maintenance schedule created.");

    /// <summary>Update; status Active | Paused, isActive=false removes the schedule from lists.</summary>
    [HttpPut("{id:long}")]
    [HasPermission(Permissions.MaintenanceUpdate)]
    public async Task<IActionResult> Update(long id, [FromBody] UpdateMaintenanceScheduleRequest request, CancellationToken ct) =>
        Ok(await _schedules.UpdateAsync(id, request, ct), "Maintenance schedule updated.");
}
