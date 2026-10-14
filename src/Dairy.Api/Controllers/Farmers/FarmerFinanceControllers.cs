using Dairy.Api.Authorization;
using Dairy.Application.Interfaces.Farmers;
using Dairy.Application.Requests.Farmers;
using Dairy.Domain.Constants;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Dairy.Api.Controllers.Farmers;

[Route("api/farmer-advances")]
public class FarmerAdvancesController : ApiControllerBase
{
    private readonly IFarmerAdvanceService _advances;

    public FarmerAdvancesController(IFarmerAdvanceService advances) => _advances = advances;

    [HttpGet]
    [HasPermission(Permissions.LoanView)]
    public async Task<IActionResult> List([FromQuery] FarmerAdvanceQuery query, CancellationToken ct) =>
        Paged(await _advances.GetPagedAsync(query, ct));

    [HttpGet("{id:long}")]
    [HasPermission(Permissions.LoanView)]
    public async Task<IActionResult> Get(long id, CancellationToken ct) =>
        Ok(await _advances.GetByIdAsync(id, ct));

    [HttpPost]
    [HasPermission(Permissions.LoanCreate)]
    public async Task<IActionResult> Create([FromBody] CreateFarmerAdvanceRequest request, CancellationToken ct) =>
        Created(await _advances.CreateAsync(request, ct), "Advance recorded.");
}

[Route("api/farmer-loans")]
public class FarmerLoansController : ApiControllerBase
{
    private readonly IFarmerLoanService _loans;

    public FarmerLoansController(IFarmerLoanService loans) => _loans = loans;

    [HttpGet]
    [HasPermission(Permissions.LoanView)]
    public async Task<IActionResult> List([FromQuery] FarmerLoanQuery query, CancellationToken ct) =>
        Paged(await _loans.GetPagedAsync(query, ct));

    [HttpGet("{id:long}")]
    [HasPermission(Permissions.LoanView)]
    public async Task<IActionResult> Get(long id, CancellationToken ct) =>
        Ok(await _loans.GetByIdAsync(id, ct));

    [HttpPost]
    [HasPermission(Permissions.LoanCreate)]
    public async Task<IActionResult> Create([FromBody] CreateFarmerLoanRequest request, CancellationToken ct) =>
        Created(await _loans.CreateAsync(request, ct), "Loan created.");

    /// <summary>Body optional: { amount? }. Omit amount to pay the remaining balance of the installment.</summary>
    [HttpPost("{id:long}/installments/{installmentNo:int}/pay")]
    [HasPermission(Permissions.LoanUpdate)]
    public async Task<IActionResult> PayInstallment(
        long id,
        int installmentNo,
        [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] PayLoanInstallmentRequest? request,
        CancellationToken ct) =>
        Ok(await _loans.PayInstallmentAsync(id, installmentNo, request, ct), "Installment paid.");
}
