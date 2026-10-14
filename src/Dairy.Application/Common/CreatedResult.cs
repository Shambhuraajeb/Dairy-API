namespace Dairy.Application.Common;

/// <summary>Result of a creating stored procedure: one row with column Id (and optionally Number).</summary>
public class CreatedResult
{
    public long Id { get; set; }
    public string? Number { get; set; }
}
