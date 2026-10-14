using Dairy.Application.Common;
using Dairy.Application.DTOs.Collection;
using Dairy.Application.Interfaces;
using Dairy.Application.Interfaces.Collection;
using Dairy.Application.Requests.Collection;
using Dairy.Domain.Constants;

namespace Dairy.Application.Services.Collection;

public class MilkRejectionService : IMilkRejectionService
{
    private readonly IMilkRejectionRepository _repo;
    private readonly ICurrentUser _user;

    public MilkRejectionService(IMilkRejectionRepository repo, ICurrentUser user)
    {
        _repo = repo;
        _user = user;
    }

    public Task<PagedResult<MilkRejectionDto>> GetPagedAsync(MilkRejectionQuery query, CancellationToken ct = default)
    {
        if (_user.IsInRole(Roles.Farmer))
            query.FarmerId = _user.FarmerId ?? throw new ForbiddenException();
        return _repo.GetPagedAsync(query, ct);
    }
}
