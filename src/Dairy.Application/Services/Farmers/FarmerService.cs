using Dairy.Application.Common;
using Dairy.Application.DTOs.Animals;
using Dairy.Application.DTOs.Farmers;
using Dairy.Application.Interfaces;
using Dairy.Application.Interfaces.Animals;
using Dairy.Application.Interfaces.Farmers;
using Dairy.Application.Requests.Animals;
using Dairy.Application.Requests.Farmers;
using Dairy.Domain.Constants;

namespace Dairy.Application.Services.Farmers;

public class FarmerService : IFarmerService
{
    private const long MaxUploadBytes = 10 * 1024 * 1024;
    private static readonly string[] AllowedExtensions = { ".pdf", ".jpg", ".jpeg", ".png" };

    private const string Verified = "Verified";
    private const string Rejected = "Rejected";
    private const string Active = "Active";
    private const string Inactive = "Inactive";

    private readonly IFarmerRepository _repo;
    private readonly IAnimalRepository _animals;
    private readonly IFarmerLoanRepository _loans;
    private readonly IAuditService _audit;
    private readonly ICurrentUser _user;
    private readonly IFileStorage _files;

    public FarmerService(
        IFarmerRepository repo,
        IAnimalRepository animals,
        IFarmerLoanRepository loans,
        IAuditService audit,
        ICurrentUser user,
        IFileStorage files)
    {
        _repo = repo;
        _animals = animals;
        _loans = loans;
        _audit = audit;
        _user = user;
        _files = files;
    }

    // ------------------------------------------------------------------ farmers

    public Task<PagedResult<FarmerListDto>> GetPagedAsync(FarmerQuery query, CancellationToken ct = default)
    {
        if (_user.IsInRole(Roles.Farmer))
            query.FarmerId = _user.FarmerId ?? throw new ForbiddenException();
        return _repo.GetPagedAsync(query, ct);
    }

    public async Task<FarmerDetailDto> GetByIdAsync(long id, CancellationToken ct = default)
    {
        await EnsureAccessAsync(id, ct);
        return await _repo.GetDetailAsync(id, ct) ?? throw new NotFoundException("Farmer", id);
    }

    public async Task<FarmerDetailDto> CreateAsync(CreateFarmerRequest request, CancellationToken ct = default)
    {
        var joinDate = (request.JoinDate ?? DateTime.Today).Date;
        if (joinDate > DateTime.Today)
            throw new RequestValidationException("joinDate", "Join date cannot be in the future.");

        string code;
        if (!string.IsNullOrWhiteSpace(request.FarmerCode))
        {
            code = request.FarmerCode.Trim();
            if (await _repo.CodeExistsAsync(code, ct))
                throw new ConflictException($"A farmer with code '{code}' already exists.");
        }
        else
        {
            code = await _repo.GenerateCodeAsync(ct);
        }

        var id = await _repo.CreateAsync(request, code, joinDate, _user.UserId, ct);
        var created = await _repo.GetDetailAsync(id, ct) ?? throw new NotFoundException("Farmer", id);

        await _audit.LogAsync(AuditActions.Create, "Farmer", id, null, ToAudit(created), ct);
        return created;
    }

    public async Task<FarmerDetailDto> UpdateAsync(long id, UpdateFarmerRequest request, CancellationToken ct = default)
    {
        var existing = await EnsureAccessAsync(id, ct);

        DateTime? joinDate = request.JoinDate?.Date;
        if (joinDate.HasValue && joinDate.Value > DateTime.Today)
            throw new RequestValidationException("joinDate", "Join date cannot be in the future.");

        // An active farmer must stay serviceable.
        if (existing.Status == Active && request.CollectionCenterId is null)
            throw new BusinessRuleException("An active farmer must have a collection center.");

        await _repo.UpdateAsync(id, request, joinDate, _user.UserId, ct);
        var updated = await _repo.GetDetailAsync(id, ct) ?? throw new NotFoundException("Farmer", id);

        await _audit.LogAsync(AuditActions.Update, "Farmer", id, existing, ToAudit(updated), ct);
        return updated;
    }

    public async Task<FarmerDetailDto> VerifyAsync(long id, VerifyFarmerRequest? request, CancellationToken ct = default)
    {
        request ??= new VerifyFarmerRequest();
        var existing = await EnsureAccessAsync(id, ct);
        var target = request.Approved ? Verified : Rejected;

        if (existing.VerificationStatus == target)
            throw new BusinessRuleException($"Farmer is already {target.ToLowerInvariant()}.");

        await _repo.SetVerificationAsync(id, target, request.Remarks, _user.UserId, ct);
        var updated = await _repo.GetDetailAsync(id, ct) ?? throw new NotFoundException("Farmer", id);

        await _audit.LogAsync(request.Approved ? AuditActions.Approve : AuditActions.Reject, "Farmer", id, existing, ToAudit(updated), ct);
        return updated;
    }

