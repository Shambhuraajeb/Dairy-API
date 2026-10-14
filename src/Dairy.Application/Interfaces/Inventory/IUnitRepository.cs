using Dairy.Application.Common;
using Dairy.Application.DTOs.Inventory;
using Dairy.Application.Requests.Inventory;

namespace Dairy.Application.Interfaces.Inventory;

public interface IUnitRepository
{
    Task<PagedResult<UnitDto>> GetPagedAsync(UnitQuery query, CancellationToken ct = default);
    Task<UnitDto?> GetByIdAsync(long id, CancellationToken ct = default);
    Task<bool> CodeExistsAsync(string code, long? excludeId, CancellationToken ct = default);
    Task<long> CreateAsync(string code, string name, long userId, CancellationToken ct = default);
    Task UpdateAsync(long id, string code, string name, bool isActive, long userId, CancellationToken ct = default);
}
