using Dairy.Application.Common;
using Dairy.Application.DTOs.Lookups;
using Dairy.Application.Interfaces;
using Dairy.Application.Interfaces.Lookups;
using Dairy.Domain.Constants;

namespace Dairy.Application.Services.Lookups;

public class LookupService : ILookupService
{
    private readonly ILookupRepository _repo;
    private readonly ICurrentUser _user;

    public LookupService(ILookupRepository repo, ICurrentUser user)
    {
        _repo = repo;
        _user = user;
    }

    public async Task<List<LookupItemDto>> GetAsync(string name, string? search, long? parentId, string? status, CancellationToken ct = default)
    {
        var key = (name ?? string.Empty).Trim().ToLowerInvariant();
        var pattern = new PagedQuery { Search = search }.SearchPattern;

        // Farmers and customers only ever see themselves in the farmer/customer lookups.
        long? ownerId = null;
        if (key == "farmers" && _user.IsInRole(Roles.Farmer))
            ownerId = _user.FarmerId ?? throw new ForbiddenException();
        else if (key == "customers" && Roles.IsCustomerRole(_user.Role))
            ownerId = _user.CustomerId ?? throw new ForbiddenException();

        var statusFilter = string.IsNullOrWhiteSpace(status) ? null : status.Trim();
        var items = await _repo.GetAsync(key, pattern, parentId, statusFilter, ownerId, ct);
        return items ?? throw new NotFoundException($"Lookup '{name}' was not found.");
    }
}
