using Dairy.Application.Common;
using Dairy.Application.DTOs.Farmers;
using Dairy.Application.Requests.Farmers;

namespace Dairy.Application.Interfaces.Farmers;

/// <summary>Values stored for a farmer document row.</summary>
public record FarmerDocumentInsert(
    long FarmerId,
    long? DocumentTypeId,
    string? DocumentNumber,
    string FileName,
    string FilePath,
    string? ContentType,
    long? FileSize,
    DateTime? ExpiryDate,
    string? Remarks);

public interface IFarmerRepository
{
    Task<PagedResult<FarmerListDto>> GetPagedAsync(FarmerQuery query, CancellationToken ct = default);

    /// <summary>Header fields only (no addresses / bank accounts / documents).</summary>
    Task<FarmerListDto?> GetBasicAsync(long id, CancellationToken ct = default);

    /// <summary>Full farmer including addresses, bank accounts and documents.</summary>
    Task<FarmerDetailDto?> GetDetailAsync(long id, CancellationToken ct = default);

    Task<bool> CodeExistsAsync(string code, CancellationToken ct = default);
    Task<string> GenerateCodeAsync(CancellationToken ct = default);

    /// <summary>Inserts the farmer plus optional address and bank account in one transaction; returns the new id.</summary>
    Task<long> CreateAsync(CreateFarmerRequest request, string farmerCode, DateTime joinDate, long userId, CancellationToken ct = default);

    Task UpdateAsync(long id, UpdateFarmerRequest request, DateTime? joinDate, long userId, CancellationToken ct = default);
    Task SetVerificationAsync(long id, string verificationStatus, string? remarks, long userId, CancellationToken ct = default);
    Task ActivateAsync(long id, long userId, CancellationToken ct = default);
    Task DeactivateAsync(long id, string reason, long userId, CancellationToken ct = default);
    Task<int> CountActiveBankAccountsAsync(long farmerId, CancellationToken ct = default);

    Task<FarmerSummaryDto?> GetSummaryAsync(long farmerId, DateTime monthStart, DateTime monthEnd, CancellationToken ct = default);
    Task<PagedResult<FarmerCollectionRowDto>> GetCollectionsPagedAsync(long farmerId, FarmerSubListQuery query, CancellationToken ct = default);
    Task<PagedResult<FarmerPaymentRowDto>> GetPaymentsPagedAsync(long farmerId, FarmerSubListQuery query, CancellationToken ct = default);

    // addresses
    Task<List<FarmerAddressDto>> GetAddressesAsync(long farmerId, CancellationToken ct = default);
    Task<FarmerAddressDto?> GetAddressAsync(long farmerId, long addressId, CancellationToken ct = default);
    Task<long> AddAddressAsync(long farmerId, FarmerAddressRequest request, long userId, CancellationToken ct = default);
    Task UpdateAddressAsync(long farmerId, long addressId, FarmerAddressRequest request, long userId, CancellationToken ct = default);
    Task DeleteAddressAsync(long farmerId, long addressId, long userId, CancellationToken ct = default);

    // bank accounts
    Task<List<FarmerBankAccountDto>> GetBankAccountsAsync(long farmerId, CancellationToken ct = default);
    Task<FarmerBankAccountDto?> GetBankAccountAsync(long farmerId, long bankAccountId, CancellationToken ct = default);
    Task<bool> BankAccountNumberExistsAsync(long farmerId, string accountNumber, long? excludeId, CancellationToken ct = default);
    Task<long> AddBankAccountAsync(long farmerId, FarmerBankAccountRequest request, long userId, CancellationToken ct = default);
    Task UpdateBankAccountAsync(long farmerId, long bankAccountId, FarmerBankAccountRequest request, long userId, CancellationToken ct = default);
    Task DeleteBankAccountAsync(long farmerId, long bankAccountId, long userId, CancellationToken ct = default);

    // documents
    Task<List<FarmerDocumentDto>> GetDocumentsAsync(long farmerId, CancellationToken ct = default);
    Task<FarmerDocumentDto?> GetDocumentAsync(long farmerId, long documentId, CancellationToken ct = default);
    Task<long> AddDocumentAsync(FarmerDocumentInsert document, long userId, CancellationToken ct = default);
    Task UpdateDocumentAsync(long farmerId, long documentId, UpdateFarmerDocumentRequest request, long userId, CancellationToken ct = default);
    Task DeleteDocumentAsync(long farmerId, long documentId, long userId, CancellationToken ct = default);
}