    public async Task<FarmerDetailDto> ActivateAsync(long id, CancellationToken ct = default)
    {
        var existing = await EnsureAccessAsync(id, ct);

        if (existing.Status == Active)
            throw new BusinessRuleException("Farmer is already active.");
        if (existing.VerificationStatus != Verified)
            throw new BusinessRuleException("The farmer must be verified before activation.");
        if (existing.CollectionCenterId is null)
            throw new BusinessRuleException("Assign a collection center before activating the farmer.");
        if (await _repo.CountActiveBankAccountsAsync(id, ct) == 0)
            throw new BusinessRuleException("Add a bank account before activating the farmer.");

        await _repo.ActivateAsync(id, _user.UserId, ct);
        var updated = await _repo.GetDetailAsync(id, ct) ?? throw new NotFoundException("Farmer", id);

        await _audit.LogAsync(AuditActions.Activate, "Farmer", id, existing, ToAudit(updated), ct);
        return updated;
    }

    public async Task<FarmerDetailDto> DeactivateAsync(long id, FarmerStatusRequest request, CancellationToken ct = default)
    {
        var existing = await EnsureAccessAsync(id, ct);

        if (string.IsNullOrWhiteSpace(request.Reason))
            throw new RequestValidationException("reason", "A reason is required to deactivate a farmer.");
        if (existing.Status == Inactive)
            throw new BusinessRuleException("Farmer is already inactive.");

        await _repo.DeactivateAsync(id, request.Reason.Trim(), _user.UserId, ct);
        var updated = await _repo.GetDetailAsync(id, ct) ?? throw new NotFoundException("Farmer", id);

        await _audit.LogAsync(AuditActions.Deactivate, "Farmer", id, existing, ToAudit(updated), ct);
        return updated;
    }

    public async Task<FarmerSummaryDto> GetSummaryAsync(long id, CancellationToken ct = default)
    {
        await EnsureAccessAsync(id, ct);
        var monthStart = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
        var monthEnd = monthStart.AddMonths(1).AddDays(-1);
        return await _repo.GetSummaryAsync(id, monthStart, monthEnd, ct) ?? throw new NotFoundException("Farmer", id);
    }

    // ------------------------------------------------------------------ detail-tab lists

    public async Task<PagedResult<AnimalListDto>> GetAnimalsAsync(long id, AnimalQuery query, CancellationToken ct = default)
    {
        await EnsureAccessAsync(id, ct);
        query.FarmerId = id;
        return await _animals.GetPagedAsync(query, ct);
    }

    public async Task<PagedResult<FarmerCollectionRowDto>> GetCollectionsAsync(long id, FarmerSubListQuery query, CancellationToken ct = default)
    {
        await EnsureAccessAsync(id, ct);
        return await _repo.GetCollectionsPagedAsync(id, query, ct);
    }

    public async Task<PagedResult<FarmerPaymentRowDto>> GetPaymentsAsync(long id, FarmerSubListQuery query, CancellationToken ct = default)
    {
        await EnsureAccessAsync(id, ct);
        return await _repo.GetPaymentsPagedAsync(id, query, ct);
    }

    public async Task<PagedResult<FarmerLoanListDto>> GetLoansAsync(long id, FarmerLoanQuery query, CancellationToken ct = default)
    {
        await EnsureAccessAsync(id, ct);
        query.FarmerId = id;
        return await _loans.GetPagedAsync(query, ct);
    }

    // ------------------------------------------------------------------ addresses

    public async Task<List<FarmerAddressDto>> GetAddressesAsync(long id, CancellationToken ct = default)
    {
        await EnsureAccessAsync(id, ct);
        return await _repo.GetAddressesAsync(id, ct);
    }

    public async Task<FarmerAddressDto> AddAddressAsync(long id, FarmerAddressRequest request, CancellationToken ct = default)
    {
        await EnsureAccessAsync(id, ct);
        var addressId = await _repo.AddAddressAsync(id, request, _user.UserId, ct);
        var created = await _repo.GetAddressAsync(id, addressId, ct) ?? throw new NotFoundException("Address", addressId);

        await _audit.LogAsync(AuditActions.Create, "FarmerAddress", addressId, null, created, ct);
        return created;
    }

    public async Task<FarmerAddressDto> UpdateAddressAsync(long id, long addressId, FarmerAddressRequest request, CancellationToken ct = default)
    {
        await EnsureAccessAsync(id, ct);
        var existing = await _repo.GetAddressAsync(id, addressId, ct) ?? throw new NotFoundException("Address", addressId);

        await _repo.UpdateAddressAsync(id, addressId, request, _user.UserId, ct);
        var updated = await _repo.GetAddressAsync(id, addressId, ct) ?? throw new NotFoundException("Address", addressId);

        await _audit.LogAsync(AuditActions.Update, "FarmerAddress", addressId, existing, updated, ct);
        return updated;
    }

