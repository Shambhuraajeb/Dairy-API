using Dairy.Application.Common;
using Dairy.Application.DTOs.Animals;
using Dairy.Application.Interfaces;
using Dairy.Application.Interfaces.Animals;
using Dairy.Application.Interfaces.Farmers;
using Dairy.Application.Requests.Animals;
using Dairy.Domain.Constants;

namespace Dairy.Application.Services.Animals;

public class AnimalService : IAnimalService
{
    private const string Sold = "Sold";
    private const string Dead = "Dead";
    private const string Archived = "Archived";

    private readonly IAnimalRepository _repo;
    private readonly IFarmerRepository _farmers;
    private readonly IAuditService _audit;
    private readonly ICurrentUser _user;

    public AnimalService(IAnimalRepository repo, IFarmerRepository farmers, IAuditService audit, ICurrentUser user)
    {
        _repo = repo;
        _farmers = farmers;
        _audit = audit;
        _user = user;
    }

    public Task<PagedResult<AnimalListDto>> GetPagedAsync(AnimalQuery query, CancellationToken ct = default)
    {
        if (_user.IsInRole(Roles.Farmer))
            query.FarmerId = _user.FarmerId ?? throw new ForbiddenException();
        return _repo.GetPagedAsync(query, ct);
    }

    public Task<AnimalDetailDto> GetByIdAsync(long id, CancellationToken ct = default) => LoadAsync(id, ct);

    public async Task<AnimalDetailDto> CreateAsync(AnimalRequest request, CancellationToken ct = default)
    {
        if (_user.IsInRole(Roles.Farmer))
            request.FarmerId = _user.FarmerId ?? throw new ForbiddenException();

        await EnsureActiveFarmerAsync(request.FarmerId, ct);
        EnsureConsistent(request);

        var tag = request.TagNumber.Trim();
        if (await _repo.TagExistsAsync(tag, null, ct))
            throw new ConflictException($"An animal with tag number '{tag}' already exists.");

        var registrationDate = (request.RegistrationDate ?? DateTime.Today).Date;
        var id = await _repo.CreateAsync(request, registrationDate, _user.UserId, ct);
        var created = await LoadAsync(id, ct);

        await _audit.LogAsync(AuditActions.Create, "Animal", id, null, ToAudit(created), ct);
        return created;
    }

    public async Task<AnimalDetailDto> UpdateAsync(long id, AnimalRequest request, CancellationToken ct = default)
    {
        var existing = await LoadAsync(id, ct);
        EnsureEditable(existing);

        if (_user.IsInRole(Roles.Farmer))
            request.FarmerId = existing.FarmerId;
        else if (request.FarmerId != existing.FarmerId)
            await EnsureActiveFarmerAsync(request.FarmerId, ct);

        EnsureConsistent(request);

        var tag = request.TagNumber.Trim();
        if (await _repo.TagExistsAsync(tag, id, ct))
            throw new ConflictException($"An animal with tag number '{tag}' already exists.");

        var registrationDate = (request.RegistrationDate ?? existing.RegistrationDate).Date;
        await _repo.UpdateAsync(id, request, registrationDate, _user.UserId, ct);
        var updated = await LoadAsync(id, ct);

        await _audit.LogAsync(AuditActions.Update, "Animal", id, ToAudit(existing), ToAudit(updated), ct);
        return updated;
    }

    public async Task<AnimalDetailDto> SellAsync(long id, AnimalSaleRequest request, CancellationToken ct = default)
    {
        var existing = await LoadAsync(id, ct);
        EnsureEditable(existing);

        var saleDate = (request.SaleDate ?? DateTime.Today).Date;
        if (saleDate < existing.RegistrationDate.Date)
            throw new RequestValidationException("saleDate", "Sale date cannot be before the registration date.");

        await _repo.SellAsync(id, request, saleDate, _user.UserId, ct);
        var updated = await LoadAsync(id, ct);

        await _audit.LogAsync("Sale", "Animal", id, ToAudit(existing), new { Animal = ToAudit(updated), updated.Sale }, ct);
        return updated;
    }

