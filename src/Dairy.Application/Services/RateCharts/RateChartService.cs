using Dairy.Application.Common;
using Dairy.Application.DTOs.Collection;
using Dairy.Application.DTOs.RateCharts;
using Dairy.Application.Interfaces;
using Dairy.Application.Interfaces.RateCharts;
using Dairy.Application.Requests.Collection;
using Dairy.Application.Requests.RateCharts;
using Dairy.Domain.Constants;

namespace Dairy.Application.Services.RateCharts;

public class RateChartService : IRateChartService
{
    private static readonly string[] MilkTypes = { "Cow", "Buffalo", "Mixed" };
    private static readonly string[] Methods = { "Slab", "Formula" };

    private readonly IRateChartRepository _repo;
    private readonly IAuditService _audit;
    private readonly ICurrentUser _user;

    public RateChartService(IRateChartRepository repo, IAuditService audit, ICurrentUser user)
    {
        _repo = repo;
        _audit = audit;
        _user = user;
    }

    public Task<PagedResult<RateChartListDto>> GetPagedAsync(RateChartQuery query, CancellationToken ct = default) =>
        _repo.GetPagedAsync(query, ct);

    public async Task<RateChartDto> GetByIdAsync(long id, CancellationToken ct = default) =>
        await _repo.GetByIdAsync(id, ct) ?? throw new NotFoundException("Rate chart", id);

    public async Task<RateChartDto> CreateAsync(SaveRateChartRequest request, CancellationToken ct = default)
    {
        await NormalizeAndCheckAsync(request, ct);
        var id = await _repo.CreateAsync(request, _user.UserId, ct);
        var created = await _repo.GetByIdAsync(id, ct) ?? throw new NotFoundException("Rate chart", id);
        await _audit.LogAsync(AuditActions.Create, "RateChart", id, null, created, ct);
        return created;
    }

    public async Task<RateChartDto> UpdateAsync(long id, SaveRateChartRequest request, CancellationToken ct = default)
    {
        var existing = await _repo.GetByIdAsync(id, ct) ?? throw new NotFoundException("Rate chart", id);
        if (existing.Status != "Draft" || !existing.IsActive)
            throw new BusinessRuleException("Only draft rate charts can be edited. Create a new chart to change an active one.");

        await NormalizeAndCheckAsync(request, ct);
        if (!await _repo.UpdateDraftAsync(id, request, _user.UserId, ct))
            throw new BusinessRuleException("Only draft rate charts can be edited. Create a new chart to change an active one.");

        var updated = await _repo.GetByIdAsync(id, ct) ?? throw new NotFoundException("Rate chart", id);
        await _audit.LogAsync(AuditActions.Update, "RateChart", id, existing, updated, ct);
        return updated;
    }

    public async Task<RateChartActivationDto> ActivateAsync(long id, CancellationToken ct = default)
    {
        var existing = await _repo.GetByIdAsync(id, ct) ?? throw new NotFoundException("Rate chart", id);
        if (!existing.IsActive)
            throw new BusinessRuleException("A deactivated rate chart cannot be activated.");
        if (existing.Status == "Active")
            throw new BusinessRuleException("The rate chart is already active.");
        if (existing.Status != "Draft")
            throw new BusinessRuleException("Only draft rate charts can be activated.");
        if (existing.EffectiveTo.HasValue && existing.EffectiveTo.Value.Date < DateTime.Today)
            throw new BusinessRuleException("The effective period of this rate chart has already ended.");

        if (existing.CalculationMethod == "Slab" && existing.DetailCount == 0)
            throw new BusinessRuleException("Add at least one FAT/SNF rate slab before activating the chart.");
        if (existing.CalculationMethod == "Formula" && existing.BaseRatePerLiter <= 0)
            throw new BusinessRuleException("The base rate per liter must be greater than zero before activating the chart.");

        var superseded = await _repo.ActivateAsync(id, _user.UserId, ct);
        var activated = await _repo.GetByIdAsync(id, ct) ?? throw new NotFoundException("Rate chart", id);

        await _audit.LogAsync(AuditActions.Activate, "RateChart", id,
            new { existing.Status }, new { activated.Status, SupersededChartIds = superseded }, ct);

        return new RateChartActivationDto { Chart = activated, SupersededChartIds = superseded };
    }

    public async Task<bool> DeleteAsync(long id, CancellationToken ct = default)
    {
        var existing = await _repo.GetByIdAsync(id, ct) ?? throw new NotFoundException("Rate chart", id);

        if (existing.Status == "Draft")
        {
            await _repo.DeleteDraftAsync(id, ct);
            await _audit.LogAsync(AuditActions.Delete, "RateChart", id, existing, null, ct);
            return true;
        }

        if (!existing.IsActive)
            throw new BusinessRuleException("The rate chart is already deactivated.");

        await _repo.DeactivateAsync(id, _user.UserId, ct);
        var after = await _repo.GetByIdAsync(id, ct);
        await _audit.LogAsync(AuditActions.Deactivate, "RateChart", id, existing, after, ct);
        return false;
    }

    public Task<RatePreviewDto> PreviewRateAsync(RatePreviewQuery query, CancellationToken ct = default)
    {
        var milkType = Canon(query.MilkType, MilkTypes) ?? "Cow";
        return _repo.PreviewRateAsync(query.CenterId, (query.Date ?? DateTime.Today).Date, milkType,
            query.Fat, query.Snf, query.Clr, query.Quantity, ct);
    }

    private async Task NormalizeAndCheckAsync(SaveRateChartRequest r, CancellationToken ct)
    {
        r.Name = r.Name.Trim();
        r.MilkType = Canon(r.MilkType, MilkTypes) ?? "Cow";
        r.CalculationMethod = Canon(r.CalculationMethod, Methods) ?? "Slab";
        r.EffectiveFrom = r.EffectiveFrom.Date;
        r.EffectiveTo = r.EffectiveTo?.Date;
        r.Description = string.IsNullOrWhiteSpace(r.Description) ? null : r.Description.Trim();
        if (r.CalculationMethod == "Formula")
            r.Details = new List<RateChartDetailRequest>();

        if (r.CollectionCenterId.HasValue && !await _repo.CenterExistsAsync(r.CollectionCenterId.Value, ct))
            throw new RequestValidationException("collectionCenterId", "Collection center not found.");
    }

    private static string? Canon(string? value, string[] allowed) =>
        string.IsNullOrWhiteSpace(value)
            ? null
            : allowed.FirstOrDefault(a => string.Equals(a, value.Trim(), StringComparison.OrdinalIgnoreCase));
}
