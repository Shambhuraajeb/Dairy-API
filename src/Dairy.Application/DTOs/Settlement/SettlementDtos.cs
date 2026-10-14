namespace Dairy.Application.DTOs.Settlement;

/// <summary>Result set 1 of sp_GetFarmerSettlement.</summary>
public class SettlementSummaryDto
{
    public long FarmerId { get; set; }
    public string? FarmerCode { get; set; }
    public string? FarmerName { get; set; }
    public DateTime PeriodFrom { get; set; }
    public DateTime PeriodTo { get; set; }
    public int TotalCollections { get; set; }
    public decimal TotalQuantity { get; set; }
    /// <summary>Sum of collection amounts excluding bonus/penalty.</summary>
    public decimal GrossAmount { get; set; }
    public decimal BonusAmount { get; set; }
    public decimal PenaltyAmount { get; set; }
    public decimal PendingAdvanceBalance { get; set; }
    public decimal LoanInstallmentsDue { get; set; }
    public decimal SuggestedAdvanceDeduction { get; set; }
    public decimal SuggestedLoanDeduction { get; set; }
    public decimal SuggestedDeduction { get; set; }
    /// <summary>Gross + bonus - penalty.</summary>
    public decimal NetBeforeDeduction { get; set; }
    /// <summary>NetBeforeDeduction - suggested deductions.</summary>
    public decimal NetPayable { get; set; }
}

/// <summary>Result set 2: approved, unsettled collections in the period.</summary>
public class SettlementCollectionDto
{
    public long Id { get; set; }
    public string CollectionNumber { get; set; } = string.Empty;
    public DateTime CollectionDate { get; set; }
    public string? ShiftName { get; set; }
    public string? CollectionCenterName { get; set; }
    public string MilkType { get; set; } = string.Empty;
    public decimal QuantityLiters { get; set; }
    public decimal Fat { get; set; }
    public decimal Snf { get; set; }
    public decimal BaseRate { get; set; }
    public decimal FinalRate { get; set; }
    public decimal BonusAmount { get; set; }
    public decimal PenaltyAmount { get; set; }
    public decimal Amount { get; set; }
}

/// <summary>Result set 3: open advances that can be recovered.</summary>
public class SettlementAdvanceDto
{
    public long Id { get; set; }
    public string AdvanceNumber { get; set; } = string.Empty;
    public DateTime AdvanceDate { get; set; }
    public decimal Amount { get; set; }
    public decimal RecoveredAmount { get; set; }
    public decimal BalanceAmount { get; set; }
    public decimal? RecoveryPerSettlement { get; set; }
    public string? Purpose { get; set; }
    public decimal AvailableAmount { get; set; }
    public decimal SuggestedRecovery { get; set; }
}

/// <summary>Result set 4: loan installments due up to the period end.</summary>
public class SettlementInstallmentDto
{
    public long Id { get; set; }
    public long FarmerLoanId { get; set; }
    public string LoanNumber { get; set; } = string.Empty;
    public int InstallmentNo { get; set; }
    public DateTime DueDate { get; set; }
    public decimal Amount { get; set; }
    public decimal PaidAmount { get; set; }
    public string Status { get; set; } = string.Empty;
    public decimal DueAmount { get; set; }
}

public class FarmerSettlementPreviewDto
{
    public SettlementSummaryDto Summary { get; set; } = new();
    public List<SettlementCollectionDto> Collections { get; set; } = new();
    public List<SettlementAdvanceDto> Advances { get; set; } = new();
    public List<SettlementInstallmentDto> LoanInstallments { get; set; } = new();
}

/// <summary>Result of sp_CreateFarmerPayment.</summary>
public class SettlementCreatedDto
{
    public long Id { get; set; }
    public string Number { get; set; } = string.Empty;
    public decimal NetPayable { get; set; }
    public decimal GrossAmount { get; set; }
    public decimal BonusAmount { get; set; }
    public decimal PenaltyAmount { get; set; }
    public decimal DeductionAmount { get; set; }
    public string Status { get; set; } = string.Empty;
}

