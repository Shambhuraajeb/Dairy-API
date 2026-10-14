using Dairy.Application.Common;
using Dairy.Application.DTOs.Settlement;
using Dairy.Application.Interfaces;
using Dairy.Application.Interfaces.Settlement;
using Dairy.Application.Requests.Settlement;
using Dairy.Domain.Constants;

namespace Dairy.Application.Services.Settlement;

/// <summary>
/// Farmer settlement and payments. The stored procedures write the audit trail for create / approve / pay / cancel,
/// so this service deliberately does not call IAuditService.
/// </summary>
public class FarmerSettlementService : IFarmerSettlementService
{
    private readonly IFarmerSettlementRepository _repo;
    private readonly ICurrentUser _user;

    public FarmerSettlementService(IFarmerSettlementRepository repo, ICurrentUser user)
    {
        _repo = repo;
        _user = user;
    }

    public Task<FarmerSettlementPreviewDto> PreviewAsync(SettlementPreviewQuery query, CancellationToken ct = default)
    {
        var farmerId = query.FarmerId;
        if (_user.IsInRole(Roles.Farmer))
            farmerId = _user.FarmerId ?? throw new ForbiddenException();
        return _repo.PreviewAsync(farmerId, query.From.Date, query.To.Date, ct);
    }

    public Task<List<PendingSettlementFarmerDto>> GetPendingFarmersAsync(PendingFarmersQuery query, CancellationToken ct = default)
    {
        if (_user.IsInRole(Roles.Farmer))
            throw new ForbiddenException();
        query.From = query.From.Date;
        query.To = query.To.Date;
        return _repo.GetPendingFarmersAsync(query, ct);
    }

    public Task<SettlementCreatedDto> CreateAsync(CreateSettlementRequest request, CancellationToken ct = default)
    {
        request.From = request.From.Date;
        request.To = request.To.Date;
        request.ReferenceNo = string.IsNullOrWhiteSpace(request.ReferenceNo) ? null : request.ReferenceNo.Trim();
        request.Remarks = string.IsNullOrWhiteSpace(request.Remarks) ? null : request.Remarks.Trim();
        request.OtherDeductionRemarks = string.IsNullOrWhiteSpace(request.OtherDeductionRemarks) ? null : request.OtherDeductionRemarks.Trim();
        return _repo.CreateAsync(request, _user.UserId, ct);
    }

    public async Task<SettlementApprovedDto> ApproveAsync(long id, CancellationToken ct = default)
    {
        await EnsureExistsAsync(id, ct);
        return await _repo.ApproveAsync(id, _user.UserId, ct);
    }

    public async Task<SettlementPaidDto> PayAsync(long id, PaySettlementRequest request, CancellationToken ct = default)
    {
        await EnsureExistsAsync(id, ct);
        request.ReferenceNo = string.IsNullOrWhiteSpace(request.ReferenceNo) ? null : request.ReferenceNo.Trim();
        return await _repo.PayAsync(id, request, _user.UserId, ct);
    }

    public async Task<SettlementCancelledDto> CancelAsync(long id, CancelSettlementRequest request, CancellationToken ct = default)
    {
        await EnsureExistsAsync(id, ct);
        return await _repo.CancelAsync(id, request.Reason.Trim(), _user.UserId, ct);
    }

    public Task<PagedResult<FarmerPaymentListDto>> GetPaymentsAsync(FarmerPaymentQuery query, CancellationToken ct = default)
    {
        if (_user.IsInRole(Roles.Farmer))
            query.FarmerId = _user.FarmerId ?? throw new ForbiddenException();
        return _repo.GetPaymentsAsync(query, ct);
    }

    public async Task<FarmerPaymentDto> GetPaymentByIdAsync(long id, CancellationToken ct = default)
    {
        var dto = await _repo.GetPaymentByIdAsync(id, ct) ?? throw new NotFoundException("Farmer payment", id);
        if (_user.IsInRole(Roles.Farmer) && dto.FarmerId != _user.FarmerId)
            throw new NotFoundException("Farmer payment", id);
        return dto;
    }

    private async Task EnsureExistsAsync(long id, CancellationToken ct)
    {
        if (await _repo.GetPaymentFarmerIdAsync(id, ct) is null)
            throw new NotFoundException("Farmer payment", id);
    }
}
