namespace Dairy.Application.DTOs.Finance;

public class ExpenseCategoryDto
{
    public long Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public int ExpenseCount { get; set; }
}

public class ExpenseDto
{
    public long Id { get; set; }
    public string ExpenseNumber { get; set; } = string.Empty;
    public long ExpenseCategoryId { get; set; }
    public string? CategoryName { get; set; }
    public DateTime ExpenseDate { get; set; }
    public decimal Amount { get; set; }
    public long? PaymentModeId { get; set; }
    public string? PaymentModeName { get; set; }
    public string? Description { get; set; }
    public string? Payee { get; set; }
    public string? ReceiptNo { get; set; }
    public long? BranchId { get; set; }
    public string? BranchName { get; set; }
    public long? CollectionCenterId { get; set; }

    /// <summary>Pending | Approved | Rejected</summary>
    public string Status { get; set; } = string.Empty;

    public long? ApprovedBy { get; set; }
    public string? ApprovedByName { get; set; }
    public DateTime? ApprovedDate { get; set; }
    public string? RejectionReason { get; set; }
    public string? ReferenceType { get; set; }
    public long? ReferenceId { get; set; }
    public long? CreatedBy { get; set; }
    public string? CreatedByName { get; set; }
    public DateTime CreatedDate { get; set; }
}

/// <summary>Result row of sp_RecordExpense.</summary>
public class ExpenseCreatedDto
{
    public long Id { get; set; }
    public string? Number { get; set; }
    public string? Status { get; set; }
}

public class CashTransactionDto
{
    public long Id { get; set; }
    public DateTime TransactionDate { get; set; }

    /// <summary>In | Out</summary>
    public string TransactionType { get; set; } = string.Empty;

    public string Category { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public decimal BalanceAfter { get; set; }
    public long? PaymentModeId { get; set; }
    public string? PaymentModeName { get; set; }
    public long? BranchId { get; set; }
    public string? BranchName { get; set; }

    /// <summary>Expense | CustomerPayment | FarmerPayment | Purchase | Salary | Manual</summary>
    public string? ReferenceType { get; set; }

    public long? ReferenceId { get; set; }
    public string? Remarks { get; set; }
    public long? CreatedBy { get; set; }
    public string? CreatedByName { get; set; }
}

public class CashSummaryDto
{
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }
    public decimal OpeningBalance { get; set; }
    public decimal TotalIn { get; set; }
    public decimal TotalOut { get; set; }
    public decimal NetChange { get; set; }
    public decimal ClosingBalance { get; set; }
    public int TransactionCount { get; set; }
    public List<CashCategorySummaryDto> ByCategory { get; set; } = new();
}

public class CashCategorySummaryDto
{
    public string Category { get; set; } = string.Empty;
    public decimal TotalIn { get; set; }
    public decimal TotalOut { get; set; }
}

/// <summary>Internal totals row used by the cash summary query.</summary>
public class CashTotalsRow
{
    public decimal TotalIn { get; set; }
    public decimal TotalOut { get; set; }
    public int TransactionCount { get; set; }
}