    public async Task<AnimalDetailDto> RecordDeathAsync(long id, AnimalDeathRequest request, CancellationToken ct = default)
    {
        var existing = await LoadAsync(id, ct);
        EnsureEditable(existing);

        var deathDate = (request.DeathDate ?? DateTime.Today).Date;
        if (deathDate < existing.RegistrationDate.Date)
            throw new RequestValidationException("deathDate", "Death date cannot be before the registration date.");

        await _repo.RecordDeathAsync(id, request, deathDate, _user.UserId, ct);
        var updated = await LoadAsync(id, ct);

        await _audit.LogAsync("Death", "Animal", id, ToAudit(existing), new { Animal = ToAudit(updated), updated.Death }, ct);
        return updated;
    }

    public async Task<AnimalDetailDto> ArchiveAsync(long id, AnimalArchiveRequest? request, CancellationToken ct = default)
    {
        var existing = await LoadAsync(id, ct);
        EnsureEditable(existing);

        await _repo.ArchiveAsync(id, request?.Reason, _user.UserId, ct);
        var updated = await LoadAsync(id, ct);

        await _audit.LogAsync("Archive", "Animal", id, ToAudit(existing), ToAudit(updated), ct);
        return updated;
    }

    public Task<PagedResult<AnimalDueDto>> GetDueAsync(AnimalDueQuery query, CancellationToken ct = default)
    {
        var type = query.Type?.Trim().ToLowerInvariant();
        if (string.IsNullOrEmpty(type))
            throw new RequestValidationException("type", "Type is required (vaccination, deworming or pregnancy).");

        if (_user.IsInRole(Roles.Farmer))
            query.FarmerId = _user.FarmerId ?? throw new ForbiddenException();

        return _repo.GetDuePagedAsync(type, query, ct);
    }

    public async Task<List<AnimalLifecycleEventDto>> GetLifecycleAsync(long id, CancellationToken ct = default)
    {
        await LoadAsync(id, ct);
        return await _repo.GetLifecycleAsync(id, ct);
    }

    // ------------------------------------------------------------------ helpers

    private async Task<AnimalDetailDto> LoadAsync(long id, CancellationToken ct)
    {
        var animal = await _repo.GetByIdAsync(id, ct) ?? throw new NotFoundException("Animal", id);
        if (_user.IsInRole(Roles.Farmer) && animal.FarmerId != _user.FarmerId)
            throw new NotFoundException("Animal", id);
        return animal;
    }

    private async Task EnsureActiveFarmerAsync(long farmerId, CancellationToken ct)
    {
        var farmer = await _farmers.GetBasicAsync(farmerId, ct) ?? throw new NotFoundException("Farmer", farmerId);
        if (farmer.Status != FarmerStatus.Active)
            throw new BusinessRuleException("The farmer must be active to own registered animals.");
    }

    private static void EnsureEditable(AnimalDetailDto animal)
    {
        if (animal.Status is Sold or Dead or Archived)
            throw new BusinessRuleException($"This animal is {animal.Status.ToLowerInvariant()} and can no longer be changed.");
    }

    private static void EnsureConsistent(AnimalRequest request)
    {
        if (request.Gender == "Male" && request.PregnancyStatus == "Pregnant")
            throw new BusinessRuleException("A male animal cannot be marked pregnant.");
    }

    private static object ToAudit(AnimalDetailDto a) => new
    {
        a.Id, a.TagNumber, a.Name, a.FarmerId, a.BreedId, a.AnimalType, a.Gender, a.DateOfBirth, a.Source, a.RegistrationDate,
        a.LactationNo, a.LastCalvingDate, a.MilkingStatus, a.PregnancyStatus, a.ExpectedDeliveryDate, a.Status, a.Remarks
    };
}
