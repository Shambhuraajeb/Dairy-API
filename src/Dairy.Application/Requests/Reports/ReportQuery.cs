namespace Dairy.Application.Requests.Reports;

/// <summary>Query-string filters shared by all reports; each report uses the subset listed in the catalog.</summary>
public class ReportQuery
{
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }
    public long? BranchId { get; set; }
    public long? CollectionCenterId { get; set; }
    public long? FarmerId { get; set; }
    public long? CustomerId { get; set; }
    public long? ProductId { get; set; }
    public long? ShiftId { get; set; }

    /// <summary>Inventory item id (stock, purchase reports).</summary>
    public long? ItemId { get; set; }

    /// <summary>Item category (inventory reports) or expense category (expenses report).</summary>
    public long? CategoryId { get; set; }
    public string? Status { get; set; }

    /// <summary>"csv" returns a CSV file instead of JSON.</summary>
    public string? Format { get; set; }

    public bool IsCsv => string.Equals(Format, "csv", StringComparison.OrdinalIgnoreCase);
}
