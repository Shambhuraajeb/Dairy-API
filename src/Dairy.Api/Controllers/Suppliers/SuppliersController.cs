using Dairy.Api.Authorization;
using Dairy.Application.Interfaces.Suppliers;
using Dairy.Application.Requests.Suppliers;
using Dairy.Domain.Constants;
using Microsoft.AspNetCore.Mvc;

namespace Dairy.Api.Controllers.Suppliers;

[Route("api/suppliers")]
public class SuppliersController : ApiControllerBase
{
    private readonly ISupplierService _suppliers;

    public SuppliersController(ISupplierService suppliers) => _suppliers = suppliers;

    [HttpGet]
    [HasPermission(Permissions.SupplierView)]
    public async Task<IActionResult> List([FromQuery] SupplierQuery query, CancellationToken ct) =>
        Paged(await _suppliers.GetPagedAsync(query, ct));

    [HttpGet("{id:long}")]
    [HasPermission(Permissions.SupplierView)]
    public async Task<IActionResult> Get(long id, CancellationToken ct) =>
        Ok(await _suppliers.GetByIdAsync(id, ct));

    [HttpPost]
    [HasPermission(Permissions.SupplierManage)]
    public async Task<IActionResult> Create([FromBody] CreateSupplierRequest request, CancellationToken ct) =>
        Created(await _suppliers.CreateAsync(request, ct), "Supplier created.");

    /// <summary>Update; send isActive=false to deactivate.</summary>
    [HttpPut("{id:long}")]
    [HasPermission(Permissions.SupplierManage)]
    public async Task<IActionResult> Update(long id, [FromBody] UpdateSupplierRequest request, CancellationToken ct) =>
        Ok(await _suppliers.UpdateAsync(id, request, ct), "Supplier updated.");
}
