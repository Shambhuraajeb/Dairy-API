using Dairy.Application.Common;
using Dairy.Application.DTOs.Products;
using Dairy.Application.Requests.Products;

namespace Dairy.Application.Interfaces.Products;

public interface IProductCategoryRepository
{
    Task<PagedResult<ProductCategoryDto>> GetPagedAsync(ProductCategoryQuery query, CancellationToken ct = default);
    Task<ProductCategoryDto?> GetByIdAsync(long id, CancellationToken ct = default);
    Task<bool> CodeExistsAsync(string code, long? excludeId, CancellationToken ct = default);
    Task<bool> NameExistsAsync(string name, long? excludeId, CancellationToken ct = default);
    Task<CreatedResult> CreateAsync(string? code, CreateProductCategoryRequest request, long userId, CancellationToken ct = default);
    Task UpdateAsync(long id, UpdateProductCategoryRequest request, bool isActive, long userId, CancellationToken ct = default);
    Task<int> CountActiveProductsAsync(long id, CancellationToken ct = default);
    Task SetActiveAsync(long id, bool isActive, long userId, CancellationToken ct = default);
}

public interface IProductCategoryService
{
    Task<PagedResult<ProductCategoryDto>> GetPagedAsync(ProductCategoryQuery query, CancellationToken ct = default);
    Task<ProductCategoryDto> GetByIdAsync(long id, CancellationToken ct = default);
    Task<ProductCategoryDto> CreateAsync(CreateProductCategoryRequest request, CancellationToken ct = default);
    Task<ProductCategoryDto> UpdateAsync(long id, UpdateProductCategoryRequest request, CancellationToken ct = default);
    Task DeleteAsync(long id, CancellationToken ct = default);
}

public interface IProductRepository
{
    Task<PagedResult<ProductDto>> GetPagedAsync(ProductQuery query, CancellationToken ct = default);
    Task<ProductDto?> GetByIdAsync(long id, CancellationToken ct = default);
    Task<bool> CodeExistsAsync(string code, long? excludeId, CancellationToken ct = default);
    Task<bool> CategoryExistsAsync(long categoryId, CancellationToken ct = default);
    Task<bool> UnitExistsAsync(long unitId, CancellationToken ct = default);
    Task<CreatedResult> CreateAsync(string? code, CreateProductRequest request, long userId, CancellationToken ct = default);
    Task UpdateAsync(long id, UpdateProductRequest request, bool isActive, long userId, CancellationToken ct = default);
    Task<int> CountOpenOrdersAsync(long id, CancellationToken ct = default);
    Task SetActiveAsync(long id, bool isActive, long userId, CancellationToken ct = default);
    Task<ProductPriceLookupDto?> GetPriceAsync(long productId, string customerType, DateTime date, CancellationToken ct = default);
}

public interface IProductService
{
    Task<PagedResult<ProductDto>> GetPagedAsync(ProductQuery query, CancellationToken ct = default);
    Task<ProductDto> GetByIdAsync(long id, CancellationToken ct = default);
    Task<ProductDto> CreateAsync(CreateProductRequest request, CancellationToken ct = default);
    Task<ProductDto> UpdateAsync(long id, UpdateProductRequest request, CancellationToken ct = default);
    Task DeleteAsync(long id, CancellationToken ct = default);
    Task<ProductPriceLookupDto> GetPriceAsync(long id, string? customerType, DateTime? date, CancellationToken ct = default);
}

public interface IProductPriceRepository
{
    Task<PagedResult<ProductPriceDto>> GetPagedAsync(ProductPriceQuery query, CancellationToken ct = default);
    Task<ProductPriceDto?> GetByIdAsync(long id, CancellationToken ct = default);
    Task<bool> ProductExistsAsync(long productId, CancellationToken ct = default);
    Task<bool> HasLaterOverlapAsync(long productId, string customerType, DateTime from, DateTime? to, CancellationToken ct = default);
    Task<long> CreateAsync(CreateProductPriceRequest request, long userId, CancellationToken ct = default);
    Task DeactivateAsync(long id, long userId, CancellationToken ct = default);
}

public interface IProductPriceService
{
    Task<PagedResult<ProductPriceDto>> GetPagedAsync(ProductPriceQuery query, CancellationToken ct = default);
    Task<ProductPriceDto> CreateAsync(CreateProductPriceRequest request, CancellationToken ct = default);
    Task DeleteAsync(long id, CancellationToken ct = default);
}
