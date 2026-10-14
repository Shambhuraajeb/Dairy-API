using Dairy.Api.Authorization;
using Dairy.Application.Interfaces.Animals;
using Dairy.Application.Requests.Animals;
using Dairy.Domain.Constants;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Dairy.Api.Controllers.Animals;

[Route("api/animals")]
public class AnimalsController : ApiControllerBase
{
    private readonly IAnimalService _animals;

    public AnimalsController(IAnimalService animals) => _animals = animals;

    [HttpGet]
    [HasPermission(Permissions.AnimalView)]
    public async Task<IActionResult> List([FromQuery] AnimalQuery query, CancellationToken ct) =>
        Paged(await _animals.GetPagedAsync(query, ct));

    /// <summary>Due lists: ?type=vaccination|deworming|pregnancy&amp;days=30 (overdue items always included).</summary>
    [HttpGet("due")]
    [HasPermission(Permissions.AnimalView)]
    public async Task<IActionResult> Due([FromQuery] AnimalDueQuery query, CancellationToken ct) =>
        Paged(await _animals.GetDueAsync(query, ct));

    [HttpGet("{id:long}")]
    [HasPermission(Permissions.AnimalView)]
    public async Task<IActionResult> Get(long id, CancellationToken ct) =>
        Ok(await _animals.GetByIdAsync(id, ct));

    [HttpPost]
    [HasPermission(Permissions.AnimalCreate)]
    public async Task<IActionResult> Create([FromBody] AnimalRequest request, CancellationToken ct) =>
        Created(await _animals.CreateAsync(request, ct), "Animal registered.");

    [HttpPut("{id:long}")]
    [HasPermission(Permissions.AnimalUpdate)]
    public async Task<IActionResult> Update(long id, [FromBody] AnimalRequest request, CancellationToken ct) =>
        Ok(await _animals.UpdateAsync(id, request, ct), "Animal updated.");

    [HttpPost("{id:long}/sale")]
    [HasPermission(Permissions.AnimalUpdate)]
    public async Task<IActionResult> Sell(long id, [FromBody] AnimalSaleRequest request, CancellationToken ct) =>
        Ok(await _animals.SellAsync(id, request, ct), "Animal sale recorded.");

    [HttpPost("{id:long}/death")]
    [HasPermission(Permissions.AnimalUpdate)]
    public async Task<IActionResult> Death(long id, [FromBody] AnimalDeathRequest request, CancellationToken ct) =>
        Ok(await _animals.RecordDeathAsync(id, request, ct), "Animal death recorded.");

    /// <summary>Body optional: { reason? }.</summary>
    [HttpPost("{id:long}/archive")]
    [HasPermission(Permissions.AnimalUpdate)]
    public async Task<IActionResult> Archive(
        long id,
        [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] AnimalArchiveRequest? request,
        CancellationToken ct) =>
        Ok(await _animals.ArchiveAsync(id, request, ct), "Animal archived.");

    [HttpGet("{id:long}/lifecycle")]
    [HasPermission(Permissions.AnimalView)]
    public async Task<IActionResult> Lifecycle(long id, CancellationToken ct) =>
        Ok(await _animals.GetLifecycleAsync(id, ct));
}

[Route("api/breeds")]
public class BreedsController : ApiControllerBase
{
    private readonly IBreedService _breeds;

    public BreedsController(IBreedService breeds) => _breeds = breeds;

    [HttpGet]
    [HasPermission(Permissions.AnimalView)]
    public async Task<IActionResult> List([FromQuery] BreedQuery query, CancellationToken ct) =>
        Paged(await _breeds.GetPagedAsync(query, ct));

    [HttpGet("{id:long}")]
    [HasPermission(Permissions.AnimalView)]
    public async Task<IActionResult> Get(long id, CancellationToken ct) =>
        Ok(await _breeds.GetByIdAsync(id, ct));

    [HttpPost]
    [HasPermission(Permissions.AnimalCreate)]
    public async Task<IActionResult> Create([FromBody] CreateBreedRequest request, CancellationToken ct) =>
        Created(await _breeds.CreateAsync(request, ct), "Breed created.");

    /// <summary>Send isActive=false to deactivate.</summary>
    [HttpPut("{id:long}")]
    [HasPermission(Permissions.AnimalUpdate)]
    public async Task<IActionResult> Update(long id, [FromBody] UpdateBreedRequest request, CancellationToken ct) =>
        Ok(await _breeds.UpdateAsync(id, request, ct), "Breed updated.");

    /// <summary>Soft delete: deactivates the breed.</summary>
    [HttpDelete("{id:long}")]
    [HasPermission(Permissions.AnimalUpdate)]
    public async Task<IActionResult> Deactivate(long id, CancellationToken ct) =>
        Ok(await _breeds.DeactivateAsync(id, ct), "Breed deactivated.");
}