    public async Task DeleteAddressAsync(long id, long addressId, CancellationToken ct = default)
    {
        await EnsureAccessAsync(id, ct);
        var existing = await _repo.GetAddressAsync(id, addressId, ct) ?? throw new NotFoundException("Address", addressId);

        await _repo.DeleteAddressAsync(id, addressId, _user.UserId, ct);
        await _audit.LogAsync(AuditActions.Delete, "FarmerAddress", addressId, existing, null, ct);
    }

    // ------------------------------------------------------------------ bank accounts

    public async Task<List<FarmerBankAccountDto>> GetBankAccountsAsync(long id, CancellationToken ct = default)
    {
        await EnsureAccessAsync(id, ct);
        return await _repo.GetBankAccountsAsync(id, ct);
    }

    public async Task<FarmerBankAccountDto> AddBankAccountAsync(long id, FarmerBankAccountRequest request, CancellationToken ct = default)
    {
        await EnsureAccessAsync(id, ct);

        var number = request.AccountNumber.Trim();
        if (await _repo.BankAccountNumberExistsAsync(id, number, null, ct))
            throw new ConflictException("This account number is already registered for the farmer.");

        var accountId = await _repo.AddBankAccountAsync(id, request, _user.UserId, ct);
        var created = await _repo.GetBankAccountAsync(id, accountId, ct) ?? throw new NotFoundException("Bank account", accountId);

        await _audit.LogAsync(AuditActions.Create, "FarmerBankAccount", accountId, null, Mask(created), ct);
        return created;
    }

    public async Task<FarmerBankAccountDto> UpdateBankAccountAsync(long id, long bankAccountId, FarmerBankAccountRequest request, CancellationToken ct = default)
    {
        await EnsureAccessAsync(id, ct);
        var existing = await _repo.GetBankAccountAsync(id, bankAccountId, ct) ?? throw new NotFoundException("Bank account", bankAccountId);

        var number = request.AccountNumber.Trim();
        if (await _repo.BankAccountNumberExistsAsync(id, number, bankAccountId, ct))
            throw new ConflictException("This account number is already registered for the farmer.");

        await _repo.UpdateBankAccountAsync(id, bankAccountId, request, _user.UserId, ct);
        var updated = await _repo.GetBankAccountAsync(id, bankAccountId, ct) ?? throw new NotFoundException("Bank account", bankAccountId);

        await _audit.LogAsync(AuditActions.Update, "FarmerBankAccount", bankAccountId, Mask(existing), Mask(updated), ct);
        return updated;
    }

    public async Task DeleteBankAccountAsync(long id, long bankAccountId, CancellationToken ct = default)
    {
        var farmer = await EnsureAccessAsync(id, ct);
        var existing = await _repo.GetBankAccountAsync(id, bankAccountId, ct) ?? throw new NotFoundException("Bank account", bankAccountId);

        if (farmer.Status == Active && await _repo.CountActiveBankAccountsAsync(id, ct) <= 1)
            throw new BusinessRuleException("An active farmer must keep at least one bank account.");

        await _repo.DeleteBankAccountAsync(id, bankAccountId, _user.UserId, ct);
        await _audit.LogAsync(AuditActions.Delete, "FarmerBankAccount", bankAccountId, Mask(existing), null, ct);
    }

    // ------------------------------------------------------------------ documents

    public async Task<List<FarmerDocumentDto>> GetDocumentsAsync(long id, CancellationToken ct = default)
    {
        await EnsureAccessAsync(id, ct);
        return await _repo.GetDocumentsAsync(id, ct);
    }

