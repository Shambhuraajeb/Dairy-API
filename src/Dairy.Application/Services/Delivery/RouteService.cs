using Dairy.Application.BusinessRules;
using Dairy.Application.Common;
using Dairy.Application.DTOs.Delivery;
using Dairy.Application.Interfaces;
using Dairy.Application.Interfaces.Delivery;
using Dairy.Application.Requests.Delivery;
using Dairy.Domain.Constants;

namespace Dairy.Application.Services.Delivery;

public class RouteService : IRouteService
{
    private readonly IRouteRepository _repo;
    private readonly IAuditService _audit;
    private readonly ICurrentUser _user;

    public RouteService(IRouteRepository repo, IAuditService audit, ICurrentUser user)
    {
        _repo = repo;
        _audit = audit;
        _user = user;
    }

    public Task<PagedResult<RouteDto>> GetPagedAsync(RouteQuery query, CancellationToken ct = default) =>
        _repo.GetPagedAsync(query, ct);

    public async Task<RouteDto> GetByIdAsync(long id, CancellationToken ct = default) =>
        await _repo.GetByIdAsync(id, ct) ?? throw new NotFoundException("Route", id);

    public async Task<RouteDto> CreateAsync(CreateRouteRequest request, CancellationToken ct = default)
    {
        var code = SalesValues.Clean(request.Code)?.ToUpperInvariant();
        if (code is not null && await _repo.CodeExistsAsync(code, null, ct))
            throw new ConflictException($"A route with code '{code}' already exists.");

        var created = await _repo.CreateAsync(code, request, _user.UserId, ct);
        var dto = await _repo.GetByIdAsync(created.Id, ct) ?? throw new NotFoundException("Route", created.Id);

        await _audit.LogAsync(AuditActions.Create, "Route", created.Id, null, dto, ct);
        return dto;
    }

    public async Task<RouteDto> UpdateAsync(long id, UpdateRouteRequest request, CancellationToken ct = default)
    {
        var existing = await _repo.GetByIdAsync(id, ct) ?? throw new NotFoundException("Route", id);
        var code = request.Code.Trim().ToUpperInvariant();
        if (await _repo.CodeExistsAsync(code, id, ct))
            throw new ConflictException($"A route with code '{code}' already exists.");

        var isActive = request.IsActive ?? existing.IsActive;
        if (existing.IsActive && !isActive) await EnsureNoOpenSchedulesAsync(id, ct);

        await _repo.UpdateAsync(id, request, isActive, _user.UserId, ct);
        var dto = await _repo.GetByIdAsync(id, ct) ?? throw new NotFoundException("Route", id);

        var action = existing.IsActive && !isActive ? AuditActions.Deactivate : AuditActions.Update;
        await _audit.LogAsync(action, "Route", id, existing, dto, ct);
        return dto;
    }

    /// <summary>Soft delete: deactivates the route (blocked while upcoming schedules use it).</summary>
    public async Task DeleteAsync(long id, CancellationToken ct = default)
    {
        var existing = await _repo.GetByIdAsync(id, ct) ?? throw new NotFoundException("Route", id);
        await EnsureNoOpenSchedulesAsync(id, ct);

        await _repo.SetActiveAsync(id, false, _user.UserId, ct);
        await _audit.LogAsync(AuditActions.Deactivate, "Route", id, existing, null, ct);
    }

    private async Task EnsureNoOpenSchedulesAsync(long id, CancellationToken ct)
    {
        var open = await _repo.CountOpenSchedulesAsync(id, ct);
        if (open > 0)
            throw new BusinessRuleException($"This route has {open} upcoming delivery schedule(s). Complete or remove them first.");
    }
}
