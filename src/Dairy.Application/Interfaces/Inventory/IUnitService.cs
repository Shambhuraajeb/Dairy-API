using Dairy.Application.Common;
using Dairy.Application.DTOs.Inventory;
using Dairy.Application.Requests.Inventory;

namespace Dairy.Application.Interfaces.Inventory;

public interface IUnitService
{
    Task<PagedResult<UnitDto>> GetPagedAsync(UnitQuery query, CancellationToken ct = default);
    Task<UnitDto> GetByIdAsync(long id, CancellationToken ct = default);
    Task<UnitDto> CreateAsync(CreateUnitRequest request, CancellationToken ct = default);
    Task<UnitDto> UpdateAsync(long id, UpdateUnitRequest request, CancellationToken ct = default);
}
