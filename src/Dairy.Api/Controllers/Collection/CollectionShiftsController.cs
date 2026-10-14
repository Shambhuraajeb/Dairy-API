using Dairy.Api.Authorization;
using Dairy.Application.Interfaces.Collection;
using Dairy.Application.Requests.Collection;
using Dairy.Domain.Constants;
using Microsoft.AspNetCore.Mvc;

namespace Dairy.Api.Controllers.Collection;

[Route("api/collection-shifts")]
public class CollectionShiftsController : ApiControllerBase
{
    private readonly ICollectionShiftService _shifts;

    public CollectionShiftsController(ICollectionShiftService shifts) => _shifts = shifts;

    [HttpGet]
    [HasPermission(Permissions.CollectionView)]
    public async Task<IActionResult> List([FromQuery] ShiftListQuery query, CancellationToken ct) =>
        Ok(await _shifts.GetShiftsAsync(query, ct));

    [HttpGet("dashboard")]
    [HasPermission(Permissions.CollectionView)]
    public async Task<IActionResult> Dashboard([FromQuery] ShiftDashboardQuery query, CancellationToken ct) =>
        Ok(await _shifts.GetDashboardAsync(query, ct));
}
