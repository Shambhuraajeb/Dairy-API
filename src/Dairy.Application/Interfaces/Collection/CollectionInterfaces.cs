using Dairy.Application.Common;
using Dairy.Application.DTOs.Collection;
using Dairy.Application.Requests.Collection;

namespace Dairy.Application.Interfaces.Collection;

// ---------- shifts / dashboard ----------

public interface ICollectionShiftRepository
{
    Task<List<CollectionShiftDto>> GetShiftsAsync(long? centerId, bool? isActive, CancellationToken ct = default);
    /// <param name="shiftName">Null = whole day (all shifts).</param>
    Task<ShiftSummaryDto> GetSummaryAsync(DateTime date, long? centerId, string? shiftName, CancellationToken ct = default);
    /// <summary>One row per (shift, center) for the date, including shifts with no collections.</summary>
    Task<List<ShiftSummaryDto>> GetBreakdownAsync(DateTime date, long? centerId, CancellationToken ct = default);
    Task<int> GetPendingApprovalCountAsync(long? centerId, CancellationToken ct = default);
}

public interface ICollectionShiftService
{
    Task<List<CollectionShiftDto>> GetShiftsAsync(ShiftListQuery query, CancellationToken ct = default);
    Task<CollectionDashboardDto> GetDashboardAsync(ShiftDashboardQuery query, CancellationToken ct = default);
}

// ---------- milk collections ----------

public class QualityRecordResult
{
    public MilkQualityTestDto? Previous { get; set; }
    public MilkQualityTestDto Current { get; set; } = new();
}

/// <summary>Minimal state needed to guard quality recording.</summary>
public class CollectionStateDto
{
    public long Id { get; set; }
    public long FarmerId { get; set; }
    public string Status { get; set; } = string.Empty;
    public string SettlementStatus { get; set; } = string.Empty;
    public decimal Fat { get; set; }
    public decimal Snf { get; set; }
}

public interface IMilkCollectionRepository
{
    Task<PagedResult<MilkCollectionListDto>> GetPagedAsync(MilkCollectionQuery query, CancellationToken ct = default);
    Task<MilkCollectionDetailDto?> GetByIdAsync(long id, CancellationToken ct = default);
    Task<CollectionStateDto?> GetStateAsync(long id, CancellationToken ct = default);
    Task<MilkCollectionCreatedDto> CreateAsync(CreateMilkCollectionRequest request, long userId, CancellationToken ct = default);
    Task AdjustAsync(long id, AdjustMilkCollectionRequest request, long userId, CancellationToken ct = default);
    Task<MilkCollectionApprovedDto> ApproveAsync(long id, long userId, CancellationToken ct = default);
    Task<MilkCollectionRejectedDto> RejectAsync(long id, string reason, decimal? quantityLiters, long userId, CancellationToken ct = default);
    Task<List<MilkQualityTestDto>> GetQualityTestsAsync(long id, CancellationToken ct = default);
    Task<QualityRecordResult> RecordQualityAsync(long id, RecordQualityRequest request, decimal fat, decimal snf,
        string result, string? failureReason, long userId, CancellationToken ct = default);
}

public interface IMilkCollectionService
{
    Task<PagedResult<MilkCollectionListDto>> GetPagedAsync(MilkCollectionQuery query, CancellationToken ct = default);
    Task<PagedResult<MilkCollectionListDto>> GetPendingApprovalsAsync(MilkCollectionQuery query, CancellationToken ct = default);
    Task<MilkCollectionDetailDto> GetByIdAsync(long id, CancellationToken ct = default);
    Task<MilkCollectionCreatedDto> CreateAsync(CreateMilkCollectionRequest request, CancellationToken ct = default);
    Task<MilkCollectionDetailDto> AdjustAsync(long id, AdjustMilkCollectionRequest request, CancellationToken ct = default);
    Task<MilkCollectionApprovedDto> ApproveAsync(long id, CancellationToken ct = default);
    Task<MilkCollectionRejectedDto> RejectAsync(long id, RejectMilkCollectionRequest request, CancellationToken ct = default);
    Task<BulkApproveResultDto> BulkApproveAsync(BulkApproveRequest request, CancellationToken ct = default);
    Task<List<MilkQualityTestDto>> GetQualityTestsAsync(long id, CancellationToken ct = default);
    Task<MilkQualityTestDto> RecordQualityAsync(long id, RecordQualityRequest request, CancellationToken ct = default);
}

// ---------- rejections ----------

public interface IMilkRejectionRepository
{
    Task<PagedResult<MilkRejectionDto>> GetPagedAsync(MilkRejectionQuery query, CancellationToken ct = default);
}

public interface IMilkRejectionService
{
    Task<PagedResult<MilkRejectionDto>> GetPagedAsync(MilkRejectionQuery query, CancellationToken ct = default);
}

// ---------- milk tanks ----------

public interface IMilkTankRepository
{
    Task<PagedResult<MilkTankDto>> GetPagedAsync(MilkTankQuery query, CancellationToken ct = default);
    Task<MilkTankDto?> GetByIdAsync(long id, CancellationToken ct = default);
    Task<bool> CodeExistsAsync(string code, long? excludeId, CancellationToken ct = default);
    Task<bool> CenterExistsAsync(long centerId, CancellationToken ct = default);
    Task<long> CreateAsync(CreateMilkTankRequest request, long userId, CancellationToken ct = default);
    Task UpdateAsync(long id, UpdateMilkTankRequest request, bool isActive, long userId, CancellationToken ct = default);
    Task<PagedResult<TankTransactionDto>> GetTransactionsAsync(long tankId, TankTransactionQuery query, CancellationToken ct = default);
    /// <summary>Locks the tank, validates the balance, inserts the transaction and updates CurrentQuantity (one DB transaction).</summary>
    Task<TankTransactionDto> AddTransactionAsync(long tankId, string type, decimal quantity, string? destination, string? remarks,
        long userId, CancellationToken ct = default);
}

public interface IMilkTankService
{
    Task<PagedResult<MilkTankDto>> GetPagedAsync(MilkTankQuery query, CancellationToken ct = default);
    Task<MilkTankDto> GetByIdAsync(long id, CancellationToken ct = default);
    Task<MilkTankDto> CreateAsync(CreateMilkTankRequest request, CancellationToken ct = default);
    Task<MilkTankDto> UpdateAsync(long id, UpdateMilkTankRequest request, CancellationToken ct = default);
    Task<PagedResult<TankTransactionDto>> GetTransactionsAsync(long tankId, TankTransactionQuery query, CancellationToken ct = default);
    Task<TankTransactionDto> AddTransactionAsync(long tankId, CreateTankTransactionRequest request, CancellationToken ct = default);
}
