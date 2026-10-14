using Dairy.Application.Common;
using Dairy.Application.DTOs.Collection;
using Dairy.Application.DTOs.RateCharts;
using Dairy.Application.Requests.Collection;
using Dairy.Application.Requests.RateCharts;

namespace Dairy.Application.Interfaces.RateCharts;

public interface IRateChartRepository
{
    Task<PagedResult<RateChartListDto>> GetPagedAsync(RateChartQuery query, CancellationToken ct = default);
    Task<RateChartDto?> GetByIdAsync(long id, CancellationToken ct = default);
    Task<bool> CenterExistsAsync(long centerId, CancellationToken ct = default);
    /// <summary>Inserts a Draft chart with its slabs (one DB transaction). Returns the new id.</summary>
    Task<long> CreateAsync(SaveRateChartRequest request, long userId, CancellationToken ct = default);
    /// <summary>Replaces header and slabs of a Draft chart (one DB transaction). Returns false when the chart is not a Draft.</summary>
    Task<bool> UpdateDraftAsync(long id, SaveRateChartRequest request, long userId, CancellationToken ct = default);
    /// <summary>Activates a Draft chart and supersedes overlapping charts for the same center / milk type. Returns superseded ids.</summary>
    Task<List<long>> ActivateAsync(long id, long userId, CancellationToken ct = default);
    Task DeleteDraftAsync(long id, CancellationToken ct = default);
    Task DeactivateAsync(long id, long userId, CancellationToken ct = default);
    Task<RatePreviewDto> PreviewRateAsync(long centerId, DateTime date, string milkType, decimal fat, decimal snf,
        decimal? clr, decimal? quantity, CancellationToken ct = default);
}

public interface IRateChartService
{
    Task<PagedResult<RateChartListDto>> GetPagedAsync(RateChartQuery query, CancellationToken ct = default);
    Task<RateChartDto> GetByIdAsync(long id, CancellationToken ct = default);
    Task<RateChartDto> CreateAsync(SaveRateChartRequest request, CancellationToken ct = default);
    Task<RateChartDto> UpdateAsync(long id, SaveRateChartRequest request, CancellationToken ct = default);
    Task<RateChartActivationDto> ActivateAsync(long id, CancellationToken ct = default);
    /// <summary>Draft charts are deleted; others are deactivated. Returns true when hard-deleted.</summary>
    Task<bool> DeleteAsync(long id, CancellationToken ct = default);
    Task<RatePreviewDto> PreviewRateAsync(RatePreviewQuery query, CancellationToken ct = default);
}
