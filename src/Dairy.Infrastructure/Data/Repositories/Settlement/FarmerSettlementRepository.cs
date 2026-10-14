using Dairy.Application.Common;
using Dairy.Application.DTOs.Settlement;
using Dairy.Application.Interfaces;
using Dairy.Application.Interfaces.Settlement;
using Dairy.Application.Requests.Settlement;

namespace Dairy.Infrastructure.Data.Repositories.Settlement;

public class FarmerSettlementRepository : IFarmerSettlementRepository
{
    private static readonly Dictionary<string, string> SortColumns = new(StringComparer.OrdinalIgnoreCase)
    {
        ["id"] = "fp.FarmerPaymentId",
        ["paymentNumber"] = "fp.PaymentNumber",
        ["farmerCode"] = "f.FarmerCode",
        ["farmerName"] = "f.FullName",
        ["paymentDate"] = "fp.PaymentDate",
        ["periodFrom"] = "fp.PeriodFrom",
        ["periodTo"] = "fp.PeriodTo",
        ["grossAmount"] = "fp.GrossAmount",
        ["deductionAmount"] = "fp.DeductionAmount",
        ["netPayable"] = "fp.NetPayable",
        ["paidAmount"] = "fp.PaidAmount",
        ["balanceAmount"] = "fp.BalanceAmount",
        ["status"] = "fp.Status",
        ["createdDate"] = "fp.CreatedDate"
    };

    private const string PaymentColumns =
        "fp.FarmerPaymentId AS Id, fp.PaymentNumber, fp.FarmerId, f.FarmerCode, f.FullName AS FarmerName, " +
        "fp.CollectionCenterId, cc.Name AS CollectionCenterName, fp.PaymentDate, fp.PeriodFrom, fp.PeriodTo, " +
        "fp.TotalCollections, fp.TotalQuantity, fp.GrossAmount, fp.BonusAmount, fp.PenaltyAmount, fp.DeductionAmount, " +
        "fp.NetPayable, fp.PaidAmount, fp.BalanceAmount, fp.Status, fp.PaymentModeId, pm.Name AS PaymentModeName, " +
        "fp.ReferenceNo, fp.PaidDate, fp.ApprovedDate, fp.CreatedDate";

    private const string PaymentFromJoins =
        " FROM `FarmerPayment` fp" +
        " JOIN `Farmer` f ON f.FarmerId = fp.FarmerId" +
        " LEFT JOIN `CollectionCenter` cc ON cc.CollectionCenterId = fp.CollectionCenterId" +
        " LEFT JOIN `PaymentMode` pm ON pm.PaymentModeId = fp.PaymentModeId";

    private const string PaymentWhere =
        " WHERE fp.IsActive = 1" +
        " AND (@FarmerId IS NULL OR fp.FarmerId = @FarmerId)" +
        " AND (@CenterId IS NULL OR fp.CollectionCenterId = @CenterId)" +
        " AND (@Status IS NULL OR fp.Status = @Status)" +
        " AND (@DateFrom IS NULL OR fp.PaymentDate >= @DateFrom)" +
        " AND (@DateTo IS NULL OR fp.PaymentDate <= @DateTo)" +
        " AND (@Search IS NULL OR fp.PaymentNumber LIKE @Search OR f.FullName LIKE @Search OR f.FarmerCode LIKE @Search)";

    private readonly IDbConnectionFactory _db;

    public FarmerSettlementRepository(IDbConnectionFactory db) => _db = db;

    public async Task<FarmerSettlementPreviewDto> PreviewAsync(long farmerId, DateTime from, DateTime to, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        using var grid = await conn.QueryMultipleSpAsync("sp_GetFarmerSettlement",
            new { p_FarmerId = farmerId, p_FromDate = from.Date, p_ToDate = to.Date }, ct);

        var result = new FarmerSettlementPreviewDto
        {
            Summary = await grid.ReadFirstOrDefaultAsync<SettlementSummaryDto>() ?? new SettlementSummaryDto()
        };
        result.Collections = (await grid.ReadAsync<SettlementCollectionDto>()).ToList();
        result.Advances = (await grid.ReadAsync<SettlementAdvanceDto>()).ToList();
        result.LoanInstallments = (await grid.ReadAsync<SettlementInstallmentDto>()).ToList();
        return result;
    }