/// <summary>Result of sp_ApproveFarmerPayment.</summary>
public class SettlementApprovedDto
{
    public long Id { get; set; }
    public string Number { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public decimal NetPayable { get; set; }
}

/// <summary>Result of sp_PayFarmerPayment.</summary>
public class SettlementPaidDto
{
    public long Id { get; set; }
    public string Number { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public decimal PaidAmount { get; set; }
    public decimal BalanceAmount { get; set; }
    public decimal NetPayable { get; set; }
}

/// <summary>Result of sp_CancelFarmerPayment.</summary>
public class SettlementCancelledDto
{
    public long Id { get; set; }
    public string Number { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int ReleasedCollections { get; set; }
}

public class PendingSettlementFarmerDto
{
    public long FarmerId { get; set; }
    public string? FarmerCode { get; set; }
    public string? FarmerName { get; set; }
    public string? Mobile { get; set; }
    public long? CollectionCenterId { get; set; }
    public string? CollectionCenterName { get; set; }
    public int CollectionsCount { get; set; }
    public decimal TotalQuantity { get; set; }
    /// <summary>Sum of collection amounts excluding bonus/penalty.</summary>
    public decimal GrossAmount { get; set; }
    public decimal BonusAmount { get; set; }
    public decimal PenaltyAmount { get; set; }
    /// <summary>Gross + bonus - penalty (before deductions).</summary>
    public decimal TotalAmount { get; set; }
    public DateTime FirstCollectionDate { get; set; }
    public DateTime LastCollectionDate { get; set; }
    public decimal PendingAdvanceBalance { get; set; }
    public decimal LoanInstallmentsDue { get; set; }
}

public class FarmerPaymentListDto
{
    public long Id { get; set; }
    public string PaymentNumber { get; set; } = string.Empty;
    public long FarmerId { get; set; }
    public string? FarmerCode { get; set; }
    public string? FarmerName { get; set; }
    public long? CollectionCenterId { get; set; }
    public string? CollectionCenterName { get; set; }
    public DateTime PaymentDate { get; set; }
    public DateTime PeriodFrom { get; set; }
    public DateTime PeriodTo { get; set; }
    public int TotalCollections { get; set; }
    public decimal TotalQuantity { get; set; }
    public decimal GrossAmount { get; set; }
    public decimal BonusAmount { get; set; }
    public decimal PenaltyAmount { get; set; }
    public decimal DeductionAmount { get; set; }
    public decimal NetPayable { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal BalanceAmount { get; set; }
    /// <summary>Draft | Approved | Paid | Cancelled</summary>
    public string Status { get; set; } = string.Empty;
    public long? PaymentModeId { get; set; }
    public string? PaymentModeName { get; set; }
    public string? ReferenceNo { get; set; }
    public DateTime? PaidDate { get; set; }
    public DateTime? ApprovedDate { get; set; }
    public DateTime CreatedDate { get; set; }
}

public class FarmerPaymentDto : FarmerPaymentListDto
{
    public string? BankName { get; set; }
    public string? BankAccountMasked { get; set; }
    public string? IfscCode { get; set; }
    public string? UpiId { get; set; }
    public long? ApprovedBy { get; set; }
    public string? ApprovedByName { get; set; }
    public long? PaidBy { get; set; }
    public string? PaidByName { get; set; }
    public DateTime? CancelledDate { get; set; }
    public string? CancelReason { get; set; }
    public string? Remarks { get; set; }
    public List<FarmerPaymentLineDto> Details { get; set; } = new();
    public List<FarmerPaymentDeductionDto> Deductions { get; set; } = new();
}

public class FarmerPaymentLineDto
{
    public long Id { get; set; }
    public long MilkCollectionId { get; set; }
    public string? CollectionNumber { get; set; }
    public DateTime CollectionDate { get; set; }
    public string? ShiftName { get; set; }
    public decimal Quantity { get; set; }
    public decimal Rate { get; set; }
    public decimal BonusAmount { get; set; }
    public decimal PenaltyAmount { get; set; }
    public decimal Amount { get; set; }
}

public class FarmerPaymentDeductionDto
{
    public long Id { get; set; }
    /// <summary>Advance | Loan | Other</summary>
    public string DeductionType { get; set; } = string.Empty;
    public long? FarmerAdvanceId { get; set; }
    public string? AdvanceNumber { get; set; }
    public long? FarmerLoanId { get; set; }
    public string? LoanNumber { get; set; }
    public long? FarmerLoanInstallmentId { get; set; }
    public int? InstallmentNo { get; set; }
    public decimal Amount { get; set; }
    public string? Remarks { get; set; }
}
