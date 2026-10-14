using Dairy.Application.Common;
using Dairy.Application.DTOs.Finance;
using Dairy.Application.Requests.Finance;

namespace Dairy.Application.Interfaces.Finance;

// ---------- expense categories ----------

public interface IExpenseCategoryRepository
{
    Task<PagedResult<ExpenseCategoryDto>> GetPagedAsync(ExpenseCategoryQuery query, CancellationToken ct = default);
    Task<ExpenseCategoryDto?> GetByIdAsync(long id, CancellationToken ct = default);
    Task<bool> ExistsAsync(string code, string name, long? excludeId, CancellationToken ct = default);
    Task<long> CreateAsync(CreateExpenseCategoryRequest request, long userId, CancellationToken ct = default);
    Task UpdateAsync(long id, UpdateExpenseCategoryRequest request, bool isActive, long userId, CancellationToken ct = default);
}

public interface IExpenseCategoryService
{
    Task<PagedResult<ExpenseCategoryDto>> GetPagedAsync(ExpenseCategoryQuery query, CancellationToken ct = default);
    Task<ExpenseCategoryDto> GetByIdAsync(long id, CancellationToken ct = default);
    Task<ExpenseCategoryDto> CreateAsync(CreateExpenseCategoryRequest request, CancellationToken ct = default);
    Task<ExpenseCategoryDto> UpdateAsync(long id, UpdateExpenseCategoryRequest request, CancellationToken ct = default);
}

// ---------- expenses ----------

public interface IExpenseRepository
{
    Task<PagedResult<ExpenseDto>> GetPagedAsync(ExpenseQuery query, CancellationToken ct = default);
    Task<ExpenseDto?> GetByIdAsync(long id, CancellationToken ct = default);
    Task<ExpenseCreatedDto> RecordAsync(CreateExpenseRequest request, long userId, CancellationToken ct = default);

    /// <summary>Sets Payee / ReceiptNo on a freshly recorded expense (the procedure does not take them).</summary>
    Task SetPayeeAndReceiptAsync(long id, string? payee, string? receiptNo, CancellationToken ct = default);

    Task ApproveOrRejectAsync(long id, bool approve, string? remarks, long userId, CancellationToken ct = default);
}

public interface IExpenseService
{
    Task<PagedResult<ExpenseDto>> GetPagedAsync(ExpenseQuery query, CancellationToken ct = default);
    Task<ExpenseDto> GetByIdAsync(long id, CancellationToken ct = default);
    Task<ExpenseCreatedDto> CreateAsync(CreateExpenseRequest request, CancellationToken ct = default);
    Task<ExpenseDto> ApproveAsync(long id, ApproveExpenseRequest? request, CancellationToken ct = default);
    Task<ExpenseDto> RejectAsync(long id, RejectExpenseRequest request, CancellationToken ct = default);
}

// ---------- cash ----------

public interface ICashTransactionRepository
{
    Task<PagedResult<CashTransactionDto>> GetPagedAsync(CashTransactionQuery query, CancellationToken ct = default);
    Task<CashSummaryDto> GetSummaryAsync(DateTime? from, DateTime? to, CancellationToken ct = default);
}

public interface ICashTransactionService
{
    Task<PagedResult<CashTransactionDto>> GetPagedAsync(CashTransactionQuery query, CancellationToken ct = default);
    Task<CashSummaryDto> GetSummaryAsync(DateTime? from, DateTime? to, CancellationToken ct = default);
}
