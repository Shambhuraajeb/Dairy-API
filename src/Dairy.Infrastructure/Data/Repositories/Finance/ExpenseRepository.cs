using Dairy.Application.Common;
using Dairy.Application.DTOs.Finance;
using Dairy.Application.Interfaces;
using Dairy.Application.Interfaces.Finance;
using Dairy.Application.Requests.Finance;

namespace Dairy.Infrastructure.Data.Repositories.Finance;

public class ExpenseRepository : IExpenseRepository
{
    private static readonly Dictionary<string, string> SortColumns = new(StringComparer.OrdinalIgnoreCase)
    {
        ["id"] = "e.ExpenseId",
        ["expenseNumber"] = "e.ExpenseNumber",
        ["expenseDate"] = "e.ExpenseDate",
        ["categoryName"] = "c.Name",
        ["amount"] = "e.Amount",
        ["status"] = "e.Status",
        ["createdDate"] = "e.CreatedDate"
    };

    private const string Columns =
        "SELECT e.ExpenseId AS Id, e.ExpenseNumber, e.ExpenseCategoryId, c.Name AS CategoryName, e.ExpenseDate, e.Amount," +
        " e.PaymentModeId, pm.Name AS PaymentModeName, e.Description, e.Payee, e.ReceiptNo, e.BranchId, b.Name AS BranchName," +
        " e.CollectionCenterId, e.Status, e.ApprovedBy, au.DisplayName AS ApprovedByName, e.ApprovedDate, e.RejectionReason," +
        " e.ReferenceType, e.ReferenceId, e.CreatedBy, cu.DisplayName AS CreatedByName, e.CreatedDate";

    private const string From =
        " FROM `Expense` e" +
        " JOIN `ExpenseCategory` c ON c.ExpenseCategoryId = e.ExpenseCategoryId" +
        " LEFT JOIN `PaymentMode` pm ON pm.PaymentModeId = e.PaymentModeId" +
        " LEFT JOIN `Branch` b ON b.BranchId = e.BranchId" +
        " LEFT JOIN `User` cu ON cu.UserId = e.CreatedBy" +
        " LEFT JOIN `User` au ON au.UserId = e.ApprovedBy";

    private const string Where =
        " WHERE e.IsActive = 1" +
        " AND (@Search IS NULL OR e.ExpenseNumber LIKE @Search OR e.Description LIKE @Search OR e.Payee LIKE @Search OR c.Name LIKE @Search)" +
        " AND (@From IS NULL OR e.ExpenseDate >= DATE(@From))" +
        " AND (@To IS NULL OR e.ExpenseDate <= DATE(@To))" +
        " AND (@CategoryId IS NULL OR e.ExpenseCategoryId = @CategoryId)" +
        " AND (@Status IS NULL OR e.Status = @Status)" +
        " AND (@PaymentModeId IS NULL OR e.PaymentModeId = @PaymentModeId)" +
        " AND (@BranchId IS NULL OR e.BranchId = @BranchId)";

    private readonly IDbConnectionFactory _db;

    public ExpenseRepository(IDbConnectionFactory db) => _db = db;

    public async Task<PagedResult<ExpenseDto>> GetPagedAsync(ExpenseQuery query, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        var param = new
        {
            Search = query.SearchPattern,
            query.From,
            query.To,
            query.CategoryId,
            query.Status,
            query.PaymentModeId,
            query.BranchId
        };
        return await conn.QueryPagedAsync<ExpenseDto>(
            Columns + From + Where,
            "SELECT COUNT(*)" + From + Where,
            param, query, SortColumns, "e.ExpenseDate DESC, e.ExpenseId DESC", ct);
    }

    public async Task<ExpenseDto?> GetByIdAsync(long id, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        return await conn.QueryFirstOrNullAsync<ExpenseDto>(Columns + From + " WHERE e.ExpenseId = @Id AND e.IsActive = 1", new { Id = id }, ct);
    }

    public async Task<ExpenseCreatedDto> RecordAsync(CreateExpenseRequest request, long userId, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        var row = await conn.QuerySpFirstOrDefaultAsync<ExpenseCreatedDto>("sp_RecordExpense", new
        {
            p_ExpenseCategoryId = request.ExpenseCategoryId,
            p_ExpenseDate = request.ExpenseDate,
            p_Amount = request.Amount,
            p_PaymentModeId = request.PaymentModeId,
            p_Description = request.Description,
            p_BranchId = request.BranchId,
            p_UserId = userId
        }, ct);
        return row ?? throw new InvalidOperationException("sp_RecordExpense did not return a row.");
    }

    public async Task SetPayeeAndReceiptAsync(long id, string? payee, string? receiptNo, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        await conn.ExecAsync(
            "UPDATE `Expense` SET Payee = COALESCE(@Payee, Payee), ReceiptNo = COALESCE(@ReceiptNo, ReceiptNo) WHERE ExpenseId = @Id",
            new { Id = id, Payee = payee, ReceiptNo = receiptNo }, ct);
    }

    public async Task ApproveOrRejectAsync(long id, bool approve, string? remarks, long userId, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        await conn.QuerySpFirstOrDefaultAsync<ExpenseCreatedDto>("sp_ApproveExpense", new
        {
            p_ExpenseId = id,
            p_UserId = userId,
            p_Approve = approve ? 1 : 0,
            p_Remarks = remarks
        }, ct);
    }
}
