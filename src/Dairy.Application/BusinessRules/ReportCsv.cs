using System.Globalization;
using System.Text;
using Dairy.Application.Common;
using Dairy.Application.DTOs.Reports;

namespace Dairy.Application.BusinessRules;

/// <summary>RFC 4180 style CSV (CRLF line ends) for report results. Encode with UTF-8 without BOM.</summary>
public static class ReportCsv
{
    public static string Build(ReportResultDto report, IReadOnlyCollection<string> totalKeys)
    {
        var sb = new StringBuilder();
        var columns = report.Columns;

        sb.Append(string.Join(",", columns.Select(c => Escape(c.Label)))).Append("\r\n");

        foreach (var row in report.Rows)
        {
            sb.Append(string.Join(",", columns.Select(c => Escape(row.TryGetValue(c.Key, out var v) ? v : null)))).Append("\r\n");
        }

        if (report.Totals is { Count: > 0 })
        {
            var columnTotals = columns.Any(c => totalKeys.Contains(c.Key) && report.Totals.ContainsKey(c.Key));
            if (columnTotals)
            {
                var cells = new List<string>();
                for (var i = 0; i < columns.Count; i++)
                {
                    if (i == 0 && !report.Totals.ContainsKey(columns[i].Key)) cells.Add("Total");
                    else cells.Add(report.Totals.TryGetValue(columns[i].Key, out var t) ? Escape(t) : string.Empty);
                }
                sb.Append(string.Join(",", cells)).Append("\r\n");
            }
            else
            {
                // Summary-style totals (e.g. profit & loss): one "label,value" row per entry.
                foreach (var kv in report.Totals)
                {
                    var cells = new List<string> { Escape(B5Text.Humanize(kv.Key)), Escape(kv.Value) };
                    while (cells.Count < columns.Count) cells.Add(string.Empty);
                    sb.Append(string.Join(",", cells)).Append("\r\n");
                }
            }
        }

        return sb.ToString();
    }

    private static string Escape(object? value)
    {
        string text;
        switch (value)
        {
            case null:
                return string.Empty;
            case string s:
                text = s;
                if (text.Length > 0 && (text[0] == '=' || text[0] == '+' || text[0] == '-' || text[0] == '@' || text[0] == '\t' || text[0] == '\r'))
                    text = "'" + text; // neutralize spreadsheet formula injection
                break;
            case bool b:
                text = b ? "Yes" : "No";
                break;
            case IFormattable f:
                text = f.ToString(null, CultureInfo.InvariantCulture);
                break;
            default:
                text = value.ToString() ?? string.Empty;
                break;
        }

        if (text.IndexOfAny(new[] { ',', '"', '\r', '\n' }) >= 0)
            return "\"" + text.Replace("\"", "\"\"") + "\"";
        return text;
    }
}
