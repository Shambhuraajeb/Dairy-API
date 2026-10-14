using Dairy.Application.Common;
using Dairy.Application.DTOs.Animals;
using Dairy.Application.DTOs.Farmers;
using Dairy.Application.Requests.Animals;
using Dairy.Application.Requests.Farmers;

namespace Dairy.Application.Interfaces.Farmers;

public interface IFarmerService
{
    Task<PagedResult<FarmerListDto>> GetPagedAsync(FarmerQuery query, CancellationToken ct = default);
    Task<FarmerDetailDto> GetByIdAsync(long id, CancellationToken ct = default);
    Task<FarmerDetailDto> CreateAsync(CreateFarmerRequest request, CancellationToken ct = default);
    Task<FarmerDetailDto> UpdateAsync(long id, UpdateFarmerRequest request, CancellationToken ct = default);
    Task<FarmerDetailDto> VerifyAsync(long id, VerifyFarmerRequest? request, CancellationToken ct = default);
    Task<FarmerDetailDto> ActivateAsync(long id, CancellationToken ct = default);
    Task<FarmerDetailDto> DeactivateAsync(long id, FarmerStatusRequest request, CancellationToken ct = default);
    Task<FarmerSummaryDto> GetSummaryAsync(long id, CancellationToken ct = default);

    Task<PagedResult<AnimalListDto>> GetAnimalsAsync(long id, AnimalQuery query, CancellationToken ct = default);
    Task<PagedResult<FarmerCollectionRowDto>> GetCollectionsAsync(long id, FarmerSubListQuery query, CancellationToken ct = default);
    Task<PagedResult<FarmerPaymentRowDto>> GetPaymentsAsync(long id, FarmerSubListQuery query, CancellationToken ct = default);
    Task<PagedResult<FarmerLoanListDto>> GetLoansAsync(long id, FarmerLoanQuery query, CancellationToken ct = default);

    Task<List<FarmerAddressDto>> GetAddressesAsync(long id, CancellationToken ct = default);
    Task<FarmerAddressDto> AddAddressAsync(long id, FarmerAddressRequest request, CancellationToken ct = default);
    Task<FarmerAddressDto> UpdateAddressAsync(long id, long addressId, FarmerAddressRequest request, CancellationToken ct = default);
    Task DeleteAddressAsync(long id, long addressId, CancellationToken ct = default);

    Task<List<FarmerBankAccountDto>> GetBankAccountsAsync(long id, CancellationToken ct = default);
    Task<FarmerBankAccountDto> AddBankAccountAsync(long id, FarmerBankAccountRequest request, CancellationToken ct = default);
    Task<FarmerBankAccountDto> UpdateBankAccountAsync(long id, long bankAccountId, FarmerBankAccountRequest request, CancellationToken ct = default);
    Task DeleteBankAccountAsync(long id, long bankAccountId, CancellationToken ct = default);

    Task<List<FarmerDocumentDto>> GetDocumentsAsync(long id, CancellationToken ct = default);

    /// <summary>Stores an optional uploaded file (content may be null for a metadata-only record).</summary>
    Task<FarmerDocumentDto> AddDocumentAsync(long id, FarmerDocumentRequest request, Stream? content, string? uploadedFileName, string? contentType, long size, CancellationToken ct = default);
    Task<FarmerDocumentDto> UpdateDocumentAsync(long id, long documentId, UpdateFarmerDocumentRequest request, CancellationToken ct = default);
    Task DeleteDocumentAsync(long id, long documentId, CancellationToken ct = default);
    Task<FarmerDocumentFile> OpenDocumentAsync(long id, long documentId, CancellationToken ct = default);
}
