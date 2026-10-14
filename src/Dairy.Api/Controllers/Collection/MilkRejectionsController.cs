using Dairy.Api.Authorization;
using Dairy.Application.Interfaces.Collection;
using Dairy.Application.Requests.Collection;
using Dairy.Domain.Constants;
using Microsoft.AspNetCore.Mvc;

namespace Dairy.Api.Controllers.Collection;

[Route("api/milk-rejections")]
public class MilkRejectionsController : ApiControllerBase
{
    private readonly IMilkRejectionService _rejections;

    public MilkRejectionsController(IMilkRejectionService rejections) => _rejections = rejections;

    [HttpGet]
    [HasPermission(Permissions.CollectionView)]
    public async Task<IActionResult> List([FromQuery] MilkRejectionQuery query, CancellationToken ct) =>
        Paged(await _rejections.GetPagedAsync(query, ct));
}
