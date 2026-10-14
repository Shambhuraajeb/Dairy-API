using Dairy.Application.Common;
using Dairy.Application.DTOs.Finance;
using Dairy.Application.Interfaces;
using Dairy.Application.Interfaces.Finance;
using Dairy.Application.Requests.Finance;
using Dairy.Domain.Constants;

namespace Dairy.Application.Services.Finance;

/// <summary>sp_RecordExpense / sp_ApproveExpense write the expense and the cash ledger but not the AuditLog; this service audits.</summary>
public class ExpenseService : IExpenseService
{
    private readonly IExpenseRepository _repo;
    private readonly IAuditService _audit;
    private readonly ICurrentUser _user;

    public ExpenseService(IExpenseRepository repo, IAuditService audit, ICurrentUser user)
    {
        _repo = repo;
        _audit = audit;
        _user = user;
    }

    public Task<PagedResult<ExpenseDto>> GetPagedAsync(ExpenseQuery query, CancellationToken ct = default) =>
        _repo.GetPagedAsync(query, ct);

    public async Task<ExpenseDto> GetByIdAsync(long id, CancellationToken ct = default) =>
        await _repo.GetByIdAsync(id, ct) ?? throw new NotFoundException("Expense", id);

    public async Task<ExpenseCreatedDto> CreateAsync(CreateExpenseRequest request, CancellationToken ct = default)
    {
        request.Description = TextClean.Opt(request.Description);
        request.Payee = TextClean.Opt(request.Payee);
        request.ReceiptNo = TextClean.Opt(request.ReceiptNo);

        var created = await _repo.RecordAsync(request, _user.UserId, ct);
        if (request.Payee is not null || request.ReceiptNo is not null)
            await _repo.SetPayeeAndReceiptAsync(created.Id, request.Payee, request.ReceiptNo, ct);

        await _audit.LogAsync(AuditActions.Create, "Expense", created.Id, null,
            new { created.Number, created.Status, request.ExpenseCategoryId, request.ExpenseDate, request.Amount, request.PaymentModeId }, ct);
        if (created.Status == ExpenseStatus.Approved)
            await _audit.LogAsync(AuditActions.Approve, "Expense", created.Id, new { Status = ExpenseStatus.Pending },
                new { created.Status, Note = "Auto-approved (below expense.autoApproveBelow)" }, ct);
        return created;
    }

    public async Task<ExpenseDto> ApproveAsync(long id, ApproveExpenseRequest? request, CancellationToken ct = default)
    {
        var before = await GetByIdAsync(id, ct);
        var remarks = TextClean.Opt(request?.Remarks);

        await _repo.ApproveOrRejectAsync(id, true, remarks, _user.UserId, ct);
        var after = await GetByIdAsync(id, ct);

        await _audit.LogAsync(AuditActions.Approve, "Expense", id,
            new { before.Status }, new { after.Status, after.ExpenseNumber, after.Amount, Remarks = remarks }, ct);
        return after;
    }

    public async Task<ExpenseDto> RejectAsync(long id, RejectExpenseRequest request, CancellationToken ct = default)
    {
        var before = await GetByIdAsync(id, ct);
        var reason = TextClean.Req(request.Reason);

        await _repo.ApproveOrRejectAsync(id, false, reason, _user.UserId, ct);
        var after = await GetByIdAsync(id, ct);

        await _audit.LogAsync(AuditActions.Reject, "Expense", id,
            new { before.Status }, new { after.Status, after.ExpenseNumber, after.Amount, Reason = reason }, ct);
        return after;
    }
}
