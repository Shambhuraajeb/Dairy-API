namespace Dairy.Application.DTOs.RateCharts;

public class RateChartListDto
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string MilkType { get; set; } = string.Empty;
    public long? CollectionCenterId { get; set; }
    public string? CollectionCenterName { get; set; }
    public DateTime EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
    /// <summary>Draft | Active | Expired</summary>
    public string Status { get; set; } = string.Empty;
    /// <summary>Slab | Formula</summary>
    public string CalculationMethod { get; set; } = string.Empty;
    public decimal BaseRatePerLiter { get; set; }
    public decimal BaseFat { get; set; }
    public decimal BaseSnf { get; set; }
    public decimal FatRatePerPoint { get; set; }
    public decimal SnfRatePerPoint { get; set; }
    public decimal MinRatePerLiter { get; set; }
    public decimal? MaxRatePerLiter { get; set; }
    public int DetailCount { get; set; }
    public DateTime? ActivatedDate { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedDate { get; set; }
}

public class RateChartDto : RateChartListDto
{
    public decimal? BonusMinFat { get; set; }
    public decimal? BonusMinSnf { get; set; }
    public decimal QualityBonusPerLiter { get; set; }
    public decimal? PenaltyBelowFat { get; set; }
    public decimal? PenaltyBelowSnf { get; set; }
    public decimal QualityPenaltyPerLiter { get; set; }
    public string? Description { get; set; }
    public long? ActivatedBy { get; set; }
    public List<RateChartDetailDto> Details { get; set; } = new();
}

public class RateChartDetailDto
{
    public long Id { get; set; }
    public decimal FatFrom { get; set; }
    public decimal FatTo { get; set; }
    public decimal SnfFrom { get; set; }
    public decimal SnfTo { get; set; }
    public decimal Rate { get; set; }
    public decimal BonusPerLiter { get; set; }
    public decimal PenaltyPerLiter { get; set; }
}

/// <summary>Result of activating a chart: the chart plus the ids of charts that were superseded.</summary>
public class RateChartActivationDto
{
    public RateChartDto Chart { get; set; } = new();
    public List<long> SupersededChartIds { get; set; } = new();
}
