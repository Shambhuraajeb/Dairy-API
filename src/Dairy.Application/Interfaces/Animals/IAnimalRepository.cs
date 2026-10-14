using Dairy.Application.Common;
using Dairy.Application.DTOs.Animals;
using Dairy.Application.Requests.Animals;

namespace Dairy.Application.Interfaces.Animals;

public interface IAnimalRepository
{
    Task<PagedResult<AnimalListDto>> GetPagedAsync(AnimalQuery query, CancellationToken ct = default);

    /// <summary>Full animal including sale / death rows when present.</summary>
    Task<AnimalDetailDto?> GetByIdAsync(long id, CancellationToken ct = default);

    Task<bool> TagExistsAsync(string tagNumber, long? excludeId, CancellationToken ct = default);
    Task<long> CreateAsync(AnimalRequest request, DateTime registrationDate, long userId, CancellationToken ct = default);
    Task UpdateAsync(long id, AnimalRequest request, DateTime registrationDate, long userId, CancellationToken ct = default);

    /// <summary>Inserts AnimalSale and sets the animal Status to Sold in one transaction.</summary>
    Task SellAsync(long animalId, AnimalSaleRequest request, DateTime saleDate, long userId, CancellationToken ct = default);

    /// <summary>Inserts AnimalDeath and sets the animal Status to Dead in one transaction.</summary>
    Task RecordDeathAsync(long animalId, AnimalDeathRequest request, DateTime deathDate, long userId, CancellationToken ct = default);

    Task ArchiveAsync(long animalId, string? reason, long userId, CancellationToken ct = default);

    Task<PagedResult<AnimalDueDto>> GetDuePagedAsync(string type, AnimalDueQuery query, CancellationToken ct = default);
    Task<List<AnimalLifecycleEventDto>> GetLifecycleAsync(long animalId, CancellationToken ct = default);
}

public interface IBreedRepository
{
    Task<PagedResult<BreedDto>> GetPagedAsync(BreedQuery query, CancellationToken ct = default);
    Task<BreedDto?> GetByIdAsync(long id, CancellationToken ct = default);
    Task<bool> CodeOrNameExistsAsync(string code, string name, long? excludeId, CancellationToken ct = default);
    Task<long> CreateAsync(CreateBreedRequest request, long userId, CancellationToken ct = default);
    Task UpdateAsync(long id, UpdateBreedRequest request, bool isActive, long userId, CancellationToken ct = default);
}

public static class AnimalRecordKinds
{
    public const string Health = "health";
    public const string Vaccination = "vaccination";
    public const string Deworming = "deworming";
    public const string Breeding = "breeding";
    public const string Pregnancy = "pregnancy";
    public const string MilkProduction = "milk-production";
}

public interface IAnimalRecordRepository
{
    Task<PagedResult<T>> GetPagedAsync<T>(string kind, long animalId, AnimalRecordQuery query, CancellationToken ct = default);
    Task<T?> GetByIdAsync<T>(string kind, long animalId, long id, CancellationToken ct = default);

    /// <summary>Inserts a row from the request's properties (named like the table columns); returns the new id.</summary>
    Task<long> InsertAsync(string kind, long animalId, object request, long userId, CancellationToken ct = default);
    Task UpdateAsync(string kind, long animalId, long id, object request, long userId, CancellationToken ct = default);
    Task<bool> DeleteAsync(string kind, long animalId, long id, long userId, CancellationToken ct = default);

    /// <summary>Animal.Status = Sick while any health record is Open/Ongoing, otherwise Active (only for Active/Sick animals).</summary>
    Task SyncSickStatusAsync(long animalId, CancellationToken ct = default);

    /// <summary>Animal.PregnancyStatus / ExpectedDeliveryDate follow the Pregnant pregnancy records.</summary>
    Task SyncPregnancyStatusAsync(long animalId, CancellationToken ct = default);

    /// <summary>Increments LactationNo, sets LastCalvingDate and MilkingStatus = Milking.</summary>
    Task ApplyDeliveryAsync(long animalId, DateTime deliveryDate, CancellationToken ct = default);
}
