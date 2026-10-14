using Dairy.Application.Common;
using Dairy.Application.DTOs.Farmers;
using Dairy.Application.Interfaces;
using Dairy.Application.Interfaces.Farmers;
using Dairy.Application.Requests.Farmers;
using Dairy.Domain.Constants;

namespace Dairy.Application.Services.Farmers;

public class FarmerAdvanceService : IFarmerAdvanceService
{
    private readonly IFarmerAdvanceRepository _repo;
    private readonly IFarmerRepository _farmers;
    private readonly IAuditService _audit;
    private readonly ICurrentUser _user;

    public FarmerAdvanceService(IFarmerAdvanceRepository repo, IFarmerRepository farmers, IAuditService audit, ICurrentUser user)
    {
        _repo = repo;
        _farmers = farmers;
        _audit = audit;
        _user = user;
    }

    public Task<PagedResult<FarmerAdvanceDto>> GetPagedAsync(FarmerAdvanceQuery query, CancellationToken ct = default)
    {
        if (_user.IsInRole(Roles.Farmer))
            query.FarmerId = _user.FarmerId ?? throw new ForbiddenException();
        return _repo.GetPagedAsync(query, ct);
    }

    public async Task<FarmerAdvanceDto> GetByIdAsync(long id, CancellationToken ct = default)
    {
        var advance = await _repo.GetByIdAsync(id, ct) ?? throw new NotFoundException("Advance", id);
        if (_user.IsInRole(Roles.Farmer) && advance.FarmerId != _user.FarmerId)
            throw new NotFoundException("Advance", id);
        return advance;
    }

    public async Task<FarmerAdvanceDto> CreateAsync(CreateFarmerAdvanceRequest request, CancellationToken ct = default)
    {
        var farmer = await _farmers.GetBasicAsync(request.FarmerId, ct) ?? throw new NotFoundException("Farmer", request.FarmerId);
        if (farmer.Status != FarmerStatus.Active)
            throw new BusinessRuleException("Advances can only be given to active farmers.");

        var advanceDate = (request.AdvanceDate ?? DateTime.Today).Date;
        if (advanceDate > DateTime.Today)
            throw new RequestValidationException("advanceDate", "Advance date cannot be in the future.");

        var id = await _repo.CreateAsync(request, advanceDate, _user.UserId, ct);
        // sp_CreateFarmerAdvance writes the audit row itself.
        return await _repo.GetByIdAsync(id, ct) ?? throw new NotFoundException("Advance", id);
    }
}
