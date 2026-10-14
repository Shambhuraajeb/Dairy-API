namespace Dairy.Application.DTOs.Lookups;

public class LookupItemDto
{
    public long Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;

    /// <summary>Optional display hint whose meaning depends on the lookup (e.g. mobile for farmers, unit code for items).</summary>
    public string? Extra { get; set; }
}
