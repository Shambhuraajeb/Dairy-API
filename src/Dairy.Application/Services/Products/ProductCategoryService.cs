using Dairy.Application.BusinessRules;
using Dairy.Application.Common;
using Dairy.Application.DTOs.Products;
using Dairy.Application.Interfaces;
using Dairy.Application.Interfaces.Products;
using Dairy.Application.Requests.Products;
using Dairy.Domain.Constants;

namespace Dairy.Application.Services.Products;

public class ProductCategoryService : IProductCategoryService
{
    private readonly IProductCategoryRepository _repo;
    private readonly IAuditService _audit;
    private readonly ICurrentUser _user;

    public ProductCategoryService(IProductCategoryRepository repo, IAuditService audit, ICurrentUser user)
    {
        _repo = repo;
        _audit = audit;
        _user = user;
    }

    public Task<PagedResult<ProductCategoryDto>> GetPagedAsync(ProductCategoryQuery query, CancellationToken ct = default) =>
        _repo.GetPagedAsync(query, ct);

    public async Task<ProductCategoryDto> GetByIdAsync(long id, CancellationToken ct = default) =>
        await _repo.GetByIdAsync(id, ct) ?? throw new NotFoundException("Product category", id);

    public async Task<ProductCategoryDto> CreateAsync(CreateProductCategoryRequest request, CancellationToken ct = default)
    {
        var code = SalesValues.Clean(request.Code)?.ToUpperInvariant();
        var name = request.Name.Trim();
        if (code is not null && await _repo.CodeExistsAsync(code, null, ct))
            throw new ConflictException($"A category with code '{code}' already exists.");
        if (await _repo.NameExistsAsync(name, null, ct))
            throw new ConflictException($"A category named '{name}' already exists.");

        var created = await _repo.CreateAsync(code, request, _user.UserId, ct);
        var dto = await _repo.GetByIdAsync(created.Id, ct) ?? throw new NotFoundException("Product category", created.Id);

        await _audit.LogAsync(AuditActions.Create, "ProductCategory", created.Id, null, dto, ct);
        return dto;
    }

    public async Task<ProductCategoryDto> UpdateAsync(long id, UpdateProductCategoryRequest request, CancellationToken ct = default)
    {
        var existing = await _repo.GetByIdAsync(id, ct) ?? throw new NotFoundException("Product category", id);
        var code = request.Code.Trim().ToUpperInvariant();
        var name = request.Name.Trim();
        if (await _repo.CodeExistsAsync(code, id, ct))
            throw new ConflictException($"A category with code '{code}' already exists.");
        if (await _repo.NameExistsAsync(name, id, ct))
            throw new ConflictException($"A category named '{name}' already exists.");

        var isActive = request.IsActive ?? existing.IsActive;
        if (existing.IsActive && !isActive) await EnsureNoActiveProductsAsync(id, ct);

        await _repo.UpdateAsync(id, request, isActive, _user.UserId, ct);
        var dto = await _repo.GetByIdAsync(id, ct) ?? throw new NotFoundException("Product category", id);

        var action = existing.IsActive && !isActive ? AuditActions.Deactivate : AuditActions.Update;
        await _audit.LogAsync(action, "ProductCategory", id, existing, dto, ct);
        return dto;
    }

    /// <summary>Soft delete: deactivates the category (blocked while active products use it).</summary>
    public async Task DeleteAsync(long id, CancellationToken ct = default)
    {
        var existing = await _repo.GetByIdAsync(id, ct) ?? throw new NotFoundException("Product category", id);
        await EnsureNoActiveProductsAsync(id, ct);

        await _repo.SetActiveAsync(id, false, _user.UserId, ct);
        await _audit.LogAsync(AuditActions.Deactivate, "ProductCategory", id, existing, null, ct);
    }

    private async Task EnsureNoActiveProductsAsync(long id, CancellationToken ct)
    {
        var count = await _repo.CountActiveProductsAsync(id, ct);
        if (count > 0)
            throw new BusinessRuleException($"This category still has {count} active product(s). Move or deactivate them first.");
    }
}