    public async Task<FarmerDocumentDto> AddDocumentAsync(
        long id, FarmerDocumentRequest request, Stream? content, string? uploadedFileName, string? contentType, long size,
        CancellationToken ct = default)
    {
        await EnsureAccessAsync(id, ct);

        var hasFile = content is not null && size > 0;
        if (!hasFile && string.IsNullOrWhiteSpace(request.FileName))
            throw new RequestValidationException("file", "Upload a file or provide a file name.");

        string fileName = request.FileName?.Trim() ?? string.Empty;
        string filePath = string.Empty;
        long? fileSize = null;

        if (hasFile)
        {
            if (size > MaxUploadBytes)
                throw new BusinessRuleException("File size must not exceed 10 MB.");

            var extension = Path.GetExtension(uploadedFileName ?? string.Empty).ToLowerInvariant();
            if (!AllowedExtensions.Contains(extension))
                throw new BusinessRuleException("Only PDF, JPG, JPEG and PNG files are allowed.");

            var stored = await _files.SaveAsync(content!, uploadedFileName ?? "document" + extension, $"farmers/{id}", ct);
            filePath = stored.RelativePath;
            fileName = string.IsNullOrWhiteSpace(request.FileName) ? stored.OriginalFileName : request.FileName.Trim();
            fileSize = stored.Size;
        }

        long documentId;
        try
        {
            documentId = await _repo.AddDocumentAsync(
                new FarmerDocumentInsert(id, request.DocumentTypeId, request.DocumentNumber, fileName, filePath,
                    hasFile ? contentType : null, fileSize, request.ExpiryDate, request.Remarks),
                _user.UserId, ct);
        }
        catch
        {
            if (filePath.Length > 0) _files.Delete(filePath);
            throw;
        }

        var created = await _repo.GetDocumentAsync(id, documentId, ct) ?? throw new NotFoundException("Document", documentId);
        await _audit.LogAsync(AuditActions.Create, "FarmerDocument", documentId, null, created, ct);
        return created;
    }

    public async Task<FarmerDocumentDto> UpdateDocumentAsync(long id, long documentId, UpdateFarmerDocumentRequest request, CancellationToken ct = default)
    {
        await EnsureAccessAsync(id, ct);
        var existing = await _repo.GetDocumentAsync(id, documentId, ct) ?? throw new NotFoundException("Document", documentId);

        await _repo.UpdateDocumentAsync(id, documentId, request, _user.UserId, ct);
        var updated = await _repo.GetDocumentAsync(id, documentId, ct) ?? throw new NotFoundException("Document", documentId);

        await _audit.LogAsync(AuditActions.Update, "FarmerDocument", documentId, existing, updated, ct);
        return updated;
    }

    public async Task DeleteDocumentAsync(long id, long documentId, CancellationToken ct = default)
    {
        await EnsureAccessAsync(id, ct);
        var existing = await _repo.GetDocumentAsync(id, documentId, ct) ?? throw new NotFoundException("Document", documentId);

        await _repo.DeleteDocumentAsync(id, documentId, _user.UserId, ct);
        if (existing.HasFile && !string.IsNullOrEmpty(existing.FilePath))
            _files.Delete(existing.FilePath);

        await _audit.LogAsync(AuditActions.Delete, "FarmerDocument", documentId, existing, null, ct);
    }

    public async Task<FarmerDocumentFile> OpenDocumentAsync(long id, long documentId, CancellationToken ct = default)
    {
        await EnsureAccessAsync(id, ct);
        var doc = await _repo.GetDocumentAsync(id, documentId, ct) ?? throw new NotFoundException("Document", documentId);
        if (!doc.HasFile || string.IsNullOrEmpty(doc.FilePath))
            throw new NotFoundException("This document has no uploaded file.");

        var stream = _files.OpenRead(doc.FilePath) ?? throw new NotFoundException("The stored file could not be found.");
        return new FarmerDocumentFile
        {
            Content = stream,
            ContentType = string.IsNullOrWhiteSpace(doc.ContentType) ? "application/octet-stream" : doc.ContentType,
            FileName = doc.FileName
        };
    }

    // ------------------------------------------------------------------ helpers

    /// <summary>Loads the farmer header, hiding other farmers from users with the Farmer role.</summary>
    private async Task<FarmerListDto> EnsureAccessAsync(long id, CancellationToken ct)
    {
        if (_user.IsInRole(Roles.Farmer) && _user.FarmerId != id)
            throw new NotFoundException("Farmer", id);

        return await _repo.GetBasicAsync(id, ct) ?? throw new NotFoundException("Farmer", id);
    }

    private static object ToAudit(FarmerDetailDto f) => new
    {
        f.Id, f.FarmerCode, f.FullName, f.FatherName, f.Mobile, f.AlternateMobile, f.Email, f.Gender, f.DateOfBirth,
        f.CollectionCenterId, f.BranchId, f.MilkType, f.JoinDate, f.VerificationStatus, f.Status, f.StatusReason, f.Notes
    };

    private static FarmerBankAccountDto Mask(FarmerBankAccountDto b)
    {
        var number = b.AccountNumber ?? string.Empty;
        var masked = number.Length <= 4 ? number : new string('X', number.Length - 4) + number[^4..];
        return new FarmerBankAccountDto
        {
            Id = b.Id,
            FarmerId = b.FarmerId,
            AccountHolderName = b.AccountHolderName,
            BankName = b.BankName,
            BranchName = b.BranchName,
            AccountNumber = masked,
            IfscCode = b.IfscCode,
            UpiId = b.UpiId,
            IsPrimary = b.IsPrimary
        };
    }
}
