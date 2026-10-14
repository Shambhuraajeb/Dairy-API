using Dairy.Application.Interfaces.Lookups;
using Microsoft.AspNetCore.Mvc;

namespace Dairy.Api.Controllers.Lookups;

/// <summary>Small dropdown lists. Any authenticated user may call these.</summary>
[Route("api/lookups")]
public class LookupsController : ApiControllerBase
{
    private readonly ILookupService _lookups;

    public LookupsController(ILookupService lookups) => _lookups = lookups;

    [HttpGet("{name}")]
    public async Task<IActionResult> Get(
        string name,
        [FromQuery] string? search,
        [FromQuery] long? parentId,
        [FromQuery] string? status,
        CancellationToken ct) =>
        Ok(await _lookups.GetAsync(name, search, parentId, status, ct));
}
