using Dairy.Application.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Dairy.Api.Controllers;

/// <summary>Base for all controllers: JSON, [Authorize], ApiResponse helpers. Add [Route("api/...")] on the derived class.</summary>
[ApiController]
[Authorize]
[Produces("application/json")]
public abstract class ApiControllerBase : ControllerBase
{
    /// <summary>200 with ApiResponse&lt;T&gt;.</summary>
    protected IActionResult Ok<T>(T data, string? message = null) =>
        base.Ok(ApiResponse<T>.Ok(data, message));

    /// <summary>201 with ApiResponse&lt;T&gt;.</summary>
    protected IActionResult Created<T>(T data, string? message = null) =>
        StatusCode(StatusCodes.Status201Created, ApiResponse<T>.Ok(data, message ?? "Created successfully."));

    /// <summary>200 with ApiResponse&lt;PagedResult&lt;T&gt;&gt;.</summary>
    protected IActionResult Paged<T>(PagedResult<T> result) =>
        base.Ok(ApiResponse<PagedResult<T>>.Ok(result));

    /// <summary>200 with a message and null data.</summary>
    protected IActionResult Message(string message) =>
        base.Ok(ApiResponse<object?>.Ok(null, message));
}
