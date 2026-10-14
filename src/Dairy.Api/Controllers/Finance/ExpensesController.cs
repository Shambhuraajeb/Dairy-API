using Dairy.Api.Authorization;
using Dairy.Application.Interfaces.Finance;
using Dairy.Application.Requests.Finance;
using Dairy.Domain.Constants;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Dairy.Api.Controllers.Finance;

[Route("api/expenses")]
public class ExpensesController : ApiControllerBase
{
    private readonly IExpenseService _expenses;

    public ExpensesController(IExpenseService expenses) => _expenses = expenses;

    [HttpGet]
    [HasPermission(Permissions.ExpenseView)]
    public async Task<IActionResult> List([FromQuery] ExpenseQuery query, CancellationToken ct) =>
        Paged(await _expenses.GetPagedAsync(query, ct));

    [HttpGet("{id:long}")]
    [HasPermission(Permissions.ExpenseView)]
    public async Task<IActionResult> Get(long id, CancellationToken ct) =>
        Ok(await _expenses.GetByIdAsync(id, ct));

    /// <summary>Records a Pending expense (auto-Approved below system setting expense.autoApproveBelow).</summary>
    [HttpPost]
    [HasPermission(Permissions.ExpenseCreate)]
    public async Task<IActionResult> Create([FromBody] CreateExpenseRequest request, CancellationToken ct) =>
        Created(await _expenses.CreateAsync(request, ct), "Expense recorded.");

    /// <summary>Approves a Pending expense and posts a cash-out ledger entry. Body optional.</summary>
    [HttpPost("{id:long}/approve")]
    [HasPermission(Permissions.ExpenseApprove)]
    public async Task<IActionResult> Approve(long id, [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] ApproveExpenseRequest? request, CancellationToken ct) =>
        Ok(await _expenses.ApproveAsync(id, request, ct), "Expense approved.");

    [HttpPost("{id:long}/reject")]
    [HasPermission(Permissions.ExpenseApprove)]
    public async Task<IActionResult> Reject(long id, [FromBody] RejectExpenseRequest request, CancellationToken ct) =>
        Ok(await _expenses.RejectAsync(id, request, ct), "Expense rejected.");
}
