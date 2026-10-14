using System.Globalization;
using System.Text;
using Dairy.Application.BusinessRules;
using Dairy.Application.Common;
using Dairy.Application.DTOs.Reports;
using Dairy.Application.Interfaces.Reports;
using Dairy.Application.Requests.Reports;

namespace Dairy.Application.Services.Reports;

public class ReportService : IReportService
{
    private static readonly UTF8Encoding Utf8NoBom = new(false);

    private readonly IReportRepository _repo;

    public ReportService(IReportRepository repo) => _repo = repo;

    public Task<List<ReportCatalogItemDto>> GetCatalogAsync(CancellationToken ct = default)
    {
        var items = ReportCatalog.All.Select(d => new ReportCatalogItemDto
        {
            Code = d.Code,
            Name = d.Name,
            Group = d.Group,
            Description = d.Description,
            Filters = d.Filters.ToList(),
            Columns = d.Columns.Select(c => new ReportColumnDto { Key = c.Key, Label = c.Label, Type = c.Type }).ToList()
        }).ToList();
        return Task.FromResult(items);
    }

    public async Task<ReportResultDto> RunAsync(string code, ReportQuery query, CancellationToken ct = default)
    {
        var def = Resolve(code);
        if (query.From.HasValue && query.To.HasValue && query.From.Value.Date > query.To.Value.Date)
            throw new RequestValidationException("from", "'From' date must be on or before the 'To' date.");

        var raw = await _repo.RunAsync(def.Code, query, ct);
        return Shape(def, raw);
    }

    public async Task<ReportFileDto> ExportCsvAsync(string code, ReportQuery query, CancellationToken ct = default)
    {
        var def = Resolve(code);
        var result = await RunAsync(code, query, ct);
        var csv = ReportCsv.Build(result, def.TotalKeys);
        return new ReportFileDto
        {
            Content = Utf8NoBom.GetBytes(csv),
            FileName = $"{def.Code}-{DateTime.Now:yyyyMMdd-HHmmss}.csv",
            ContentType = "text/csv; charset=utf-8"
        };
    }

    private static ReportDefinition Resolve(string code) =>
        ReportCatalog.Find(code) ?? throw new NotFoundException($"Report '{code}' was not found.");

    private static ReportResultDto Shape(ReportDefinition def, ReportData raw)
    {
        var rows = raw.Rows;

        // Columns: catalog hints, limited to keys the query actually returned.
        List<ReportColumnDto> columns;
        if (rows.Count == 0)
        {
            columns = def.Columns.Select(Clone).ToList();
        }
        else
        {
            var present = rows[0].Keys.ToHashSet(StringComparer.Ordinal);
            columns = def.Columns.Where(c => present.Contains(c.Key)).Select(Clone).ToList();
            if (columns.Count == 0)
            {
                columns = rows[0].Keys.Where(k => k != "id")
                    .Select(k => new ReportColumnDto { Key = k, Label = B5Text.Humanize(k), Type = Infer(rows, k) })
                    .ToList();
            }
        }

        var types = columns.ToDictionary(c => c.Key, c => c.Type, StringComparer.Ordinal);

        // Totals are computed from the raw numeric values.
        Dictionary<string, object?>? totals = null;
        if (raw.Summary != null)
        {
            totals = raw.Summary.ToDictionary(kv => kv.Key, kv => NormalizeValue(kv.Value, null), StringComparer.Ordinal);
        }
        else if (def.TotalKeys.Length > 0)
        {
            totals = new Dictionary<string, object?>(StringComparer.Ordinal);
            foreach (var key in def.TotalKeys)
            {
                if (!types.ContainsKey(key)) continue;
                decimal sum = 0;
                foreach (var row in rows)
                {
                    if (row.TryGetValue(key, out var v) && v != null && v is IConvertible)
                        sum += Convert.ToDecimal(v, CultureInfo.InvariantCulture);
                }
                totals[key] = Math.Round(sum, types[key] == "money" ? 2 : 3);
            }
            if (totals.Count == 0) totals = null;
        }

        var shaped = new List<Dictionary<string, object?>>(rows.Count);
        foreach (var row in rows)
        {
            var outRow = new Dictionary<string, object?>(row.Count, StringComparer.Ordinal);
            foreach (var kv in row)
                outRow[kv.Key] = NormalizeValue(kv.Value, types.TryGetValue(kv.Key, out var t) ? t : null);
            shaped.Add(outRow);
        }

        Dictionary<string, List<Dictionary<string, object?>>>? extra = null;
        if (raw.Extra != null)
        {
            extra = raw.Extra.ToDictionary(
                kv => kv.Key,
                kv => kv.Value.Select(r => r.ToDictionary(x => x.Key, x => NormalizeValue(x.Value, null), StringComparer.Ordinal)).ToList(),
                StringComparer.Ordinal);
        }

        return new ReportResultDto
        {
            Code = def.Code,
            Name = def.Name,
            Columns = columns,
            Rows = shaped,
            Totals = totals,
            Extra = extra
        };
    }

    private static ReportColumnDto Clone(ReportColumnDto c) => new() { Key = c.Key, Label = c.Label, Type = c.Type };

    private static string Infer(List<Dictionary<string, object?>> rows, string key)
    {
        foreach (var row in rows)
        {
            if (!row.TryGetValue(key, out var v) || v == null) continue;
            return v switch
            {
                bool => "boolean",
                DateTime dt => dt.TimeOfDay == TimeSpan.Zero ? "date" : "datetime",
                DateOnly => "date",
                decimal or double or float or long or int or short or byte or sbyte or ulong or uint or ushort => "number",
                _ => "text"
            };
        }
        return "text";
    }

    private static object? NormalizeValue(object? value, string? type)
    {
        switch (value)
        {
            case null:
                return null;
            case DateTime dt:
                var asDate = type == "date" || (type == null && dt.TimeOfDay == TimeSpan.Zero);
                return dt.ToString(asDate ? "yyyy-MM-dd" : "yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture);
            case DateOnly d:
                return d.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            case TimeSpan ts:
                return ts.ToString(@"hh\:mm\:ss", CultureInfo.InvariantCulture);
            case TimeOnly t:
                return t.ToString("HH:mm:ss", CultureInfo.InvariantCulture);
            case byte[]:
                return null;
            case bool:
                return value;
            case double dbl when double.IsNaN(dbl) || double.IsInfinity(dbl):
                return null;
            default:
                if (type == "boolean" && value is IConvertible && value is not string)
                    return Convert.ToDecimal(value, CultureInfo.InvariantCulture) != 0;
                return value;
        }
    }
}
