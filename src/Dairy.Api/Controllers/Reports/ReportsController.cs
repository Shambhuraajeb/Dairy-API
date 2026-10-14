using Dairy.Api.Authorization;
using Dairy.Application.Interfaces.Reports;
using Dairy.Application.Requests.Reports;
using Dairy.Domain.Constants;
using Microsoft.AspNetCore.Mvc;

namespace Dairy.Api.Controllers.Reports;

[Route("api/reports")]
[HasPermission(Permissions.ReportView)]
public class ReportsController : ApiControllerBase
{
    private readonly IReportService _reports;

    public ReportsController(IReportService reports) => _reports = reports;

    [HttpGet("catalog")]
    public async Task<IActionResult> Catalog(CancellationToken ct) =>
        Ok(await _reports.GetCatalogAsync(ct));

    /// <summary>Runs a report; add <c>?format=csv</c> to download a CSV file instead of JSON.</summary>
    [HttpGet("{code}")]
    public async Task<IActionResult> Run(string code, [FromQuery] ReportQuery query, CancellationToken ct)
    {
        if (query.IsCsv)
        {
            var file = await _reports.ExportCsvAsync(code, query, ct);
            return File(file.Content, file.ContentType, file.FileName);
        }
        return Ok(await _reports.RunAsync(code, query, ct));
    }
}
