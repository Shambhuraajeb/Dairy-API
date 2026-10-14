using Dairy.Application.Common;
using Dairy.Application.DTOs.Collection;
using Dairy.Application.Interfaces;
using Dairy.Application.Interfaces.Collection;
using Dairy.Application.Requests.Collection;
using Dairy.Domain.Constants;
using MySqlConnector;

namespace Dairy.Application.Services.Collection;

public class MilkCollectionService : IMilkCollectionService
{
    private readonly IMilkCollectionRepository _repo;
    private readonly IAuditService _audit;
    private readonly ICurrentUser _user;

    public MilkCollectionService(IMilkCollectionRepository repo, IAuditService audit, ICurrentUser user)
    {
        _repo = repo;
        _audit = audit;
        _user = user;
    }

    public Task<PagedResult<MilkCollectionListDto>> GetPagedAsync(MilkCollectionQuery query, CancellationToken ct = default)
    {
        ApplyOwnership(query);
        return _repo.GetPagedAsync(query, ct);
    }

    public Task<PagedResult<MilkCollectionListDto>> GetPendingApprovalsAsync(MilkCollectionQuery query, CancellationToken ct = default)
    {
        if (_user.IsInRole(Roles.Farmer))
            throw new ForbiddenException();
        query.Status = MilkCollectionStatus.Pending;
        return _repo.GetPagedAsync(query, ct);
    }

    public async Task<MilkCollectionDetailDto> GetByIdAsync(long id, CancellationToken ct = default)
    {
        var dto = await _repo.GetByIdAsync(id, ct) ?? throw new NotFoundException("Milk collection", id);
        if (_user.IsInRole(Roles.Farmer) && dto.FarmerId != _user.FarmerId)
            throw new NotFoundException("Milk collection", id);
        return dto;
    }

    public Task<MilkCollectionCreatedDto> CreateAsync(CreateMilkCollectionRequest request, CancellationToken ct = default)
    {
        request.MilkType = string.IsNullOrWhiteSpace(request.MilkType) ? null : request.MilkType.Trim();
        request.Remarks = string.IsNullOrWhiteSpace(request.Remarks) ? null : request.Remarks.Trim();
        // sp_CreateMilkCollection validates farmer/center/shift/ranges, calculates the rate and writes quality + rate rows atomically.
        return _repo.CreateAsync(request, _user.UserId, ct);
    }

    public async Task<MilkCollectionDetailDto> AdjustAsync(long id, AdjustMilkCollectionRequest request, CancellationToken ct = default)
    {
        _ = await _repo.GetStateAsync(id, ct) ?? throw new NotFoundException("Milk collection", id);
        request.Reason = request.Reason.Trim();
        // The procedure keeps original + adjusted values and writes the audit trail.
        await _repo.AdjustAsync(id, request, _user.UserId, ct);
        return await _repo.GetByIdAsync(id, ct) ?? throw new NotFoundException("Milk collection", id);
    }

    public async Task<MilkCollectionApprovedDto> ApproveAsync(long id, CancellationToken ct = default)
    {
        _ = await _repo.GetStateAsync(id, ct) ?? throw new NotFoundException("Milk collection", id);
        return await _repo.ApproveAsync(id, _user.UserId, ct);
    }

    public async Task<MilkCollectionRejectedDto> RejectAsync(long id, RejectMilkCollectionRequest request, CancellationToken ct = default)
    {
        _ = await _repo.GetStateAsync(id, ct) ?? throw new NotFoundException("Milk collection", id);
        return await _repo.RejectAsync(id, request.Reason.Trim(), request.QuantityLiters, _user.UserId, ct);
    }

    public async Task<BulkApproveResultDto> BulkApproveAsync(BulkApproveRequest request, CancellationToken ct = default)
    {
        var result = new BulkApproveResultDto();
        foreach (var id in request.Ids.Distinct())
        {
            ct.ThrowIfCancellationRequested();
            try
            {
                var approved = await _repo.ApproveAsync(id, _user.UserId, ct);
                result.Results.Add(new BulkApproveItemDto { Id = id, Success = true, Number = approved.Number, Message = "Approved." });
            }
            catch (MySqlException ex) when (ex.SqlState == "45000")
            {
                result.Results.Add(new BulkApproveItemDto { Id = id, Success = false, Message = ex.Message });
            }
            catch (BusinessRuleException ex)
            {
                result.Results.Add(new BulkApproveItemDto { Id = id, Success = false, Message = ex.Message });
            }
        }

        result.Total = result.Results.Count;
        result.Approved = result.Results.Count(r => r.Success);
        result.Failed = result.Total - result.Approved;
        return result;
    }

    public async Task<List<MilkQualityTestDto>> GetQualityTestsAsync(long id, CancellationToken ct = default)
    {
        var state = await _repo.GetStateAsync(id, ct) ?? throw new NotFoundException("Milk collection", id);
        if (_user.IsInRole(Roles.Farmer) && state.FarmerId != _user.FarmerId)
            throw new NotFoundException("Milk collection", id);
        return await _repo.GetQualityTestsAsync(id, ct);
    }

    public async Task<MilkQualityTestDto> RecordQualityAsync(long id, RecordQualityRequest request, CancellationToken ct = default)
    {
        var state = await _repo.GetStateAsync(id, ct) ?? throw new NotFoundException("Milk collection", id);
        if (state.Status != MilkCollectionStatus.Pending)
            throw new BusinessRuleException("Quality results can only be recorded for pending collections.");

        var flags = new List<string>();
        if (request.WaterAdded) flags.Add("Water added");
        if (request.StarchDetected) flags.Add("Starch detected");
        if (request.UreaDetected) flags.Add("Urea detected");
        if (request.NeutralizerDetected) flags.Add("Neutralizer detected");
        if (request.AntibioticDetected) flags.Add("Antibiotic detected");
        if (!string.IsNullOrWhiteSpace(request.OtherAdulteration)) flags.Add(request.OtherAdulteration.Trim());
        var adulterated = flags.Count > 0;

        string result;
        if (string.IsNullOrWhiteSpace(request.Result))
            result = adulterated ? "Fail" : "Pass";
        else
            result = string.Equals(request.Result.Trim(), "Fail", StringComparison.OrdinalIgnoreCase) ? "Fail" : "Pass";

        if (result == "Pass" && adulterated)
            throw new BusinessRuleException("A sample with detected adulteration cannot be marked as Pass.");

        string? failureReason = null;
        if (result == "Fail")
        {
            failureReason = !string.IsNullOrWhiteSpace(request.FailureReason)
                ? request.FailureReason.Trim()
                : adulterated ? string.Join(", ", flags) : "Failed quality test";
            if (failureReason.Length > 255) failureReason = failureReason[..255];
        }

        var fat = request.Fat ?? state.Fat;
        var snf = request.Snf ?? state.Snf;

        var outcome = await _repo.RecordQualityAsync(id, request, fat, snf, result, failureReason, _user.UserId, ct);
        await _audit.LogAsync(AuditActions.Update, "MilkQualityTest", outcome.Current.Id, outcome.Previous, outcome.Current, ct);
        return outcome.Current;
    }

    private void ApplyOwnership(MilkCollectionQuery query)
    {
        if (_user.IsInRole(Roles.Farmer))
            query.FarmerId = _user.FarmerId ?? throw new ForbiddenException();
    }
}
