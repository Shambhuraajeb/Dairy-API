using Dairy.Application.Common;
using Dairy.Application.DTOs.Finance;
using Dairy.Application.Interfaces;
using Dairy.Application.Interfaces.Finance;
using Dairy.Application.Requests.Finance;
using Dairy.Domain.Constants;

namespace Dairy.Application.Services.Finance;

public class ExpenseCategoryService : IExpenseCategoryService
{
    private readonly IExpenseCategoryRepository _repo;
    private readonly IAuditService _audit;
    private readonly ICurrentUser _user;

    public ExpenseCategoryService(IExpenseCategoryRepository repo, IAuditService audit, ICurrentUser user)
    {
        _repo = repo;
        _audit = audit;
        _user = user;
    }

    public Task<PagedResult<ExpenseCategoryDto>> GetPagedAsync(ExpenseCategoryQuery query, CancellationToken ct = default) =>
        _repo.GetPagedAsync(query, ct);

    public async Task<ExpenseCategoryDto> GetByIdAsync(long id, CancellationToken ct = default) =>
        await _repo.GetByIdAsync(id, ct) ?? throw new NotFoundException("Expense category", id);

    public async Task<ExpenseCategoryDto> CreateAsync(CreateExpenseCategoryRequest request, CancellationToken ct = default)
    {
        request.Code = TextClean.Req(request.Code);
        request.Name = TextClean.Req(request.Name);
        request.Description = TextClean.Opt(request.Description);

        if (await _repo.ExistsAsync(request.Code, request.Name, null, ct))
            throw new ConflictException("An expense category with the same code or name already exists.");

        var id = await _repo.CreateAsync(request, _user.UserId, ct);
        var created = await GetByIdAsync(id, ct);
        await _audit.LogAsync(AuditActions.Create, "ExpenseCategory", id, null, created, ct);
        return created;
    }

    public async Task<ExpenseCategoryDto> UpdateAsync(long id, UpdateExpenseCategoryRequest request, CancellationToken ct = default)
    {
        var existing = await GetByIdAsync(id, ct);

        request.Code = TextClean.Req(request.Code);
        request.Name = TextClean.Req(request.Name);
        request.Description = TextClean.Opt(request.Description);
        var isActive = request.IsActive ?? existing.IsActive;

        if (await _repo.ExistsAsync(request.Code, request.Name, id, ct))
            throw new ConflictException("An expense category with the same code or name already exists.");

        await _repo.UpdateAsync(id, request, isActive, _user.UserId, ct);
        var updated = await GetByIdAsync(id, ct);

        var action = existing.IsActive && !isActive ? AuditActions.Deactivate : AuditActions.Update;
        await _audit.LogAsync(action, "ExpenseCategory", id, existing, updated, ct);
        return updated;
    }
}
