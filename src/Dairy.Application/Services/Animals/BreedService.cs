using Dairy.Application.Common;
using Dairy.Application.DTOs.Animals;
using Dairy.Application.Interfaces;
using Dairy.Application.Interfaces.Animals;
using Dairy.Application.Requests.Animals;
using Dairy.Domain.Constants;

namespace Dairy.Application.Services.Animals;

public class BreedService : IBreedService
{
    private readonly IBreedRepository _repo;
    private readonly IAuditService _audit;
    private readonly ICurrentUser _user;

    public BreedService(IBreedRepository repo, IAuditService audit, ICurrentUser user)
    {
        _repo = repo;
        _audit = audit;
        _user = user;
    }

    public Task<PagedResult<BreedDto>> GetPagedAsync(BreedQuery query, CancellationToken ct = default) =>
        _repo.GetPagedAsync(query, ct);

    public async Task<BreedDto> GetByIdAsync(long id, CancellationToken ct = default) =>
        await _repo.GetByIdAsync(id, ct) ?? throw new NotFoundException("Breed", id);

    public async Task<BreedDto> CreateAsync(CreateBreedRequest request, CancellationToken ct = default)
    {
        var code = request.Code.Trim();
        var name = request.Name.Trim();
        if (await _repo.CodeOrNameExistsAsync(code, name, null, ct))
            throw new ConflictException("A breed with the same code or name already exists.");

        var id = await _repo.CreateAsync(request, _user.UserId, ct);
        var created = await GetByIdAsync(id, ct);

        await _audit.LogAsync(AuditActions.Create, "Breed", id, null, created, ct);
        return created;
    }

    public async Task<BreedDto> UpdateAsync(long id, UpdateBreedRequest request, CancellationToken ct = default)
    {
        var existing = await GetByIdAsync(id, ct);

        var code = request.Code.Trim();
        var name = request.Name.Trim();
        if (await _repo.CodeOrNameExistsAsync(code, name, id, ct))
            throw new ConflictException("A breed with the same code or name already exists.");

        var isActive = request.IsActive ?? existing.IsActive;
        await _repo.UpdateAsync(id, request, isActive, _user.UserId, ct);
        var updated = await GetByIdAsync(id, ct);

        var action = existing.IsActive && !isActive ? AuditActions.Deactivate : AuditActions.Update;
        await _audit.LogAsync(action, "Breed", id, existing, updated, ct);
        return updated;
    }

    public async Task<BreedDto> DeactivateAsync(long id, CancellationToken ct = default)
    {
        var existing = await GetByIdAsync(id, ct);
        if (!existing.IsActive) return existing;

        await _repo.UpdateAsync(id,
            new UpdateBreedRequest
            {
                Code = existing.Code,
                Name = existing.Name,
                AnimalType = existing.AnimalType,
                AverageDailyYield = existing.AverageDailyYield,
                Description = existing.Description
            },
            false, _user.UserId, ct);

        var updated = await GetByIdAsync(id, ct);
        await _audit.LogAsync(AuditActions.Deactivate, "Breed", id, existing, updated, ct);
        return updated;
    }
}