    public async Task<List<PendingSettlementFarmerDto>> GetPendingFarmersAsync(PendingFarmersQuery query, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        return await conn.QueryListAsync<PendingSettlementFarmerDto>(
            "SELECT f.FarmerId, f.FarmerCode, f.FullName AS FarmerName, f.Mobile, f.CollectionCenterId, cc.Name AS CollectionCenterName, " +
            "CAST(COUNT(mc.MilkCollectionId) AS SIGNED) AS CollectionsCount, " +
            "SUM(mc.QuantityLiters) AS TotalQuantity, " +
            "SUM(mc.Amount - mc.BonusAmount + mc.PenaltyAmount) AS GrossAmount, " +
            "SUM(mc.BonusAmount) AS BonusAmount, SUM(mc.PenaltyAmount) AS PenaltyAmount, SUM(mc.Amount) AS TotalAmount, " +
            "MIN(mc.CollectionDate) AS FirstCollectionDate, MAX(mc.CollectionDate) AS LastCollectionDate, " +
            "(SELECT COALESCE(SUM(a.BalanceAmount), 0) FROM `FarmerAdvance` a " +
            " WHERE a.FarmerId = f.FarmerId AND a.Status = 'Open' AND a.IsActive = 1 AND a.BalanceAmount > 0) AS PendingAdvanceBalance, " +
            "(SELECT COALESCE(SUM(i.Amount - i.PaidAmount), 0) FROM `FarmerLoanInstallment` i " +
            " JOIN `FarmerLoan` l ON l.FarmerLoanId = i.FarmerLoanId " +
            " WHERE l.FarmerId = f.FarmerId AND l.Status = 'Active' AND l.IsActive = 1 AND i.IsActive = 1 " +
            " AND i.Status IN ('Pending', 'Partial') AND i.DueDate <= @DateTo) AS LoanInstallmentsDue " +
            "FROM `MilkCollection` mc " +
            "JOIN `Farmer` f ON f.FarmerId = mc.FarmerId " +
            "LEFT JOIN `CollectionCenter` cc ON cc.CollectionCenterId = f.CollectionCenterId " +
            "WHERE mc.Status = 'Approved' AND mc.SettlementStatus = 'Unsettled' AND mc.IsActive = 1 " +
            "AND mc.CollectionDate BETWEEN @DateFrom AND @DateTo " +
            "AND (@CenterId IS NULL OR mc.CollectionCenterId = @CenterId) " +
            "AND (@Search IS NULL OR f.FullName LIKE @Search OR f.FarmerCode LIKE @Search OR f.Mobile LIKE @Search) " +
            "GROUP BY f.FarmerId, f.FarmerCode, f.FullName, f.Mobile, f.CollectionCenterId, cc.Name " +
            "ORDER BY f.FullName, f.FarmerId LIMIT 1000",
            new
            {
                DateFrom = query.From.Date,
                DateTo = query.To.Date,
                CenterId = query.CenterId,
                Search = LikePattern(query.Search)
            }, ct);
    }

    public async Task<SettlementCreatedDto> CreateAsync(CreateSettlementRequest request, long userId, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        var row = await conn.QuerySpFirstOrDefaultAsync<SettlementCreatedDto>("sp_CreateFarmerPayment", new
        {
            p_FarmerId = request.FarmerId,
            p_FromDate = request.From.Date,
            p_ToDate = request.To.Date,
            p_BonusAmount = request.BonusAmount,
            p_PenaltyAmount = request.PenaltyAmount,
            p_AdvanceDeduction = request.AdvanceDeduction,
            p_IncludeLoanInstallments = request.IncludeLoanInstallments ? 1 : 0,
            p_OtherDeduction = request.OtherDeduction,
            p_OtherDeductionRemarks = request.OtherDeductionRemarks,
            p_PaymentModeId = request.PaymentModeId,
            p_ReferenceNo = request.ReferenceNo,
            p_Remarks = request.Remarks,
            p_UserId = userId
        }, ct);
        return row ?? throw new InvalidOperationException("sp_CreateFarmerPayment did not return a row.");
    }

    public async Task<SettlementApprovedDto> ApproveAsync(long id, long userId, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        var row = await conn.QuerySpFirstOrDefaultAsync<SettlementApprovedDto>("sp_ApproveFarmerPayment",
            new { p_FarmerPaymentId = id, p_UserId = userId }, ct);
        return row ?? throw new InvalidOperationException("sp_ApproveFarmerPayment did not return a row.");
    }

    public async Task<SettlementPaidDto> PayAsync(long id, PaySettlementRequest request, long userId, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        var row = await conn.QuerySpFirstOrDefaultAsync<SettlementPaidDto>("sp_PayFarmerPayment", new
        {
            p_FarmerPaymentId = id,
            p_PaymentModeId = request.PaymentModeId,
            p_ReferenceNo = request.ReferenceNo,
            p_PaidAmount = request.PaidAmount,
            p_UserId = userId
        }, ct);
        return row ?? throw new InvalidOperationException("sp_PayFarmerPayment did not return a row.");
    }

