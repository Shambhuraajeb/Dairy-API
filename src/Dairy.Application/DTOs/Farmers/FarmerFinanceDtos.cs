namespace Dairy.Application.DTOs.Farmers;

public class FarmerAdvanceDto
{
    public long Id { get; set; }
    public string AdvanceNumber { get; set; } = string.Empty;
    public long FarmerId { get; set; }
    public string FarmerCode { get; set; } = string.Empty;
    public string FarmerName { get; set; } = string.Empty;
    public DateTime AdvanceDate { get; set; }
    public decimal Amount { get; set; }
    public decimal RecoveredAmount { get; set; }
    public decimal BalanceAmount { get; set; }
    public decimal? RecoveryPerSettlement { get; set; }
    public string? Purpose { get; set; }
    public long? PaymentModeId { get; set; }
    public string? PaymentModeName { get; set; }
    public string? ReferenceNo { get; set; }

    /// <summary>Open | Recovered | Cancelled.</summary>
    public string Status { get; set; } = string.Empty;
    public DateTime? ApprovedDate { get; set; }
    public string? Remarks { get; set; }
    public DateTime CreatedDate { get; set; }
}

public class FarmerLoanListDto
{
    public long Id { get; set; }
    public string LoanNumber { get; set; } = string.Empty;
    public long FarmerId { get; set; }
    public string FarmerCode { get; set; } = string.Empty;
    public string FarmerName { get; set; } = string.Empty;
    public DateTime LoanDate { get; set; }
    public decimal PrincipalAmount { get; set; }
    public decimal InterestRate { get; set; }
    public decimal InterestAmount { get; set; }
    public decimal TotalPayable { get; set; }
    public int NumberOfInstallments { get; set; }
    public decimal InstallmentAmount { get; set; }
    public DateTime FirstInstallmentDate { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal OutstandingAmount { get; set; }

    /// <summary>Active | Closed | Cancelled.</summary>
    public string Status { get; set; } = string.Empty;
    public string? Purpose { get; set; }
    public int PaidInstallments { get; set; }
    public DateTime? NextDueDate { get; set; }
}

public class FarmerLoanDto : FarmerLoanListDto
{
    public string InterestType { get; set; } = string.Empty;
    public long? PaymentModeId { get; set; }
    public string? PaymentModeName { get; set; }
    public string? ReferenceNo { get; set; }
    public DateTime? ApprovedDate { get; set; }
    public DateTime? ClosedDate { get; set; }
    public string? Remarks { get; set; }
    public DateTime CreatedDate { get; set; }
    public List<FarmerLoanInstallmentDto> Installments { get; set; } = new();
}

public class FarmerLoanInstallmentDto
{
    public long Id { get; set; }
    public long FarmerLoanId { get; set; }
    public int InstallmentNo { get; set; }
    public DateTime DueDate { get; set; }
    public decimal Amount { get; set; }
    public decimal PrincipalComponent { get; set; }
    public decimal InterestComponent { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal BalanceAmount { get; set; }
    public DateTime? PaidDate { get; set; }

    /// <summary>Pending | Partial | Paid.</summary>
    public string Status { get; set; } = string.Empty;
    public bool IsOverdue { get; set; }
    public string? PaymentSource { get; set; }
    public long? FarmerPaymentId { get; set; }
    public string? Remarks { get; set; }
}
