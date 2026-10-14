using Dairy.Application.Common;
using Dairy.Application.DTOs.Animals;
using Dairy.Application.Interfaces;
using Dairy.Application.Interfaces.Animals;
using Dairy.Application.Requests.Animals;
using Dairy.Domain.Constants;

namespace Dairy.Application.Services.Animals;

/// <summary>Per-animal health / vaccination / deworming / breeding / pregnancy / milk-production records.</summary>
public class AnimalRecordService : IAnimalRecordService
{
    private static readonly Dictionary<string, string> EntityNames = new()
    {
        [AnimalRecordKinds.Health] = "AnimalHealthRecord",
        [AnimalRecordKinds.Vaccination] = "Vaccination",
        [AnimalRecordKinds.Deworming] = "Deworming",
        [AnimalRecordKinds.Breeding] = "BreedingRecord",
        [AnimalRecordKinds.Pregnancy] = "PregnancyRecord",
        [AnimalRecordKinds.MilkProduction] = "AnimalMilkProduction"
    };

    private readonly IAnimalRecordRepository _repo;
    private readonly IAnimalRepository _animals;
    private readonly IAuditService _audit;
    private readonly ICurrentUser _user;

    public AnimalRecordService(IAnimalRecordRepository repo, IAnimalRepository animals, IAuditService audit, ICurrentUser user)
    {
        _repo = repo;
        _animals = animals;
        _audit = audit;
        _user = user;
    }

    public async Task<PagedResult<T>> GetPagedAsync<T>(string kind, long animalId, AnimalRecordQuery query, CancellationToken ct = default)
    {
        await LoadAnimalAsync(animalId, ct);
        return await _repo.GetPagedAsync<T>(kind, animalId, query, ct);
    }

    public async Task<T> CreateAsync<T>(string kind, long animalId, object request, CancellationToken ct = default)
    {
        var animal = await LoadAnimalAsync(animalId, ct);

        if (animal.Status is "Sold" or "Dead" or "Archived")
            throw new BusinessRuleException($"This animal is {animal.Status.ToLowerInvariant()}; no new records can be added.");

        EnsureEligible(kind, animal);
        if (request is PregnancyRecordRequest pregnancy && pregnancy.Status == "Pregnant" && animal.PregnancyStatus == "Pregnant")
            throw new BusinessRuleException("This animal already has an active pregnancy.");

        var id = await _repo.InsertAsync(kind, animalId, request, _user.UserId, ct);
        await AfterWriteAsync(kind, animalId, request, null, ct);

        var created = await _repo.GetByIdAsync<T>(kind, animalId, id, ct) ?? throw new NotFoundException(EntityNames[kind], id);
        await _audit.LogAsync(AuditActions.Create, EntityNames[kind], id, null, created, ct);
        return created;
    }

    public async Task<T> UpdateAsync<T>(string kind, long animalId, long id, object request, CancellationToken ct = default)
    {
        await LoadAnimalAsync(animalId, ct);
        var existing = await _repo.GetByIdAsync<T>(kind, animalId, id, ct) ?? throw new NotFoundException(EntityNames[kind], id);

        await _repo.UpdateAsync(kind, animalId, id, request, _user.UserId, ct);
        await AfterWriteAsync(kind, animalId, request, existing, ct);

        var updated = await _repo.GetByIdAsync<T>(kind, animalId, id, ct) ?? throw new NotFoundException(EntityNames[kind], id);
        await _audit.LogAsync(AuditActions.Update, EntityNames[kind], id, existing, updated, ct);
        return updated;
    }

    public async Task DeleteAsync<T>(string kind, long animalId, long id, CancellationToken ct = default)
    {
        await LoadAnimalAsync(animalId, ct);
        var existing = await _repo.GetByIdAsync<T>(kind, animalId, id, ct) ?? throw new NotFoundException(EntityNames[kind], id);

        if (!await _repo.DeleteAsync(kind, animalId, id, _user.UserId, ct))
            throw new NotFoundException(EntityNames[kind], id);

        await AfterWriteAsync(kind, animalId, null, null, ct);
        await _audit.LogAsync(AuditActions.Delete, EntityNames[kind], id, existing, null, ct);
    }

    // ------------------------------------------------------------------ helpers

    private async Task<AnimalDetailDto> LoadAnimalAsync(long animalId, CancellationToken ct)
    {
        var animal = await _animals.GetByIdAsync(animalId, ct) ?? throw new NotFoundException("Animal", animalId);
        if (_user.IsInRole(Roles.Farmer) && animal.FarmerId != _user.FarmerId)
            throw new NotFoundException("Animal", animalId);
        return animal;
    }

    private static void EnsureEligible(string kind, AnimalDetailDto animal)
    {
        var femaleOnly = kind is AnimalRecordKinds.Breeding or AnimalRecordKinds.Pregnancy or AnimalRecordKinds.MilkProduction;
        if (femaleOnly && animal.Gender != "Female")
            throw new BusinessRuleException("These records apply to female animals only.");
    }

    /// <summary>Keeps Animal.Status / PregnancyStatus and lactation counters in step with the child records.</summary>
    private async Task AfterWriteAsync(string kind, long animalId, object? request, object? previous, CancellationToken ct)
    {
        switch (kind)
        {
            case AnimalRecordKinds.Health:
                await _repo.SyncSickStatusAsync(animalId, ct);
                break;

            case AnimalRecordKinds.Pregnancy:
                await _repo.SyncPregnancyStatusAsync(animalId, ct);
                if (request is PregnancyRecordRequest req
                    && req.Status == "Delivered"
                    && req.ActualDeliveryDate.HasValue
                    && !(previous is PregnancyRecordDto old && old.Status == "Delivered"))
                {
                    await _repo.ApplyDeliveryAsync(animalId, req.ActualDeliveryDate.Value, ct);
                }
                break;
        }
    }
}
