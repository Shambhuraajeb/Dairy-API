namespace Dairy.Application.DTOs.Collection;

public class CollectionShiftDto
{
    public long Id { get; set; }
    public long? CollectionCenterId { get; set; }
    public string? CollectionCenterName { get; set; }
    public string ShiftName { get; set; } = string.Empty;
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; }
}

/// <summary>Aggregate of MilkCollection for one (date, shift, center) slice.</summary>
public class ShiftSummaryDto
{
    public long? ShiftId { get; set; }
    public string? ShiftName { get; set; }
    public TimeSpan? StartTime { get; set; }
    public TimeSpan? EndTime { get; set; }
    public long? CollectionCenterId { get; set; }
    public string? CollectionCenterName { get; set; }
    public int FarmersServed { get; set; }
    public int TotalCollections { get; set; }
    public decimal TotalLiters { get; set; }
    public decimal AcceptedLiters { get; set; }
    public decimal RejectedLiters { get; set; }
    public decimal AvgFat { get; set; }
    public decimal AvgSnf { get; set; }
    public int AcceptedCount { get; set; }
    public int RejectedCount { get; set; }
    public int ApprovedCount { get; set; }
    public int PendingApprovals { get; set; }
    public decimal TotalAmount { get; set; }
}

public class CollectionDashboardDto
{
    public DateTime Date { get; set; }
    public long? CollectionCenterId { get; set; }
    public long? ShiftId { get; set; }
    public string? ShiftName { get; set; }
    public TimeSpan? StartTime { get; set; }
    public TimeSpan? EndTime { get; set; }
    public bool IsCurrentShift { get; set; }
    public ShiftSummaryDto Summary { get; set; } = new();
    public List<ShiftSummaryDto> Shifts { get; set; } = new();
    /// <summary>All pending (unapproved) collections, any date, for the selected center.</summary>
    public int TotalPendingApprovals { get; set; }
}

public class MilkCollectionListDto
{
    public long Id { get; set; }
    public string CollectionNumber { get; set; } = string.Empty;
    public DateTime CollectionDate { get; set; }
    public long ShiftId { get; set; }
    public string? ShiftName { get; set; }
    public long CollectionCenterId { get; set; }
    public string? CollectionCenterName { get; set; }
    public long FarmerId { get; set; }
    public string? FarmerCode { get; set; }
    public string? FarmerName { get; set; }
    public string MilkType { get; set; } = string.Empty;
    public decimal QuantityLiters { get; set; }
    public decimal Fat { get; set; }
    public decimal Snf { get; set; }
    public decimal? Clr { get; set; }
    /// <summary>Base rate per liter from the chart.</summary>
    public decimal Rate { get; set; }
    /// <summary>Amount / quantity (base + bonus - penalty per liter).</summary>
    public decimal FinalRate { get; set; }
    public decimal BonusAmount { get; set; }
    public decimal PenaltyAmount { get; set; }
    public decimal Amount { get; set; }
    public string? QualityResult { get; set; }
    public string Status { get; set; } = string.Empty;
    public string SettlementStatus { get; set; } = string.Empty;
    public bool AdjustedFlag { get; set; }
    public long? FarmerPaymentId { get; set; }
    public DateTime CreatedDate { get; set; }
}

public class MilkCollectionDetailDto : MilkCollectionListDto
{
    public decimal? Temperature { get; set; }
    public int? CanCount { get; set; }
    public long? RateChartId { get; set; }
    public string? RateChartName { get; set; }
    public long? TankId { get; set; }
    public string? TankName { get; set; }

    public decimal? OriginalQuantity { get; set; }
    public decimal? OriginalFat { get; set; }
    public decimal? OriginalSnf { get; set; }
    public decimal? OriginalClr { get; set; }
    public decimal? OriginalRate { get; set; }
    public decimal? OriginalAmount { get; set; }
    public string? AdjustmentReason { get; set; }
    public long? AdjustedBy { get; set; }
    public string? AdjustedByName { get; set; }
    public DateTime? AdjustedDate { get; set; }

    public long? ApprovedBy { get; set; }
    public string? ApprovedByName { get; set; }
    public DateTime? ApprovedDate { get; set; }
    public long? RejectedBy { get; set; }
    public string? RejectedByName { get; set; }
    public DateTime? RejectedDate { get; set; }
    public string? RejectionReason { get; set; }

    public long? CollectedBy { get; set; }
    public string? CollectedByName { get; set; }
    public string? Remarks { get; set; }

    public List<MilkQualityTestDto> QualityTests { get; set; } = new();
    public MilkRateBreakdownDto? RateBreakdown { get; set; }
    public List<MilkRejectionDto> Rejections { get; set; } = new();
}

