using Dairy.Api.Authorization;
using Dairy.Application.Interfaces.Inventory;
using Dairy.Application.Requests.Inventory;
using Dairy.Domain.Constants;
using Microsoft.AspNetCore.Mvc;

namespace Dairy.Api.Controllers.Inventory;

[Route("api/inventory/categories")]
public class ItemCategoriesController : ApiControllerBase
{
    private readonly IItemCategoryService _categories;

    public ItemCategoriesController(IItemCategoryService categories) => _categories = categories;

    [HttpGet]
    [HasPermission(Permissions.InventoryView)]
    public async Task<IActionResult> List([FromQuery] ItemCategoryQuery query, CancellationToken ct) =>
        Paged(await _categories.GetPagedAsync(query, ct));

    [HttpGet("{id:long}")]
    [HasPermission(Permissions.InventoryView)]
    public async Task<IActionResult> Get(long id, CancellationToken ct) =>
        Ok(await _categories.GetByIdAsync(id, ct));

    [HttpPost]
    [HasPermission(Permissions.InventoryCreate)]
    public async Task<IActionResult> Create([FromBody] CreateItemCategoryRequest request, CancellationToken ct) =>
        Created(await _categories.CreateAsync(request, ct), "Item category created.");

    /// <summary>Update; send isActive=false to deactivate.</summary>
    [HttpPut("{id:long}")]
    [HasPermission(Permissions.InventoryUpdate)]
    public async Task<IActionResult> Update(long id, [FromBody] UpdateItemCategoryRequest request, CancellationToken ct) =>
        Ok(await _categories.UpdateAsync(id, request, ct), "Item category updated.");
}
