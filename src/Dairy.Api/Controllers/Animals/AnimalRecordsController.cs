using Dairy.Api.Authorization;
using Dairy.Application.DTOs.Animals;
using Dairy.Application.Interfaces.Animals;
using Dairy.Application.Requests.Animals;
using Dairy.Domain.Constants;
using Microsoft.AspNetCore.Mvc;

namespace Dairy.Api.Controllers.Animals;

/// <summary>Per-animal records. GET lists are paged (?page&amp;pageSize&amp;dateFrom&amp;dateTo&amp;sortBy=date|id&amp;sortDir).</summary>
[Route("api/animals/{animalId:long}")]
public class AnimalRecordsController : ApiControllerBase
{
    private readonly IAnimalRecordService _records;

    public AnimalRecordsController(IAnimalRecordService records) => _records = records;

    // ---- health

    [HttpGet("health")]
    [HasPermission(Permissions.AnimalView)]
    public async Task<IActionResult> ListHealth(long animalId, [FromQuery] AnimalRecordQuery query, CancellationToken ct) =>
        Paged(await _records.GetPagedAsync<AnimalHealthRecordDto>(AnimalRecordKinds.Health, animalId, query, ct));

    [HttpPost("health")]
    [HasPermission(Permissions.AnimalCreate, Permissions.AnimalUpdate)]
    public async Task<IActionResult> CreateHealth(long animalId, [FromBody] AnimalHealthRecordRequest request, CancellationToken ct) =>
        Created(await _records.CreateAsync<AnimalHealthRecordDto>(AnimalRecordKinds.Health, animalId, request, ct), "Health record added.");

    [HttpPut("health/{id:long}")]
    [HasPermission(Permissions.AnimalUpdate)]
    public async Task<IActionResult> UpdateHealth(long animalId, long id, [FromBody] AnimalHealthRecordRequest request, CancellationToken ct) =>
        Ok(await _records.UpdateAsync<AnimalHealthRecordDto>(AnimalRecordKinds.Health, animalId, id, request, ct), "Health record updated.");

    [HttpDelete("health/{id:long}")]
    [HasPermission(Permissions.AnimalUpdate)]
    public async Task<IActionResult> DeleteHealth(long animalId, long id, CancellationToken ct)
    {
        await _records.DeleteAsync<AnimalHealthRecordDto>(AnimalRecordKinds.Health, animalId, id, ct);
        return Message("Health record deleted.");
    }

    // ---- vaccinations

    [HttpGet("vaccinations")]
    [HasPermission(Permissions.AnimalView)]
    public async Task<IActionResult> ListVaccinations(long animalId, [FromQuery] AnimalRecordQuery query, CancellationToken ct) =>
        Paged(await _records.GetPagedAsync<VaccinationDto>(AnimalRecordKinds.Vaccination, animalId, query, ct));

    [HttpPost("vaccinations")]
    [HasPermission(Permissions.AnimalCreate, Permissions.AnimalUpdate)]
    public async Task<IActionResult> CreateVaccination(long animalId, [FromBody] VaccinationRequest request, CancellationToken ct) =>
        Created(await _records.CreateAsync<VaccinationDto>(AnimalRecordKinds.Vaccination, animalId, request, ct), "Vaccination added.");

    [HttpPut("vaccinations/{id:long}")]
    [HasPermission(Permissions.AnimalUpdate)]
    public async Task<IActionResult> UpdateVaccination(long animalId, long id, [FromBody] VaccinationRequest request, CancellationToken ct) =>
        Ok(await _records.UpdateAsync<VaccinationDto>(AnimalRecordKinds.Vaccination, animalId, id, request, ct), "Vaccination updated.");

    [HttpDelete("vaccinations/{id:long}")]
    [HasPermission(Permissions.AnimalUpdate)]
    public async Task<IActionResult> DeleteVaccination(long animalId, long id, CancellationToken ct)
    {
        await _records.DeleteAsync<VaccinationDto>(AnimalRecordKinds.Vaccination, animalId, id, ct);
        return Message("Vaccination deleted.");
    }

    // ---- dewormings

    [HttpGet("dewormings")]
    [HasPermission(Permissions.AnimalView)]
    public async Task<IActionResult> ListDewormings(long animalId, [FromQuery] AnimalRecordQuery query, CancellationToken ct) =>
        Paged(await _records.GetPagedAsync<DewormingDto>(AnimalRecordKinds.Deworming, animalId, query, ct));

    [HttpPost("dewormings")]
    [HasPermission(Permissions.AnimalCreate, Permissions.AnimalUpdate)]
    public async Task<IActionResult> CreateDeworming(long animalId, [FromBody] DewormingRequest request, CancellationToken ct) =>
        Created(await _records.CreateAsync<DewormingDto>(AnimalRecordKinds.Deworming, animalId, request, ct), "Deworming added.");

    [HttpPut("dewormings/{id:long}")]
    [HasPermission(Permissions.AnimalUpdate)]
    public async Task<IActionResult> UpdateDeworming(long animalId, long id, [FromBody] DewormingRequest request, CancellationToken ct) =>
        Ok(await _records.UpdateAsync<DewormingDto>(AnimalRecordKinds.Deworming, animalId, id, request, ct), "Deworming updated.");

