using Dairy.Application.Common;

namespace Dairy.Application.Requests.RateCharts;

public class RateChartQuery : PagedQuery
{
    /// <summary>Cow | Buffalo | Mixed</summary>
    public string? MilkType { get; set; }
    /// <summary>Draft | Active | Expired</summary>
    public string? Status { get; set; }
    public long? CenterId { get; set; }
    public bool? IsActive { get; set; }
}

/// <summary>Body for both POST (create as Draft) and PUT (replace header and all details; Draft only).</summary>
public class SaveRateChartRequest
{
    public string Name { get; set; } = string.Empty;
    /// <summary>Cow | Buffalo | Mixed</summary>
    public string MilkType { get; set; } = "Cow";
    /// <summary>Null = applies to all centers.</summary>
    public long? CollectionCenterId { get; set; }
    public DateTime EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
    /// <summary>Slab | Formula</summary>
    public string CalculationMethod { get; set; } = "Slab";
    public decimal BaseRatePerLiter { get; set; }
    public decimal BaseFat { get; set; }
    public decimal BaseSnf { get; set; }
    public decimal FatRatePerPoint { get; set; }
    public decimal SnfRatePerPoint { get; set; }
    public decimal MinRatePerLiter { get; set; }
    public decimal? MaxRatePerLiter { get; set; }
    public decimal? BonusMinFat { get; set; }
    public decimal? BonusMinSnf { get; set; }
    public decimal QualityBonusPerLiter { get; set; }
    public decimal? PenaltyBelowFat { get; set; }
    public decimal? PenaltyBelowSnf { get; set; }
    public decimal QualityPenaltyPerLiter { get; set; }
    public string? Description { get; set; }
    /// <summary>Slab method: FAT x SNF rate slabs (ignored when CalculationMethod = Formula).</summary>
    public List<RateChartDetailRequest> Details { get; set; } = new();
}

public class RateChartDetailRequest
{
    public decimal FatFrom { get; set; }
    public decimal FatTo { get; set; }
    public decimal SnfFrom { get; set; }
    public decimal SnfTo { get; set; }
    public decimal Rate { get; set; }
    public decimal BonusPerLiter { get; set; }
    public decimal PenaltyPerLiter { get; set; }
}
