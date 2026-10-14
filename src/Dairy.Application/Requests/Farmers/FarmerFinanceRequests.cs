using Dairy.Application.Common;

namespace Dairy.Application.Requests.Farmers;

public class FarmerAdvanceQuery : PagedQuery
{
    /// <summary>Forced to the caller's own farmer for the Farmer role.</summary>
    public long? FarmerId { get; set; }

    /// <summary>Open | Recovered | Cancelled.</summary>
    public string? Status { get; set; }
    public DateTime? DateFrom { get; set; }
    public DateTime? DateTo { get; set; }
}

public class CreateFarmerAdvanceRequest
{
    public long FarmerId { get; set; }
    public DateTime? AdvanceDate { get; set; }
    public decimal Amount { get; set; }

    /// <summary>Optional cap recovered per settlement; null = recover the whole balance.</summary>
    public decimal? RecoveryPerSettlement { get; set; }
    public string? Purpose { get; set; }
    public long? PaymentModeId { get; set; }
    public string? ReferenceNo { get; set; }
    public string? Remarks { get; set; }
}

public class FarmerLoanQuery : PagedQuery
{
    /// <summary>Forced to the caller's own farmer for the Farmer role.</summary>
    public long? FarmerId { get; set; }

    /// <summary>Active | Closed | Cancelled.</summary>
    public string? Status { get; set; }
}

public class CreateFarmerLoanRequest
{
    public long FarmerId { get; set; }
    public decimal PrincipalAmount { get; set; }

    /// <summary>Annual percent.</summary>
    public decimal InterestRate { get; set; }
    public int NumberOfInstallments { get; set; }

    /// <summary>Due date of the first installment.</summary>
    public DateTime? StartDate { get; set; }
    public string? Remarks { get; set; }
}

public class PayLoanInstallmentRequest
{
    /// <summary>Null = pay the remaining balance of the installment.</summary>
    public decimal? Amount { get; set; }
}