    [HttpDelete("dewormings/{id:long}")]
    [HasPermission(Permissions.AnimalUpdate)]
    public async Task<IActionResult> DeleteDeworming(long animalId, long id, CancellationToken ct)
    {
        await _records.DeleteAsync<DewormingDto>(AnimalRecordKinds.Deworming, animalId, id, ct);
        return Message("Deworming deleted.");
    }

    // ---- breedings

    [HttpGet("breedings")]
    [HasPermission(Permissions.AnimalView)]
    public async Task<IActionResult> ListBreedings(long animalId, [FromQuery] AnimalRecordQuery query, CancellationToken ct) =>
        Paged(await _records.GetPagedAsync<BreedingRecordDto>(AnimalRecordKinds.Breeding, animalId, query, ct));

    [HttpPost("breedings")]
    [HasPermission(Permissions.AnimalCreate, Permissions.AnimalUpdate)]
    public async Task<IActionResult> CreateBreeding(long animalId, [FromBody] BreedingRecordRequest request, CancellationToken ct) =>
        Created(await _records.CreateAsync<BreedingRecordDto>(AnimalRecordKinds.Breeding, animalId, request, ct), "Breeding record added.");

    [HttpPut("breedings/{id:long}")]
    [HasPermission(Permissions.AnimalUpdate)]
    public async Task<IActionResult> UpdateBreeding(long animalId, long id, [FromBody] BreedingRecordRequest request, CancellationToken ct) =>
        Ok(await _records.UpdateAsync<BreedingRecordDto>(AnimalRecordKinds.Breeding, animalId, id, request, ct), "Breeding record updated.");

    [HttpDelete("breedings/{id:long}")]
    [HasPermission(Permissions.AnimalUpdate)]
    public async Task<IActionResult> DeleteBreeding(long animalId, long id, CancellationToken ct)
    {
        await _records.DeleteAsync<BreedingRecordDto>(AnimalRecordKinds.Breeding, animalId, id, ct);
        return Message("Breeding record deleted.");
    }

    // ---- pregnancies

    [HttpGet("pregnancies")]
    [HasPermission(Permissions.AnimalView)]
    public async Task<IActionResult> ListPregnancies(long animalId, [FromQuery] AnimalRecordQuery query, CancellationToken ct) =>
        Paged(await _records.GetPagedAsync<PregnancyRecordDto>(AnimalRecordKinds.Pregnancy, animalId, query, ct));

    [HttpPost("pregnancies")]
    [HasPermission(Permissions.AnimalCreate, Permissions.AnimalUpdate)]
    public async Task<IActionResult> CreatePregnancy(long animalId, [FromBody] PregnancyRecordRequest request, CancellationToken ct) =>
        Created(await _records.CreateAsync<PregnancyRecordDto>(AnimalRecordKinds.Pregnancy, animalId, request, ct), "Pregnancy record added.");

    [HttpPut("pregnancies/{id:long}")]
    [HasPermission(Permissions.AnimalUpdate)]
    public async Task<IActionResult> UpdatePregnancy(long animalId, long id, [FromBody] PregnancyRecordRequest request, CancellationToken ct) =>
        Ok(await _records.UpdateAsync<PregnancyRecordDto>(AnimalRecordKinds.Pregnancy, animalId, id, request, ct), "Pregnancy record updated.");

    [HttpDelete("pregnancies/{id:long}")]
    [HasPermission(Permissions.AnimalUpdate)]
    public async Task<IActionResult> DeletePregnancy(long animalId, long id, CancellationToken ct)
    {
        await _records.DeleteAsync<PregnancyRecordDto>(AnimalRecordKinds.Pregnancy, animalId, id, ct);
        return Message("Pregnancy record deleted.");
    }

    // ---- milk production

    [HttpGet("milk-production")]
    [HasPermission(Permissions.AnimalView)]
    public async Task<IActionResult> ListMilkProduction(long animalId, [FromQuery] AnimalRecordQuery query, CancellationToken ct) =>
        Paged(await _records.GetPagedAsync<AnimalMilkProductionDto>(AnimalRecordKinds.MilkProduction, animalId, query, ct));

    [HttpPost("milk-production")]
    [HasPermission(Permissions.AnimalCreate, Permissions.AnimalUpdate)]
    public async Task<IActionResult> CreateMilkProduction(long animalId, [FromBody] AnimalMilkProductionRequest request, CancellationToken ct) =>
        Created(await _records.CreateAsync<AnimalMilkProductionDto>(AnimalRecordKinds.MilkProduction, animalId, request, ct), "Milk production recorded.");

    [HttpPut("milk-production/{id:long}")]
    [HasPermission(Permissions.AnimalUpdate)]
    public async Task<IActionResult> UpdateMilkProduction(long animalId, long id, [FromBody] AnimalMilkProductionRequest request, CancellationToken ct) =>
        Ok(await _records.UpdateAsync<AnimalMilkProductionDto>(AnimalRecordKinds.MilkProduction, animalId, id, request, ct), "Milk production updated.");

    [HttpDelete("milk-production/{id:long}")]
    [HasPermission(Permissions.AnimalUpdate)]
    public async Task<IActionResult> DeleteMilkProduction(long animalId, long id, CancellationToken ct)
    {
        await _records.DeleteAsync<AnimalMilkProductionDto>(AnimalRecordKinds.MilkProduction, animalId, id, ct);
        return Message("Milk production record deleted.");
    }
}
