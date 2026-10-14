using Dairy.Application.Common;
using Dairy.Application.DTOs.Animals;
using Dairy.Application.Requests.Animals;

namespace Dairy.Application.Interfaces.Animals;

public interface IAnimalService
{
    Task<PagedResult<AnimalListDto>> GetPagedAsync(AnimalQuery query, CancellationToken ct = default);
    Task<AnimalDetailDto> GetByIdAsync(long id, CancellationToken ct = default);
    Task<AnimalDetailDto> CreateAsync(AnimalRequest request, CancellationToken ct = default);
    Task<AnimalDetailDto> UpdateAsync(long id, AnimalRequest request, CancellationToken ct = default);
    Task<AnimalDetailDto> SellAsync(long id, AnimalSaleRequest request, CancellationToken ct = default);
    Task<AnimalDetailDto> RecordDeathAsync(long id, AnimalDeathRequest request, CancellationToken ct = default);
    Task<AnimalDetailDto> ArchiveAsync(long id, AnimalArchiveRequest? request, CancellationToken ct = default);
    Task<PagedResult<AnimalDueDto>> GetDueAsync(AnimalDueQuery query, CancellationToken ct = default);
    Task<List<AnimalLifecycleEventDto>> GetLifecycleAsync(long id, CancellationToken ct = default);
}

public interface IBreedService
{
    Task<PagedResult<BreedDto>> GetPagedAsync(BreedQuery query, CancellationToken ct = default);
    Task<BreedDto> GetByIdAsync(long id, CancellationToken ct = default);
    Task<BreedDto> CreateAsync(CreateBreedRequest request, CancellationToken ct = default);
    Task<BreedDto> UpdateAsync(long id, UpdateBreedRequest request, CancellationToken ct = default);
    Task<BreedDto> DeactivateAsync(long id, CancellationToken ct = default);
}

public interface IAnimalRecordService
{
    Task<PagedResult<T>> GetPagedAsync<T>(string kind, long animalId, AnimalRecordQuery query, CancellationToken ct = default);
    Task<T> CreateAsync<T>(string kind, long animalId, object request, CancellationToken ct = default);
    Task<T> UpdateAsync<T>(string kind, long animalId, long id, object request, CancellationToken ct = default);
    Task DeleteAsync<T>(string kind, long animalId, long id, CancellationToken ct = default);
}
