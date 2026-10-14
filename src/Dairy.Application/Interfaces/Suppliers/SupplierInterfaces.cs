using Dairy.Application.Common;
using Dairy.Application.DTOs.Suppliers;
using Dairy.Application.Requests.Suppliers;

namespace Dairy.Application.Interfaces.Suppliers;

public interface ISupplierRepository
{
    Task<PagedResult<SupplierDto>> GetPagedAsync(SupplierQuery query, CancellationToken ct = default);
    Task<SupplierDto?> GetByIdAsync(long id, CancellationToken ct = default);
    Task<bool> CodeExistsAsync(string code, long? excludeId, CancellationToken ct = default);
    Task<long> CreateAsync(CreateSupplierRequest request, long userId, CancellationToken ct = default);
    Task UpdateAsync(long id, UpdateSupplierRequest request, bool isActive, long userId, CancellationToken ct = default);
}

public interface ISupplierService
{
    Task<PagedResult<SupplierDto>> GetPagedAsync(SupplierQuery query, CancellationToken ct = default);
    Task<SupplierDto> GetByIdAsync(long id, CancellationToken ct = default);
    Task<SupplierDto> CreateAsync(CreateSupplierRequest request, CancellationToken ct = default);
    Task<SupplierDto> UpdateAsync(long id, UpdateSupplierRequest request, CancellationToken ct = default);
}
