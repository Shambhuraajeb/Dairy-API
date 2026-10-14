using Dairy.Api.Authorization;
using Dairy.Application.Interfaces.Farmers;
using Dairy.Application.Requests.Animals;
using Dairy.Application.Requests.Farmers;
using Dairy.Domain.Constants;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Dairy.Api.Controllers.Farmers;

[Route("api/farmers")]
public class FarmersController : ApiControllerBase
{
    private readonly IFarmerService _farmers;

    public FarmersController(IFarmerService farmers) => _farmers = farmers;

    // ---- farmers

    [HttpGet]
    [HasPermission(Permissions.FarmerView)]
    public async Task<IActionResult> List([FromQuery] FarmerQuery query, CancellationToken ct) =>
        Paged(await _farmers.GetPagedAsync(query, ct));

    [HttpGet("{id:long}")]
    [HasPermission(Permissions.FarmerView)]
    public async Task<IActionResult> Get(long id, CancellationToken ct) =>
        Ok(await _farmers.GetByIdAsync(id, ct));

    [HttpPost]
    [HasPermission(Permissions.FarmerCreate)]
    public async Task<IActionResult> Create([FromBody] CreateFarmerRequest request, CancellationToken ct) =>
        Created(await _farmers.CreateAsync(request, ct), "Farmer registered.");

    [HttpPut("{id:long}")]
    [HasPermission(Permissions.FarmerUpdate)]
    public async Task<IActionResult> Update(long id, [FromBody] UpdateFarmerRequest request, CancellationToken ct) =>
        Ok(await _farmers.UpdateAsync(id, request, ct), "Farmer updated.");

    /// <summary>Body optional: { approved?: true, remarks? }. approved=false rejects the verification.</summary>
    [HttpPost("{id:long}/verify")]
    [HasPermission(Permissions.FarmerUpdate)]
    public async Task<IActionResult> Verify(
        long id,
        [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] VerifyFarmerRequest? request,
        CancellationToken ct) =>
        Ok(await _farmers.VerifyAsync(id, request, ct), "Farmer verification updated.");

    [HttpPost("{id:long}/activate")]
    [HasPermission(Permissions.FarmerUpdate)]
    public async Task<IActionResult> Activate(long id, CancellationToken ct) =>
        Ok(await _farmers.ActivateAsync(id, ct), "Farmer activated.");

    [HttpPost("{id:long}/deactivate")]
    [HasPermission(Permissions.FarmerUpdate)]
    public async Task<IActionResult> Deactivate(long id, [FromBody] FarmerStatusRequest request, CancellationToken ct) =>
        Ok(await _farmers.DeactivateAsync(id, request, ct), "Farmer deactivated.");

    [HttpGet("{id:long}/summary")]
    [HasPermission(Permissions.FarmerView)]
    public async Task<IActionResult> Summary(long id, CancellationToken ct) =>
        Ok(await _farmers.GetSummaryAsync(id, ct));

    // ---- detail-tab lists

    [HttpGet("{id:long}/animals")]
    [HasPermission(Permissions.FarmerView, Permissions.AnimalView)]
    public async Task<IActionResult> Animals(long id, [FromQuery] AnimalQuery query, CancellationToken ct) =>
        Paged(await _farmers.GetAnimalsAsync(id, query, ct));

    [HttpGet("{id:long}/collections")]
    [HasPermission(Permissions.FarmerView, Permissions.CollectionView)]
    public async Task<IActionResult> Collections(long id, [FromQuery] FarmerSubListQuery query, CancellationToken ct) =>
        Paged(await _farmers.GetCollectionsAsync(id, query, ct));

    [HttpGet("{id:long}/payments")]
    [HasPermission(Permissions.FarmerView, Permissions.PaymentView)]
    public async Task<IActionResult> Payments(long id, [FromQuery] FarmerSubListQuery query, CancellationToken ct) =>
        Paged(await _farmers.GetPaymentsAsync(id, query, ct));

    [HttpGet("{id:long}/loans")]
    [HasPermission(Permissions.FarmerView, Permissions.LoanView)]
    public async Task<IActionResult> Loans(long id, [FromQuery] FarmerLoanQuery query, CancellationToken ct) =>
        Paged(await _farmers.GetLoansAsync(id, query, ct));

    // ---- addresses

    [HttpGet("{id:long}/addresses")]
    [HasPermission(Permissions.FarmerView)]
    public async Task<IActionResult> Addresses(long id, CancellationToken ct) =>
        Ok(await _farmers.GetAddressesAsync(id, ct));

