using Dairy.Application.Common;
using Dairy.Application.DTOs.Collection;
using Dairy.Application.Interfaces;
using Dairy.Application.Interfaces.Collection;
using Dairy.Application.Requests.Collection;
using Dairy.Domain.Constants;

namespace Dairy.Application.Services.Collection;

public class MilkTankService : IMilkTankService
{
    private static readonly string[] MilkTypes = { "Cow", "Buffalo", "Mixed" };
    private static readonly string[] Statuses = { "Active", "Maintenance", "Inactive" };

    private readonly IMilkTankRepository _repo;
    private readonly IAuditService _audit;
    private readonly ICurrentUser _user;

    public MilkTankService(IMilkTankRepository repo, IAuditService audit, ICurrentUser user)
    {
        _repo = repo;
        _audit = audit;
        _user = user;
    }

    public Task<PagedResult<MilkTankDto>> GetPagedAsync(MilkTankQuery query, CancellationToken ct = default)
    {
        EnsureNotFarmer();
        return _repo.GetPagedAsync(query, ct);
    }

    public async Task<MilkTankDto> GetByIdAsync(long id, CancellationToken ct = default)
    {
        EnsureNotFarmer();
        return await _repo.GetByIdAsync(id, ct) ?? throw new NotFoundException("Milk tank", id);
    }

    public async Task<MilkTankDto> CreateAsync(CreateMilkTankRequest request, CancellationToken ct = default)
    {
        request.Code = request.Code.Trim();
        request.Name = request.Name.Trim();
        request.MilkType = Canon(request.MilkType, MilkTypes);

        if (await _repo.CodeExistsAsync(request.Code, null, ct))
            throw new ConflictException($"A milk tank with code '{request.Code}' already exists.");
        if (!await _repo.CenterExistsAsync(request.CollectionCenterId, ct))
            throw new RequestValidationException("collectionCenterId", "Collection center not found.");

        var id = await _repo.CreateAsync(request, _user.UserId, ct);
        var created = await _repo.GetByIdAsync(id, ct) ?? throw new NotFoundException("Milk tank", id);
        await _audit.LogAsync(AuditActions.Create, "MilkTank", id, null, created, ct);
        return created;
    }

    public async Task<MilkTankDto> UpdateAsync(long id, UpdateMilkTankRequest request, CancellationToken ct = default)
    {
        var existing = await _repo.GetByIdAsync(id, ct) ?? throw new NotFoundException("Milk tank", id);

        request.Name = request.Name.Trim();
        request.MilkType = Canon(request.MilkType, MilkTypes);
        request.Status = Canon(request.Status, Statuses) ?? "Active";

        if (!await _repo.CenterExistsAsync(request.CollectionCenterId, ct))
            throw new RequestValidationException("collectionCenterId", "Collection center not found.");
        if (request.CapacityLiters < existing.CurrentQuantity)
            throw new BusinessRuleException(
                $"Capacity cannot be lower than the current quantity in the tank ({existing.CurrentQuantity:0.###} L).");
        if (request.CollectionCenterId != existing.CollectionCenterId && existing.CurrentQuantity > 0)
            throw new BusinessRuleException("A tank holding milk cannot be moved to another collection center.");

        var isActive = request.IsActive ?? existing.IsActive;
        await _repo.UpdateAsync(id, request, isActive, _user.UserId, ct);

        var updated = await _repo.GetByIdAsync(id, ct) ?? throw new NotFoundException("Milk tank", id);
        var action = existing.IsActive && !isActive ? AuditActions.Deactivate : AuditActions.Update;
        await _audit.LogAsync(action, "MilkTank", id, existing, updated, ct);
        return updated;
    }

    public async Task<PagedResult<TankTransactionDto>> GetTransactionsAsync(long tankId, TankTransactionQuery query, CancellationToken ct = default)
    {
        EnsureNotFarmer();
        _ = await _repo.GetByIdAsync(tankId, ct) ?? throw new NotFoundException("Milk tank", tankId);
        return await _repo.GetTransactionsAsync(tankId, query, ct);
    }

    public async Task<TankTransactionDto> AddTransactionAsync(long tankId, CreateTankTransactionRequest request, CancellationToken ct = default)
    {
        var tank = await _repo.GetByIdAsync(tankId, ct) ?? throw new NotFoundException("Milk tank", tankId);
        if (!tank.IsActive || tank.Status == "Inactive")
            throw new BusinessRuleException("The milk tank is inactive.");

        var type = string.Equals(request.TransactionType.Trim(), "Out", StringComparison.OrdinalIgnoreCase) ? "Out" : "Adjust";
        var destination = string.IsNullOrWhiteSpace(request.Destination) ? null : request.Destination.Trim();
        var remarks = string.IsNullOrWhiteSpace(request.Remarks) ? null : request.Remarks.Trim();

        var txn = await _repo.AddTransactionAsync(tankId, type, request.Quantity, destination, remarks, _user.UserId, ct);
        await _audit.LogAsync(AuditActions.Create, "TankTransaction", txn.Id,
            new { tank.Id, tank.CurrentQuantity }, txn, ct);
        return txn;
    }

    private void EnsureNotFarmer()
    {
        if (_user.IsInRole(Roles.Farmer))
            throw new ForbiddenException();
    }

    private static string? Canon(string? value, string[] allowed) =>
        string.IsNullOrWhiteSpace(value)
            ? null
            : allowed.FirstOrDefault(a => string.Equals(a, value.Trim(), StringComparison.OrdinalIgnoreCase));
}
