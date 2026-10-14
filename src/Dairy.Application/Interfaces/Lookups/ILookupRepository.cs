using Dairy.Application.DTOs.Lookups;

namespace Dairy.Application.Interfaces.Lookups;

public interface ILookupRepository
{
    /// <summary>Returns null when <paramref name="name"/> is not a known lookup.</summary>
    Task<List<LookupItemDto>?> GetAsync(string name, string? searchPattern, long? parentId, string? status, long? ownerId, CancellationToken ct = default);
}
