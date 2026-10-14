using Dairy.Application.DTOs.Reports;
using Dairy.Application.Requests.Reports;

namespace Dairy.Application.Interfaces.Reports;

public interface IReportRepository
{
    /// <summary>Runs the report with the given (already validated, lower-case) code.</summary>
    Task<ReportData> RunAsync(string code, ReportQuery query, CancellationToken ct = default);
}

public interface IReportService
{
    Task<List<ReportCatalogItemDto>> GetCatalogAsync(CancellationToken ct = default);
    Task<ReportResultDto> RunAsync(string code, ReportQuery query, CancellationToken ct = default);
    Task<ReportFileDto> ExportCsvAsync(string code, ReportQuery query, CancellationToken ct = default);
}
