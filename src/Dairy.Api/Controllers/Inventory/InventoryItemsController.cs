using Dairy.Api.Authorization;
using Dairy.Application.Interfaces.Inventory;
using Dairy.Application.Requests.Inventory;
using Dairy.Domain.Constants;
using Microsoft.AspNetCore.Mvc;

namespace Dairy.Api.Controllers.Inventory;

[Route("api/inventory/items")]
public class InventoryItemsController : ApiControllerBase
{
    private readonly IInventoryItemService _items;

    public InventoryItemsController(IInventoryItemService items) => _items = items;

    [HttpGet]
    [HasPermission(Permissions.InventoryView)]
    public async Task<IActionResult> List([FromQuery] InventoryItemQuery query, CancellationToken ct) =>
        Paged(await _items.GetPagedAsync(query, ct));

    /// <summary>Item with batch summary and its batches.</summary>
    [HttpGet("{id:long}")]
    [HasPermission(Permissions.InventoryView)]
    public async Task<IActionResult> Get(long id, CancellationToken ct) =>
        Ok(await _items.GetByIdAsync(id, ct));

    [HttpPost]
    [HasPermission(Permissions.InventoryCreate)]
    public async Task<IActionResult> Create([FromBody] CreateInventoryItemRequest request, CancellationToken ct) =>
        Created(await _items.CreateAsync(request, ct), "Inventory item created.");

    /// <summary>Update; send isActive=false to deactivate. Stock is never edited here (use purchases / adjustments).</summary>
    [HttpPut("{id:long}")]
    [HasPermission(Permissions.InventoryUpdate)]
    public async Task<IActionResult> Update(long id, [FromBody] UpdateInventoryItemRequest request, CancellationToken ct) =>
        Ok(await _items.UpdateAsync(id, request, ct), "Inventory item updated.");
}