public class MilkQualityTestDto
{
    public long Id { get; set; }
    public long MilkCollectionId { get; set; }
    public decimal Fat { get; set; }
    public decimal Snf { get; set; }
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
    public string Result { get; set; } = string.Empty;
    public string? FailureReason { get; set; }
    public long? TestedBy { get; set; }
    public string? TestedByName { get; set; }
    public DateTime TestDate { get; set; }
    public string? Remarks { get; set; }
}

public class MilkRateBreakdownDto
{
    public long RateChartId { get; set; }
    public string? RateChartName { get; set; }
    public string? CalculationMethod { get; set; }
    public long? RateChartDetailId { get; set; }
    public decimal? SlabFatFrom { get; set; }
    public decimal? SlabFatTo { get; set; }
    public decimal? SlabSnfFrom { get; set; }
    public decimal? SlabSnfTo { get; set; }
    public decimal FatUsed { get; set; }
    public decimal SnfUsed { get; set; }
    public decimal QuantityUsed { get; set; }
    public decimal BaseRate { get; set; }
    public decimal Bonus { get; set; }
    public decimal Penalty { get; set; }
    public decimal FinalRate { get; set; }
    public decimal BonusAmount { get; set; }
    public decimal PenaltyAmount { get; set; }
    public DateTime CalculatedDate { get; set; }
}

public class MilkRejectionDto
{
    public long Id { get; set; }
    public long MilkCollectionId { get; set; }
    public string? CollectionNumber { get; set; }
    public DateTime? CollectionDate { get; set; }
    public long FarmerId { get; set; }
    public string? FarmerCode { get; set; }
    public string? FarmerName { get; set; }
    public long? CollectionCenterId { get; set; }
    public string? CollectionCenterName { get; set; }
    public DateTime RejectionDate { get; set; }
    public string ReasonCode { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public decimal QuantityLiters { get; set; }
    public string? DisposalMethod { get; set; }
    public long? RejectedBy { get; set; }
    public string? RejectedByName { get; set; }
    public DateTime RejectedDate { get; set; }
    public string? Remarks { get; set; }
}

/// <summary>Result of sp_CreateMilkCollection.</summary>
public class MilkCollectionCreatedDto
{
    public long Id { get; set; }
    public string Number { get; set; } = string.Empty;
    /// <summary>Final rate per liter (base + bonus - penalty).</summary>
    public decimal Rate { get; set; }
    public decimal BaseRate { get; set; }
    public decimal BonusAmount { get; set; }
    public decimal PenaltyAmount { get; set; }
    public decimal Amount { get; set; }
    public string Status { get; set; } = string.Empty;
}

/// <summary>Result of sp_ApproveMilkCollection.</summary>
public class MilkCollectionApprovedDto
{
    public long Id { get; set; }
    public string Number { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
}

/// <summary>Result of sp_RejectMilkCollection (Id = MilkRejection id).</summary>
public class MilkCollectionRejectedDto
{
    public long Id { get; set; }
    public long MilkCollectionId { get; set; }
    public string Number { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public decimal RejectedQuantity { get; set; }
}

public class BulkApproveItemDto
{
    public long Id { get; set; }
    public bool Success { get; set; }
    public string? Number { get; set; }
    public string Message { get; set; } = string.Empty;
}

public class BulkApproveResultDto
{
    public int Total { get; set; }
    public int Approved { get; set; }
    public int Failed { get; set; }
    public List<BulkApproveItemDto> Results { get; set; } = new();
}

/// <summary>Result of sp_PreviewMilkRate.</summary>
public class RatePreviewDto
{
    public long RateChartId { get; set; }
    public string? RateChartName { get; set; }
    public long? RateChartDetailId { get; set; }
    public decimal BaseRate { get; set; }
    public decimal Bonus { get; set; }
    public decimal Penalty { get; set; }
    public decimal FinalRate { get; set; }
    public decimal Amount { get; set; }
}

public class MilkTankDto
{
    public long Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public long CollectionCenterId { get; set; }
    public string? CollectionCenterName { get; set; }
    public decimal CapacityLiters { get; set; }
    public decimal CurrentQuantity { get; set; }
    public decimal AvailableCapacity { get; set; }
    public decimal FillPercent { get; set; }
    public string? MilkType { get; set; }
    public decimal? MinTemperature { get; set; }
    public decimal? MaxTemperature { get; set; }
    public decimal? CurrentTemperature { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime? LastCleanedDate { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedDate { get; set; }
}

public class TankTransactionDto
{
    public long Id { get; set; }
    public long TankId { get; set; }
    public string TransactionType { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal BalanceAfter { get; set; }
    public long? MilkCollectionId { get; set; }
    public string? CollectionNumber { get; set; }
    public DateTime TransactionDate { get; set; }
    public string? Destination { get; set; }
    public string? ReferenceType { get; set; }
    public long? ReferenceId { get; set; }
    public string? Remarks { get; set; }
    public long? CreatedBy { get; set; }
    public string? CreatedByName { get; set; }
}
