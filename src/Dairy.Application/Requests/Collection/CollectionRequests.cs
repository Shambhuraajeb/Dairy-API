using Dairy.Application.Common;

namespace Dairy.Application.Requests.Collection;

public class ShiftListQuery
{
    public long? CenterId { get; set; }
    public bool? IsActive { get; set; }
}

public class ShiftDashboardQuery
{
    /// <summary>Defaults to today.</summary>
    public DateTime? Date { get; set; }
    public long? CenterId { get; set; }
    /// <summary>Defaults to the current (or most recently started) shift.</summary>
    public long? ShiftId { get; set; }
}

public class MilkCollectionQuery : PagedQuery
{
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }
    public long? CenterId { get; set; }
    public long? ShiftId { get; set; }
    public long? FarmerId { get; set; }
    /// <summary>Pending | Approved | Rejected</summary>
    public string? Status { get; set; }
    /// <summary>Unsettled | Settled</summary>
    public string? SettlementStatus { get; set; }
}

public class MilkRejectionQuery : PagedQuery
{
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }
    public long? CenterId { get; set; }
    public long? FarmerId { get; set; }
    /// <summary>LowFat | LowSnf | Adulterated | HighTemperature | Sour | Other</summary>
    public string? ReasonCode { get; set; }
}

public class RatePreviewQuery
{
    public long CenterId { get; set; }
    /// <summary>Defaults to today.</summary>
    public DateTime? Date { get; set; }
    /// <summary>Cow | Buffalo | Mixed (default Cow).</summary>
    public string? MilkType { get; set; }
    public decimal Fat { get; set; }
    public decimal Snf { get; set; }
    public decimal? Clr { get; set; }
    public decimal? Quantity { get; set; }
}

public class CreateMilkCollectionRequest
{
    /// <summary>Defaults to today; cannot be in the future.</summary>
    public DateTime? CollectionDate { get; set; }
    public long ShiftId { get; set; }
    public long CollectionCenterId { get; set; }
    public long FarmerId { get; set; }
    /// <summary>Cow | Buffalo | Mixed. Defaults to the farmer's milk type.</summary>
    public string? MilkType { get; set; }
    public decimal QuantityLiters { get; set; }
    public decimal Fat { get; set; }
    public decimal Snf { get; set; }
    public decimal? Clr { get; set; }
    public decimal? Temperature { get; set; }
    public int? CanCount { get; set; }
    public long? MilkTankId { get; set; }
    public decimal? Acidity { get; set; }
    public bool WaterAdded { get; set; }
    public string? Remarks { get; set; }
}

/// <summary>Null quantity/fat/snf/clr keep the current value; at least one must change.</summary>
public class AdjustMilkCollectionRequest
{
    public decimal? QuantityLiters { get; set; }
    public decimal? Fat { get; set; }
    public decimal? Snf { get; set; }
    public decimal? Clr { get; set; }
    public string Reason { get; set; } = string.Empty;
}

public class RejectMilkCollectionRequest
{
    public string Reason { get; set; } = string.Empty;
    /// <summary>Null = reject the whole collection; otherwise a partial rejection of this quantity.</summary>
    public decimal? QuantityLiters { get; set; }
}

public class BulkApproveRequest
{
    public List<long> Ids { get; set; } = new();
}

public class RecordQualityRequest
{
    /// <summary>Default: the collection's FAT.</summary>
    public decimal? Fat { get; set; }
    /// <summary>Default: the collection's SNF.</summary>
    public decimal? Snf { get; set; }
    public decimal? Clr { get; set; }
    public decimal? Protein { get; set; }
    public decimal? Lactose { get; set; }
    public decimal? Temperature { get; set; }
    public decimal? Acidity { get; set; }
    public bool WaterAdded { get; set; }
    public bool StarchDetected { get; set; }
    public bool UreaDetected { get; set; }
    public bool NeutralizerDetected { get; set; }
    public bool AntibioticDetected { get; set; }
    public string? OtherAdulteration { get; set; }
    /// <summary>Pass | Fail. Default: Fail when any adulteration is flagged, else Pass.</summary>
    public string? Result { get; set; }
    public string? FailureReason { get; set; }
    public string? Remarks { get; set; }
}

public class MilkTankQuery : PagedQuery
{
    public long? CenterId { get; set; }
    /// <summary>Active | Maintenance | Inactive</summary>
    public string? Status { get; set; }
    public bool? IsActive { get; set; }
}

public class CreateMilkTankRequest
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public long CollectionCenterId { get; set; }
    public decimal CapacityLiters { get; set; }
    /// <summary>Cow | Buffalo | Mixed</summary>
    public string? MilkType { get; set; }
    public decimal? MinTemperature { get; set; }
    public decimal? MaxTemperature { get; set; }
    public decimal? CurrentTemperature { get; set; }
}

public class UpdateMilkTankRequest
{
    public string Name { get; set; } = string.Empty;
    public long CollectionCenterId { get; set; }
    public decimal CapacityLiters { get; set; }
    public string? MilkType { get; set; }
    public decimal? MinTemperature { get; set; }
    public decimal? MaxTemperature { get; set; }
    public decimal? CurrentTemperature { get; set; }
    /// <summary>Active | Maintenance | Inactive</summary>
    public string Status { get; set; } = "Active";
    public DateTime? LastCleanedDate { get; set; }
    /// <summary>Null = unchanged; false = deactivate.</summary>
    public bool? IsActive { get; set; }
}

public class TankTransactionQuery : PagedQuery
{
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }
    /// <summary>In | Out | Adjust</summary>
    public string? Type { get; set; }
}

public class CreateTankTransactionRequest
{
    /// <summary>Out | Adjust</summary>
    public string TransactionType { get; set; } = string.Empty;
    /// <summary>Out: positive liters removed. Adjust: signed delta (negative reduces).</summary>
    public decimal Quantity { get; set; }
    public string? Destination { get; set; }
    public string? Remarks { get; set; }
}
