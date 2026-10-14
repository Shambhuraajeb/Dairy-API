using Dairy.Api.Authorization;
using Dairy.Application.Interfaces.Maintenance;
using Dairy.Application.Requests.Maintenance;
using Dairy.Domain.Constants;
using Microsoft.AspNetCore.Mvc;

namespace Dairy.Api.Controllers.Maintenance;

[Route("api/maintenance/equipment")]
public class EquipmentController : ApiControllerBase
{
    private readonly IEquipmentService _equipment;

    public EquipmentController(IEquipmentService equipment) => _equipment = equipment;

    [HttpGet]
    [HasPermission(Permissions.MaintenanceView)]
    public async Task<IActionResult> List([FromQuery] EquipmentQuery query, CancellationToken ct) =>
        Paged(await _equipment.GetPagedAsync(query, ct));

    [HttpGet("{id:long}")]
    [HasPermission(Permissions.MaintenanceView)]
    public async Task<IActionResult> Get(long id, CancellationToken ct) =>
        Ok(await _equipment.GetByIdAsync(id, ct));

    [HttpPost]
    [HasPermission(Permissions.MaintenanceCreate)]
    public async Task<IActionResult> Create([FromBody] CreateEquipmentRequest request, CancellationToken ct) =>
        Created(await _equipment.CreateAsync(request, ct), "Equipment created.");

    /// <summary>Update; set status (Operational | UnderMaintenance | Retired) or isActive=false to deactivate.</summary>
    [HttpPut("{id:long}")]
    [HasPermission(Permissions.MaintenanceUpdate)]
    public async Task<IActionResult> Update(long id, [FromBody] UpdateEquipmentRequest request, CancellationToken ct) =>
        Ok(await _equipment.UpdateAsync(id, request, ct), "Equipment updated.");
}
