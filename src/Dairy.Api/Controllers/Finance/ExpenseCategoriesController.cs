using Dairy.Api.Authorization;
using Dairy.Application.Interfaces.Finance;
using Dairy.Application.Requests.Finance;
using Dairy.Domain.Constants;
using Microsoft.AspNetCore.Mvc;

namespace Dairy.Api.Controllers.Finance;

[Route("api/expense-categories")]
public class ExpenseCategoriesController : ApiControllerBase
{
    private readonly IExpenseCategoryService _categories;

    public ExpenseCategoriesController(IExpenseCategoryService categories) => _categories = categories;

    [HttpGet]
    [HasPermission(Permissions.ExpenseView)]
    public async Task<IActionResult> List([FromQuery] ExpenseCategoryQuery query, CancellationToken ct) =>
        Paged(await _categories.GetPagedAsync(query, ct));

    [HttpGet("{id:long}")]
    [HasPermission(Permissions.ExpenseView)]
    public async Task<IActionResult> Get(long id, CancellationToken ct) =>
        Ok(await _categories.GetByIdAsync(id, ct));

    /// <summary>Category master data is managed by users who can approve expenses.</summary>
    [HttpPost]
    [HasPermission(Permissions.ExpenseApprove)]
    public async Task<IActionResult> Create([FromBody] CreateExpenseCategoryRequest request, CancellationToken ct) =>
        Created(await _categories.CreateAsync(request, ct), "Expense category created.");

    /// <summary>Update; send isActive=false to deactivate.</summary>
    [HttpPut("{id:long}")]
    [HasPermission(Permissions.ExpenseApprove)]
    public async Task<IActionResult> Update(long id, [FromBody] UpdateExpenseCategoryRequest request, CancellationToken ct) =>
        Ok(await _categories.UpdateAsync(id, request, ct), "Expense category updated.");
}
