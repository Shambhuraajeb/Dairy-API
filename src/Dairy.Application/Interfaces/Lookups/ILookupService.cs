using Dairy.Application.DTOs.Lookups;

namespace Dairy.Application.Interfaces.Lookups;

public interface ILookupService
{
    Task<List<LookupItemDto>> GetAsync(string name, string? search, long? parentId, string? status, CancellationToken ct = default);
}
