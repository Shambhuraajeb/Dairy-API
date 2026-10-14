using Dairy.Application.Common;
using Dairy.Application.DTOs.Inventory;
using Dairy.Application.Interfaces;
using Dairy.Application.Interfaces.Inventory;
using Dairy.Application.Requests.Inventory;
using Dairy.Domain.Constants;

namespace Dairy.Application.Services.Inventory;

public class ItemCategoryService : IItemCategoryService
{
    private readonly IItemCategoryRepository _repo;
    private readonly IAuditService _audit;
    private readonly ICurrentUser _user;

    public ItemCategoryService(IItemCategoryRepository repo, IAuditService audit, ICurrentUser user)
    {
        _repo = repo;
        _audit = audit;
        _user = user;
    }

    public Task<PagedResult<ItemCategoryDto>> GetPagedAsync(ItemCategoryQuery query, CancellationToken ct = default) =>
        _repo.GetPagedAsync(query, ct);

    public async Task<ItemCategoryDto> GetByIdAsync(long id, CancellationToken ct = default) =>
        await _repo.GetByIdAsync(id, ct) ?? throw new NotFoundException("Item category", id);

    public async Task<ItemCategoryDto> CreateAsync(CreateItemCategoryRequest request, CancellationToken ct = default)
    {
        request.Code = TextClean.Req(request.Code);
        request.Name = TextClean.Req(request.Name);
        request.Description = TextClean.Opt(request.Description);

        if (await _repo.ExistsAsync(request.Code, request.Name, null, ct))
            throw new ConflictException("An item category with the same code or name already exists.");

        var id = await _repo.CreateAsync(request, _user.UserId, ct);
        var created = await GetByIdAsync(id, ct);
        await _audit.LogAsync(AuditActions.Create, "ItemCategory", id, null, created, ct);
        return created;
    }

    public async Task<ItemCategoryDto> UpdateAsync(long id, UpdateItemCategoryRequest request, CancellationToken ct = default)
    {
        var existing = await _repo.GetByIdAsync(id, ct) ?? throw new NotFoundException("Item category", id);

        request.Code = TextClean.Req(request.Code);
        request.Name = TextClean.Req(request.Name);
        request.Description = TextClean.Opt(request.Description);
        var isActive = request.IsActive ?? existing.IsActive;

        if (await _repo.ExistsAsync(request.Code, request.Name, id, ct))
            throw new ConflictException("An item category with the same code or name already exists.");

        await _repo.UpdateAsync(id, request, isActive, _user.UserId, ct);
        var updated = await GetByIdAsync(id, ct);

        var action = existing.IsActive && !isActive ? AuditActions.Deactivate : AuditActions.Update;
        await _audit.LogAsync(action, "ItemCategory", id, existing, updated, ct);
        return updated;
    }
}