    [HttpPost("{id:long}/addresses")]
    [HasPermission(Permissions.FarmerUpdate)]
    public async Task<IActionResult> AddAddress(long id, [FromBody] FarmerAddressRequest request, CancellationToken ct) =>
        Created(await _farmers.AddAddressAsync(id, request, ct), "Address added.");

    [HttpPut("{id:long}/addresses/{addressId:long}")]
    [HasPermission(Permissions.FarmerUpdate)]
    public async Task<IActionResult> UpdateAddress(long id, long addressId, [FromBody] FarmerAddressRequest request, CancellationToken ct) =>
        Ok(await _farmers.UpdateAddressAsync(id, addressId, request, ct), "Address updated.");

    [HttpDelete("{id:long}/addresses/{addressId:long}")]
    [HasPermission(Permissions.FarmerUpdate)]
    public async Task<IActionResult> DeleteAddress(long id, long addressId, CancellationToken ct)
    {
        await _farmers.DeleteAddressAsync(id, addressId, ct);
        return Message("Address deleted.");
    }

    // ---- bank accounts

    [HttpGet("{id:long}/bank-accounts")]
    [HasPermission(Permissions.FarmerView)]
    public async Task<IActionResult> BankAccounts(long id, CancellationToken ct) =>
        Ok(await _farmers.GetBankAccountsAsync(id, ct));

    [HttpPost("{id:long}/bank-accounts")]
    [HasPermission(Permissions.FarmerUpdate)]
    public async Task<IActionResult> AddBankAccount(long id, [FromBody] FarmerBankAccountRequest request, CancellationToken ct) =>
        Created(await _farmers.AddBankAccountAsync(id, request, ct), "Bank account added.");

    [HttpPut("{id:long}/bank-accounts/{bankAccountId:long}")]
    [HasPermission(Permissions.FarmerUpdate)]
    public async Task<IActionResult> UpdateBankAccount(long id, long bankAccountId, [FromBody] FarmerBankAccountRequest request, CancellationToken ct) =>
        Ok(await _farmers.UpdateBankAccountAsync(id, bankAccountId, request, ct), "Bank account updated.");

    [HttpDelete("{id:long}/bank-accounts/{bankAccountId:long}")]
    [HasPermission(Permissions.FarmerUpdate)]
    public async Task<IActionResult> DeleteBankAccount(long id, long bankAccountId, CancellationToken ct)
    {
        await _farmers.DeleteBankAccountAsync(id, bankAccountId, ct);
        return Message("Bank account deleted.");
    }

    // ---- documents

    [HttpGet("{id:long}/documents")]
    [HasPermission(Permissions.FarmerView)]
    public async Task<IActionResult> Documents(long id, CancellationToken ct) =>
        Ok(await _farmers.GetDocumentsAsync(id, ct));

    /// <summary>multipart/form-data: optional file (pdf/jpg/png, max 10 MB) plus documentTypeId, documentNumber, fileName, expiryDate, remarks.</summary>
    [HttpPost("{id:long}/documents")]
    [HasPermission(Permissions.FarmerUpdate)]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> AddDocument(long id, [FromForm] FarmerDocumentRequest request, CancellationToken ct)
    {
        var file = HttpContext.Request.Form.Files.FirstOrDefault();
        await using var stream = file?.OpenReadStream();
        var created = await _farmers.AddDocumentAsync(id, request, stream, file?.FileName, file?.ContentType, file?.Length ?? 0, ct);
        return Created(created, "Document added.");
    }

    [HttpPut("{id:long}/documents/{documentId:long}")]
    [HasPermission(Permissions.FarmerUpdate)]
    public async Task<IActionResult> UpdateDocument(long id, long documentId, [FromBody] UpdateFarmerDocumentRequest request, CancellationToken ct) =>
        Ok(await _farmers.UpdateDocumentAsync(id, documentId, request, ct), "Document updated.");

    [HttpDelete("{id:long}/documents/{documentId:long}")]
    [HasPermission(Permissions.FarmerUpdate)]
    public async Task<IActionResult> DeleteDocument(long id, long documentId, CancellationToken ct)
    {
        await _farmers.DeleteDocumentAsync(id, documentId, ct);
        return Message("Document deleted.");
    }

    [HttpGet("{id:long}/documents/{documentId:long}/download")]
    [HasPermission(Permissions.FarmerView)]
    public async Task<IActionResult> DownloadDocument(long id, long documentId, CancellationToken ct)
    {
        var file = await _farmers.OpenDocumentAsync(id, documentId, ct);
        return File(file.Content, file.ContentType, file.FileName);
    }
}
