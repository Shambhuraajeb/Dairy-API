using Dairy.Application.BusinessRules;
using Dairy.Application.Common;
using Dairy.Application.DTOs.Products;
using Dairy.Application.Interfaces;
using Dairy.Application.Interfaces.Products;
using Dairy.Application.Requests.Products;
using Dairy.Domain.Constants;

namespace Dairy.Application.Services.Products;

public class ProductService : IProductService
{
    private readonly IProductRepository _repo;
    private readonly IAuditService _audit;
    private readonly ICurrentUser _user;

    public ProductService(IProductRepository repo, IAuditService audit, ICurrentUser user)
    {
        _repo = repo;
        _audit = audit;
        _user = user;
    }

    public Task<PagedResult<ProductDto>> GetPagedAsync(ProductQuery query, CancellationToken ct = default) =>
        _repo.GetPagedAsync(query, ct);

    public async Task<ProductDto> GetByIdAsync(long id, CancellationToken ct = default) =>
        await _repo.GetByIdAsync(id, ct) ?? throw new NotFoundException("Product", id);

    public async Task<ProductDto> CreateAsync(CreateProductRequest request, CancellationToken ct = default)
    {
        var code = SalesValues.Clean(request.ProductCode)?.ToUpperInvariant();
        if (code is not null && await _repo.CodeExistsAsync(code, null, ct))
            throw new ConflictException($"A product with code '{code}' already exists.");
        await EnsureReferencesAsync(request.ProductCategoryId, request.UnitId, ct);

        var created = await _repo.CreateAsync(code, request, _user.UserId, ct);
        var dto = await _repo.GetByIdAsync(created.Id, ct) ?? throw new NotFoundException("Product", created.Id);

        await _audit.LogAsync(AuditActions.Create, "Product", created.Id, null, dto, ct);
        return dto;
    }

    public async Task<ProductDto> UpdateAsync(long id, UpdateProductRequest request, CancellationToken ct = default)
    {
        var existing = await _repo.GetByIdAsync(id, ct) ?? throw new NotFoundException("Product", id);
        var code = request.ProductCode.Trim().ToUpperInvariant();
        if (await _repo.CodeExistsAsync(code, id, ct))
            throw new ConflictException($"A product with code '{code}' already exists.");
        await EnsureReferencesAsync(request.ProductCategoryId, request.UnitId, ct);

        var isActive = request.IsActive ?? existing.IsActive;
        if (existing.IsActive && !isActive) await EnsureNotInUseAsync(id, ct);

        await _repo.UpdateAsync(id, request, isActive, _user.UserId, ct);
        var dto = await _repo.GetByIdAsync(id, ct) ?? throw new NotFoundException("Product", id);

        var action = existing.IsActive && !isActive ? AuditActions.Deactivate : AuditActions.Update;
        await _audit.LogAsync(action, "Product", id, existing, dto, ct);
        return dto;
    }

    /// <summary>Soft delete: deactivates the product (blocked while open orders/subscriptions use it).</summary>
    public async Task DeleteAsync(long id, CancellationToken ct = default)
    {
        var existing = await _repo.GetByIdAsync(id, ct) ?? throw new NotFoundException("Product", id);
        await EnsureNotInUseAsync(id, ct);

        await _repo.SetActiveAsync(id, false, _user.UserId, ct);
        await _audit.LogAsync(AuditActions.Deactivate, "Product", id, existing, null, ct);
    }

    public async Task<ProductPriceLookupDto> GetPriceAsync(long id, string? customerType, DateTime? date, CancellationToken ct = default)
    {
        var type = SalesValues.Clean(customerType) ?? "Retail";
        if (!SalesValues.PriceCustomerTypes.Contains(type))
            throw new RequestValidationException("customerType", "customerType must be Retail or Distributor.");

        return await _repo.GetPriceAsync(id, type, (date ?? DateTime.Today).Date, ct)
               ?? throw new NotFoundException("Product", id);
    }

    private async Task EnsureReferencesAsync(long categoryId, long unitId, CancellationToken ct)
    {
        if (!await _repo.CategoryExistsAsync(categoryId, ct))
            throw new BusinessRuleException("The selected product category does not exist or is inactive.");
        if (!await _repo.UnitExistsAsync(unitId, ct))
            throw new BusinessRuleException("The selected unit does not exist or is inactive.");
    }

    private async Task EnsureNotInUseAsync(long id, CancellationToken ct)
    {
        var open = await _repo.CountOpenOrdersAsync(id, ct);
        if (open > 0)
            throw new BusinessRuleException($"This product is used by {open} open order(s)/active subscription(s) and cannot be deactivated yet.");
    }
}
