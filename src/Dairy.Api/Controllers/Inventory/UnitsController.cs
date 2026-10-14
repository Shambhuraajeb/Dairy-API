using Dairy.Api.Authorization;
using Dairy.Application.Interfaces.Inventory;
using Dairy.Application.Requests.Inventory;
using Dairy.Domain.Constants;
using Microsoft.AspNetCore.Mvc;

namespace Dairy.Api.Controllers.Inventory;

/// <summary>REFERENCE module: copy this pattern for other CRUD controllers.</summary>
[Route("api/inventory/units")]
public class UnitsController : ApiControllerBase
{
    private readonly IUnitService _units;

    public UnitsController(IUnitService units) => _units = units;

    [HttpGet]
    [HasPermission(Permissions.InventoryView)]
    public async Task<IActionResult> List([FromQuery] UnitQuery query, CancellationToken ct) =>
        Paged(await _units.GetPagedAsync(query, ct));

    [HttpGet("{id:long}")]
    [HasPermission(Permissions.InventoryView)]
    public async Task<IActionResult> Get(long id, CancellationToken ct) =>
        Ok(await _units.GetByIdAsync(id, ct));

    [HttpPost]
    [HasPermission(Permissions.InventoryCreate)]
    public async Task<IActionResult> Create([FromBody] CreateUnitRequest request, CancellationToken ct) =>
        Created(await _units.CreateAsync(request, ct), "Unit created.");

    /// <summary>Update; send isActive=false to deactivate.</summary>
    [HttpPut("{id:long}")]
    [HasPermission(Permissions.InventoryUpdate)]
    public async Task<IActionResult> Update(long id, [FromBody] UpdateUnitRequest request, CancellationToken ct) =>
        Ok(await _units.UpdateAsync(id, request, ct), "Unit updated.");
}
