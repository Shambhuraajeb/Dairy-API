using Dairy.Application.Common;

namespace Dairy.Application.Requests.Settlement;

public class SettlementPreviewQuery
{
    public long FarmerId { get; set; }
    public DateTime From { get; set; }
    public DateTime To { get; set; }
}

public class PendingFarmersQuery
{
    public DateTime From { get; set; }
    public DateTime To { get; set; }
    public long? CenterId { get; set; }
    public string? Search { get; set; }
}

public class CreateSettlementRequest
{
    public long FarmerId { get; set; }
    public DateTime From { get; set; }
    public DateTime To { get; set; }
    /// <summary>Extra bonus on top of the per-collection bonuses.</summary>
    public decimal? BonusAmount { get; set; }
    /// <summary>Extra penalty on top of the per-collection penalties.</summary>
    public decimal? PenaltyAmount { get; set; }
    public decimal? AdvanceDeduction { get; set; }
    public bool IncludeLoanInstallments { get; set; } = true;
    public decimal? OtherDeduction { get; set; }
    /// <summary>Required when OtherDeduction &gt; 0.</summary>
    public string? OtherDeductionRemarks { get; set; }
    public long? PaymentModeId { get; set; }
    public string? ReferenceNo { get; set; }
    public string? Remarks { get; set; }
}

public class PaySettlementRequest
{
    /// <summary>Defaults to the mode saved on the settlement.</summary>
    public long? PaymentModeId { get; set; }
    public string? ReferenceNo { get; set; }
    /// <summary>Defaults to the full outstanding balance.</summary>
    public decimal? PaidAmount { get; set; }
}

public class CancelSettlementRequest
{
    public string Reason { get; set; } = string.Empty;
}

public class FarmerPaymentQuery : PagedQuery
{
    public long? FarmerId { get; set; }
    public long? CenterId { get; set; }
    /// <summary>Draft | Approved | Paid | Cancelled</summary>
    public string? Status { get; set; }
    /// <summary>Filters PaymentDate (settlement date) &gt;= From.</summary>
    public DateTime? From { get; set; }
    /// <summary>Filters PaymentDate (settlement date) &lt;= To.</summary>
    public DateTime? To { get; set; }
}
