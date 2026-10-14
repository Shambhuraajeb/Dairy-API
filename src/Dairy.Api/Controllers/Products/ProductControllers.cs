using Dairy.Api.Authorization;
using Dairy.Application.Interfaces.Products;
using Dairy.Application.Requests.Products;
using Dairy.Domain.Constants;
using Microsoft.AspNetCore.Mvc;

namespace Dairy.Api.Controllers.Products;

[Route("api/product-categories")]
public class ProductCategoriesController : ApiControllerBase
{
    private readonly IProductCategoryService _categories;

    public ProductCategoriesController(IProductCategoryService categories) => _categories = categories;

    [HttpGet]
    [HasPermission(Permissions.ProductView)]
    public async Task<IActionResult> List([FromQuery] ProductCategoryQuery query, CancellationToken ct) =>
        Paged(await _categories.GetPagedAsync(query, ct));

    [HttpGet("{id:long}")]
    [HasPermission(Permissions.ProductView)]
    public async Task<IActionResult> Get(long id, CancellationToken ct) =>
        Ok(await _categories.GetByIdAsync(id, ct));

    [HttpPost]
    [HasPermission(Permissions.ProductCreate)]
    public async Task<IActionResult> Create([FromBody] CreateProductCategoryRequest request, CancellationToken ct) =>
        Created(await _categories.CreateAsync(request, ct), "Product category created.");

    [HttpPut("{id:long}")]
    [HasPermission(Permissions.ProductUpdate)]
    public async Task<IActionResult> Update(long id, [FromBody] UpdateProductCategoryRequest request, CancellationToken ct) =>
        Ok(await _categories.UpdateAsync(id, request, ct), "Product category updated.");

    /// <summary>Soft delete (deactivates the category).</summary>
    [HttpDelete("{id:long}")]
    [HasPermission(Permissions.ProductUpdate)]
    public async Task<IActionResult> Delete(long id, CancellationToken ct)
    {
        await _categories.DeleteAsync(id, ct);
        return Message("Product category deactivated.");
    }
}

[Route("api/products")]
public class ProductsController : ApiControllerBase
{
    private readonly IProductService _products;

    public ProductsController(IProductService products) => _products = products;

    [HttpGet]
    [HasPermission(Permissions.ProductView)]
    public async Task<IActionResult> List([FromQuery] ProductQuery query, CancellationToken ct) =>
        Paged(await _products.GetPagedAsync(query, ct));

    [HttpGet("{id:long}")]
    [HasPermission(Permissions.ProductView)]
    public async Task<IActionResult> Get(long id, CancellationToken ct) =>
        Ok(await _products.GetByIdAsync(id, ct));

    /// <summary>Effective price on a date: ?customerType=Retail|Distributor (default Retail)&amp;date=yyyy-MM-dd (default today).</summary>
    [HttpGet("{id:long}/price")]
    [HasPermission(Permissions.ProductView)]
    public async Task<IActionResult> Price(long id, [FromQuery] string? customerType, [FromQuery] DateTime? date, CancellationToken ct) =>
        Ok(await _products.GetPriceAsync(id, customerType, date, ct));

    [HttpPost]
    [HasPermission(Permissions.ProductCreate)]
    public async Task<IActionResult> Create([FromBody] CreateProductRequest request, CancellationToken ct) =>
        Created(await _products.CreateAsync(request, ct), "Product created.");

    [HttpPut("{id:long}")]
    [HasPermission(Permissions.ProductUpdate)]
    public async Task<IActionResult> Update(long id, [FromBody] UpdateProductRequest request, CancellationToken ct) =>
        Ok(await _products.UpdateAsync(id, request, ct), "Product updated.");

    /// <summary>Soft delete (deactivates the product).</summary>
    [HttpDelete("{id:long}")]
    [HasPermission(Permissions.ProductUpdate)]
    public async Task<IActionResult> Delete(long id, CancellationToken ct)
    {
        await _products.DeleteAsync(id, ct);
        return Message("Product deactivated.");
    }
}

[Route("api/product-prices")]
public class ProductPricesController : ApiControllerBase
{
    private readonly IProductPriceService _prices;

    public ProductPricesController(IProductPriceService prices) => _prices = prices;

    [HttpGet]
    [HasPermission(Permissions.ProductView)]
    public async Task<IActionResult> List([FromQuery] ProductPriceQuery query, CancellationToken ct) =>
        Paged(await _prices.GetPagedAsync(query, ct));

    [HttpPost]
    [HasPermission(Permissions.ProductUpdate)]
    public async Task<IActionResult> Create([FromBody] CreateProductPriceRequest request, CancellationToken ct) =>
        Created(await _prices.CreateAsync(request, ct), "Price added.");

    /// <summary>Soft delete (deactivates the price row).</summary>
    [HttpDelete("{id:long}")]
    [HasPermission(Permissions.ProductUpdate)]
    public async Task<IActionResult> Delete(long id, CancellationToken ct)
    {
        await _prices.DeleteAsync(id, ct);
        return Message("Price removed.");
    }
}
