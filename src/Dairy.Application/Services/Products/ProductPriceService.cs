using Dairy.Application.Common;
using Dairy.Application.DTOs.Products;
using Dairy.Application.Interfaces;
using Dairy.Application.Interfaces.Products;
using Dairy.Application.Requests.Products;
using Dairy.Domain.Constants;

namespace Dairy.Application.Services.Products;

public class ProductPriceService : IProductPriceService
{
    private readonly IProductPriceRepository _repo;
    private readonly IAuditService _audit;
    private readonly ICurrentUser _user;

    public ProductPriceService(IProductPriceRepository repo, IAuditService audit, ICurrentUser user)
    {
        _repo = repo;
        _audit = audit;
        _user = user;
    }

    public Task<PagedResult<ProductPriceDto>> GetPagedAsync(ProductPriceQuery query, CancellationToken ct = default) =>
        _repo.GetPagedAsync(query, ct);

    public async Task<ProductPriceDto> CreateAsync(CreateProductPriceRequest request, CancellationToken ct = default)
    {
        if (!await _repo.ProductExistsAsync(request.ProductId, ct))
            throw new BusinessRuleException("The selected product does not exist or is inactive.");

        if (await _repo.HasLaterOverlapAsync(request.ProductId, request.CustomerType, request.EffectiveFrom, request.EffectiveTo, ct))
            throw new BusinessRuleException("Another price for this product and customer type already starts within this period. Adjust the dates or deactivate that price first.");

        var id = await _repo.CreateAsync(request, _user.UserId, ct);
        var dto = await _repo.GetByIdAsync(id, ct) ?? throw new NotFoundException("Product price", id);

        await _audit.LogAsync(AuditActions.Create, "ProductPrice", id, null, dto, ct);
        return dto;
    }

    /// <summary>Soft delete: deactivates the price row.</summary>
    public async Task DeleteAsync(long id, CancellationToken ct = default)
    {
        var existing = await _repo.GetByIdAsync(id, ct) ?? throw new NotFoundException("Product price", id);
        await _repo.DeactivateAsync(id, _user.UserId, ct);
        await _audit.LogAsync(AuditActions.Deactivate, "ProductPrice", id, existing, null, ct);
    }
}
