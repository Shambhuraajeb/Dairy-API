using Dairy.Api.Authorization;
using Dairy.Application.Interfaces.Collection;
using Dairy.Application.Requests.Collection;
using Dairy.Domain.Constants;
using Microsoft.AspNetCore.Mvc;

namespace Dairy.Api.Controllers.Collection;

[Route("api/milk-tanks")]
public class MilkTanksController : ApiControllerBase
{
    private readonly IMilkTankService _tanks;

    public MilkTanksController(IMilkTankService tanks) => _tanks = tanks;

    [HttpGet]
    [HasPermission(Permissions.CollectionView)]
    public async Task<IActionResult> List([FromQuery] MilkTankQuery query, CancellationToken ct) =>
        Paged(await _tanks.GetPagedAsync(query, ct));

    [HttpGet("{id:long}")]
    [HasPermission(Permissions.CollectionView)]
    public async Task<IActionResult> Get(long id, CancellationToken ct) =>
        Ok(await _tanks.GetByIdAsync(id, ct));

    [HttpPost]
    [HasPermission(Permissions.CollectionCreate)]
    public async Task<IActionResult> Create([FromBody] CreateMilkTankRequest request, CancellationToken ct) =>
        Created(await _tanks.CreateAsync(request, ct), "Milk tank created.");

    /// <summary>Update; send isActive=false to deactivate.</summary>
    [HttpPut("{id:long}")]
    [HasPermission(Permissions.CollectionUpdate)]
    public async Task<IActionResult> Update(long id, [FromBody] UpdateMilkTankRequest request, CancellationToken ct) =>
        Ok(await _tanks.UpdateAsync(id, request, ct), "Milk tank updated.");

    [HttpGet("{id:long}/transactions")]
    [HasPermission(Permissions.CollectionView)]
    public async Task<IActionResult> Transactions(long id, [FromQuery] TankTransactionQuery query, CancellationToken ct) =>
        Paged(await _tanks.GetTransactionsAsync(id, query, ct));

    /// <summary>Manual Out (dispatch / disposal) or Adjust (stock correction) transaction.</summary>
    [HttpPost("{id:long}/transactions")]
    [HasPermission(Permissions.CollectionUpdate)]
    public async Task<IActionResult> AddTransaction(long id, [FromBody] CreateTankTransactionRequest request, CancellationToken ct) =>
        Created(await _tanks.AddTransactionAsync(id, request, ct), "Tank transaction recorded.");
}
