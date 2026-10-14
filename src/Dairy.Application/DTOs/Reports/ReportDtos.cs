namespace Dairy.Application.DTOs.Reports;

public class ReportColumnDto
{
    public string Key { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;

    /// <summary>text | number | money | date | datetime | boolean</summary>
    public string Type { get; set; } = "text";
}

public class ReportCatalogItemDto
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Group { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    /// <summary>Supported query-string filters (from, to, branchId, collectionCenterId, farmerId, customerId, productId, status, shiftId, itemId, categoryId).</summary>
    public List<string> Filters { get; set; } = new();
    public List<ReportColumnDto> Columns { get; set; } = new();
}

public class ReportResultDto
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public List<ReportColumnDto> Columns { get; set; } = new();

    /// <summary>One dictionary per row; keys are camelCase column aliases; dates are "yyyy-MM-dd" / "yyyy-MM-ddTHH:mm:ss" strings.</summary>
    public List<Dictionary<string, object?>> Rows { get; set; } = new();

    /// <summary>Column sums (or the P&amp;L summary); null when the report has none.</summary>
    public Dictionary<string, object?>? Totals { get; set; }

    /// <summary>Additional named row sets (profit-loss: expenseByCategory); null otherwise.</summary>
    public Dictionary<string, List<Dictionary<string, object?>>>? Extra { get; set; }
}

public class ReportFileDto
{
    public byte[] Content { get; set; } = Array.Empty<byte>();
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = "text/csv; charset=utf-8";
}

/// <summary>Raw repository output before shaping (keys already camelCase, values as read from MySQL).</summary>
public class ReportData
{
    public List<Dictionary<string, object?>> Rows { get; set; } = new();
    public Dictionary<string, object?>? Summary { get; set; }
    public Dictionary<string, List<Dictionary<string, object?>>>? Extra { get; set; }
}
