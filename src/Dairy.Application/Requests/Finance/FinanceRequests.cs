using Dairy.Application.Common;

namespace Dairy.Application.Requests.Finance;

public class ExpenseCategoryQuery : PagedQuery
{
    public bool? IsActive { get; set; }
}

public class CreateExpenseCategoryRequest
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
}

public class UpdateExpenseCategoryRequest : CreateExpenseCategoryRequest
{
    /// <summary>Null = leave unchanged; false = deactivate.</summary>
    public bool? IsActive { get; set; }
}

public class ExpenseQuery : PagedQuery
{
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }
    public long? CategoryId { get; set; }

    /// <summary>Pending | Approved | Rejected</summary>
    public string? Status { get; set; }

    public long? PaymentModeId { get; set; }
    public long? BranchId { get; set; }
}

public class CreateExpenseRequest
{
    public long ExpenseCategoryId { get; set; }

    /// <summary>Null = today. Cannot be in the future.</summary>
    public DateTime? ExpenseDate { get; set; }

    public decimal Amount { get; set; }
    public long? PaymentModeId { get; set; }
    public string? Description { get; set; }

    /// <summary>Null = the user's own branch.</summary>
    public long? BranchId { get; set; }

    public string? Payee { get; set; }
    public string? ReceiptNo { get; set; }
}

public class ApproveExpenseRequest
{
    public string? Remarks { get; set; }
}

public class RejectExpenseRequest
{
    public string Reason { get; set; } = string.Empty;
}

public class CashTransactionQuery : PagedQuery
{
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }

    /// <summary>In | Out</summary>
    public string? Type { get; set; }

    public string? Category { get; set; }
    public long? PaymentModeId { get; set; }
}