    public async Task<SettlementCancelledDto> CancelAsync(long id, string reason, long userId, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        var row = await conn.QuerySpFirstOrDefaultAsync<SettlementCancelledDto>("sp_CancelFarmerPayment",
            new { p_FarmerPaymentId = id, p_UserId = userId, p_Reason = reason }, ct);
        return row ?? throw new InvalidOperationException("sp_CancelFarmerPayment did not return a row.");
    }

    public async Task<long?> GetPaymentFarmerIdAsync(long id, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        return await conn.ScalarAsync<long?>(
            "SELECT FarmerId FROM `FarmerPayment` WHERE FarmerPaymentId = @Id AND IsActive = 1", new { Id = id }, ct);
    }

    public async Task<PagedResult<FarmerPaymentListDto>> GetPaymentsAsync(FarmerPaymentQuery query, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        var param = new
        {
            FarmerId = query.FarmerId,
            CenterId = query.CenterId,
            Status = string.IsNullOrWhiteSpace(query.Status) ? null : query.Status.Trim(),
            DateFrom = query.From?.Date,
            DateTo = query.To?.Date,
            Search = query.SearchPattern
        };
        return await conn.QueryPagedAsync<FarmerPaymentListDto>(
            "SELECT " + PaymentColumns + PaymentFromJoins + PaymentWhere,
            "SELECT COUNT(*)" + PaymentFromJoins + PaymentWhere,
            param, query, SortColumns, "fp.PaymentDate DESC, fp.FarmerPaymentId DESC", ct);
    }

    public async Task<FarmerPaymentDto?> GetPaymentByIdAsync(long id, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        var dto = await conn.QueryFirstOrNullAsync<FarmerPaymentDto>(
            "SELECT " + PaymentColumns + ", fba.BankName, " +
            "CONCAT(REPEAT('X', GREATEST(CHAR_LENGTH(fba.AccountNumber) - 4, 0)), RIGHT(fba.AccountNumber, 4)) AS BankAccountMasked, " +
            "fba.IfscCode, fba.UpiId, fp.ApprovedBy, ua.DisplayName AS ApprovedByName, fp.PaidBy, up.DisplayName AS PaidByName, " +
            "fp.CancelledDate, fp.CancelReason, fp.Remarks" +
            PaymentFromJoins +
            " LEFT JOIN `FarmerBankAccount` fba ON fba.FarmerBankAccountId = fp.FarmerBankAccountId" +
            " LEFT JOIN `User` ua ON ua.UserId = fp.ApprovedBy" +
            " LEFT JOIN `User` up ON up.UserId = fp.PaidBy" +
            " WHERE fp.FarmerPaymentId = @Id AND fp.IsActive = 1",
            new { Id = id }, ct);
        if (dto is null) return null;

        dto.Details = await conn.QueryListAsync<FarmerPaymentLineDto>(
            "SELECT d.FarmerPaymentDetailId AS Id, d.MilkCollectionId, mc.CollectionNumber, d.CollectionDate, s.ShiftName, " +
            "d.Quantity, d.Rate, d.BonusAmount, d.PenaltyAmount, d.Amount " +
            "FROM `FarmerPaymentDetail` d " +
            "JOIN `MilkCollection` mc ON mc.MilkCollectionId = d.MilkCollectionId " +
            "JOIN `CollectionShift` s ON s.CollectionShiftId = mc.ShiftId " +
            "WHERE d.FarmerPaymentId = @Id AND d.IsActive = 1 ORDER BY d.CollectionDate, d.FarmerPaymentDetailId",
            new { Id = id }, ct);

        dto.Deductions = await conn.QueryListAsync<FarmerPaymentDeductionDto>(
            "SELECT d.FarmerPaymentDeductionId AS Id, d.DeductionType, d.FarmerAdvanceId, a.AdvanceNumber, d.FarmerLoanId, l.LoanNumber, " +
            "d.FarmerLoanInstallmentId, i.InstallmentNo, d.Amount, d.Remarks " +
            "FROM `FarmerPaymentDeduction` d " +
            "LEFT JOIN `FarmerAdvance` a ON a.FarmerAdvanceId = d.FarmerAdvanceId " +
            "LEFT JOIN `FarmerLoan` l ON l.FarmerLoanId = d.FarmerLoanId " +
            "LEFT JOIN `FarmerLoanInstallment` i ON i.FarmerLoanInstallmentId = d.FarmerLoanInstallmentId " +
            "WHERE d.FarmerPaymentId = @Id AND (d.IsActive = 1 OR @Cancelled = 1) ORDER BY d.FarmerPaymentDeductionId",
            new { Id = id, Cancelled = dto.Status == "Cancelled" ? 1 : 0 }, ct);

        return dto;
    }

    private static string? LikePattern(string? search)
    {
        var s = search?.Trim();
        if (string.IsNullOrEmpty(s)) return null;
        s = s.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_");
        return "%" + s + "%";
    }
}
