using Dairy.Application.Common;
using Dairy.Application.DTOs.Settlement;
using Dairy.Application.Requests.Settlement;

namespace Dairy.Application.Interfaces.Settlement;

public interface IFarmerSettlementRepository
{
    Task<FarmerSettlementPreviewDto> PreviewAsync(long farmerId, DateTime from, DateTime to, CancellationToken ct = default);
    Task<List<PendingSettlementFarmerDto>> GetPendingFarmersAsync(PendingFarmersQuery query, CancellationToken ct = default);
    Task<SettlementCreatedDto> CreateAsync(CreateSettlementRequest request, long userId, CancellationToken ct = default);
    Task<SettlementApprovedDto> ApproveAsync(long id, long userId, CancellationToken ct = default);
    Task<SettlementPaidDto> PayAsync(long id, PaySettlementRequest request, long userId, CancellationToken ct = default);
    Task<SettlementCancelledDto> CancelAsync(long id, string reason, long userId, CancellationToken ct = default);
    /// <summary>FarmerId of the payment, or null when it does not exist.</summary>
    Task<long?> GetPaymentFarmerIdAsync(long id, CancellationToken ct = default);
    Task<PagedResult<FarmerPaymentListDto>> GetPaymentsAsync(FarmerPaymentQuery query, CancellationToken ct = default);
    Task<FarmerPaymentDto?> GetPaymentByIdAsync(long id, CancellationToken ct = default);
}

public interface IFarmerSettlementService
{
    Task<FarmerSettlementPreviewDto> PreviewAsync(SettlementPreviewQuery query, CancellationToken ct = default);
    Task<List<PendingSettlementFarmerDto>> GetPendingFarmersAsync(PendingFarmersQuery query, CancellationToken ct = default);
    Task<SettlementCreatedDto> CreateAsync(CreateSettlementRequest request, CancellationToken ct = default);
    Task<SettlementApprovedDto> ApproveAsync(long id, CancellationToken ct = default);
    Task<SettlementPaidDto> PayAsync(long id, PaySettlementRequest request, CancellationToken ct = default);
    Task<SettlementCancelledDto> CancelAsync(long id, CancelSettlementRequest request, CancellationToken ct = default);
    Task<PagedResult<FarmerPaymentListDto>> GetPaymentsAsync(FarmerPaymentQuery query, CancellationToken ct = default);
    Task<FarmerPaymentDto> GetPaymentByIdAsync(long id, CancellationToken ct = default);
}
