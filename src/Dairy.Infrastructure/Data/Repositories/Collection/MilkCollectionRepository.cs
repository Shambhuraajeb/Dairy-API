using Dairy.Application.Common;
using Dairy.Application.DTOs.Collection;
using Dairy.Application.Interfaces;
using Dairy.Application.Interfaces.Collection;
using Dairy.Application.Requests.Collection;
using Dapper;

namespace Dairy.Infrastructure.Data.Repositories.Collection;

public class MilkCollectionRepository : IMilkCollectionRepository
{
    private static readonly Dictionary<string, string> SortColumns = new(StringComparer.OrdinalIgnoreCase)
    {
        ["id"] = "mc.MilkCollectionId",
        ["collectionNumber"] = "mc.CollectionNumber",
        ["collectionDate"] = "mc.CollectionDate",
        ["shiftName"] = "s.ShiftName",
        ["collectionCenterName"] = "cc.Name",
        ["farmerCode"] = "f.FarmerCode",
        ["farmerName"] = "f.FullName",
        ["milkType"] = "mc.MilkType",
        ["quantityLiters"] = "mc.QuantityLiters",
        ["fat"] = "mc.Fat",
        ["snf"] = "mc.Snf",
        ["amount"] = "mc.Amount",
        ["status"] = "mc.Status",
        ["settlementStatus"] = "mc.SettlementStatus",
        ["createdDate"] = "mc.CreatedDate"
    };

    private const string ListColumns =
        "mc.MilkCollectionId AS Id, mc.CollectionNumber, mc.CollectionDate, mc.ShiftId, s.ShiftName, " +
        "mc.CollectionCenterId, cc.Name AS CollectionCenterName, mc.FarmerId, f.FarmerCode, f.FullName AS FarmerName, " +
        "mc.MilkType, mc.QuantityLiters, mc.Fat, mc.Snf, mc.Clr, mc.Rate, " +
        "COALESCE(ROUND(mc.Amount / NULLIF(mc.QuantityLiters, 0), 4), 0) AS FinalRate, " +
        "mc.BonusAmount, mc.PenaltyAmount, mc.Amount, mc.QualityResult, mc.Status, mc.SettlementStatus, " +
        "mc.AdjustedFlag, mc.FarmerPaymentId, mc.CreatedDate";

    private const string FromJoins =
        " FROM `MilkCollection` mc" +
        " JOIN `CollectionShift` s ON s.CollectionShiftId = mc.ShiftId" +
        " JOIN `CollectionCenter` cc ON cc.CollectionCenterId = mc.CollectionCenterId" +
        " JOIN `Farmer` f ON f.FarmerId = mc.FarmerId";

    private const string Where =
        " WHERE mc.IsActive = 1" +
        " AND (@DateFrom IS NULL OR mc.CollectionDate >= @DateFrom)" +
        " AND (@DateTo IS NULL OR mc.CollectionDate <= @DateTo)" +
        " AND (@CenterId IS NULL OR mc.CollectionCenterId = @CenterId)" +
        " AND (@ShiftId IS NULL OR mc.ShiftId = @ShiftId)" +
        " AND (@FarmerId IS NULL OR mc.FarmerId = @FarmerId)" +
        " AND (@Status IS NULL OR mc.Status = @Status)" +
        " AND (@SettlementStatus IS NULL OR mc.SettlementStatus = @SettlementStatus)" +
        " AND (@Search IS NULL OR mc.CollectionNumber LIKE @Search OR f.FullName LIKE @Search OR f.FarmerCode LIKE @Search)";

    private const string QualitySelect =
        "SELECT q.MilkQualityTestId AS Id, q.MilkCollectionId, q.Fat, q.Snf, q.Clr, q.Protein, q.Lactose, q.Temperature, q.Acidity, " +
        "q.WaterAdded, q.StarchDetected, q.UreaDetected, q.NeutralizerDetected, q.AntibioticDetected, q.OtherAdulteration, " +
        "q.Result, q.FailureReason, q.TestedBy, u.DisplayName AS TestedByName, q.TestDate, q.Remarks " +
        "FROM `MilkQualityTest` q LEFT JOIN `User` u ON u.UserId = q.TestedBy ";

    private readonly IDbConnectionFactory _db;

    public MilkCollectionRepository(IDbConnectionFactory db) => _db = db;

    public async Task<PagedResult<MilkCollectionListDto>> GetPagedAsync(MilkCollectionQuery query, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        var param = new
        {
            DateFrom = query.From?.Date,
            DateTo = query.To?.Date,
            CenterId = query.CenterId,
            ShiftId = query.ShiftId,
            FarmerId = query.FarmerId,
            Status = string.IsNullOrWhiteSpace(query.Status) ? null : query.Status.Trim(),
            SettlementStatus = string.IsNullOrWhiteSpace(query.SettlementStatus) ? null : query.SettlementStatus.Trim(),
            Search = query.SearchPattern
        };
        return await conn.QueryPagedAsync<MilkCollectionListDto>(
            "SELECT " + ListColumns + FromJoins + Where,
            "SELECT COUNT(*)" + FromJoins + Where,
            param, query, SortColumns, "mc.CollectionDate DESC, mc.MilkCollectionId DESC", ct);
    }

    public async Task<MilkCollectionDetailDto?> GetByIdAsync(long id, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);

        var dto = await conn.QueryFirstOrNullAsync<MilkCollectionDetailDto>(
            "SELECT " + ListColumns + ", " +
            "mc.Temperature, mc.CanCount, mc.RateChartId, rc.Name AS RateChartName, mc.TankId, t.Name AS TankName, " +
            "mc.OriginalQuantity, mc.OriginalFat, mc.OriginalSnf, mc.OriginalClr, mc.OriginalRate, mc.OriginalAmount, " +
            "mc.AdjustmentReason, mc.AdjustedBy, ua.DisplayName AS AdjustedByName, mc.AdjustedDate, " +
            "mc.ApprovedBy, uap.DisplayName AS ApprovedByName, mc.ApprovedDate, " +
            "mc.RejectedBy, ure.DisplayName AS RejectedByName, mc.RejectedDate, mc.RejectionReason, " +
            "mc.CollectedBy, uco.DisplayName AS CollectedByName, mc.Remarks" +
            FromJoins +
            " LEFT JOIN `RateChart` rc ON rc.RateChartId = mc.RateChartId" +
            " LEFT JOIN `MilkTank` t ON t.MilkTankId = mc.TankId" +
            " LEFT JOIN `User` ua ON ua.UserId = mc.AdjustedBy" +
            " LEFT JOIN `User` uap ON uap.UserId = mc.ApprovedBy" +
            " LEFT JOIN `User` ure ON ure.UserId = mc.RejectedBy" +
            " LEFT JOIN `User` uco ON uco.UserId = mc.CollectedBy" +
            " WHERE mc.MilkCollectionId = @Id AND mc.IsActive = 1",
            new { Id = id }, ct);
        if (dto is null) return null;

        dto.QualityTests = await conn.QueryListAsync<MilkQualityTestDto>(
            QualitySelect + "WHERE q.MilkCollectionId = @Id AND q.IsActive = 1 ORDER BY q.TestDate DESC, q.MilkQualityTestId DESC",
            new { Id = id }, ct);

        dto.RateBreakdown = await conn.QueryFirstOrNullAsync<MilkRateBreakdownDto>(
            "SELECT r.RateChartId, rc.Name AS RateChartName, rc.CalculationMethod, r.RateChartDetailId, " +
            "d.FatFrom AS SlabFatFrom, d.FatTo AS SlabFatTo, d.SnfFrom AS SlabSnfFrom, d.SnfTo AS SlabSnfTo, " +
            "r.FatUsed, r.SnfUsed, r.QuantityUsed, r.BaseRate, r.Bonus, r.Penalty, r.FinalRate, r.BonusAmount, r.PenaltyAmount, r.CalculatedDate " +
            "FROM `MilkCollectionRate` r " +
            "JOIN `RateChart` rc ON rc.RateChartId = r.RateChartId " +
            "LEFT JOIN `RateChartDetail` d ON d.RateChartDetailId = r.RateChartDetailId " +
            "WHERE r.MilkCollectionId = @Id",
            new { Id = id }, ct);

        dto.Rejections = await conn.QueryListAsync<MilkRejectionDto>(
            "SELECT r.MilkRejectionId AS Id, r.MilkCollectionId, mc.CollectionNumber, mc.CollectionDate, r.FarmerId, f.FarmerCode, " +
            "f.FullName AS FarmerName, r.CollectionCenterId, cc.Name AS CollectionCenterName, r.RejectionDate, r.ReasonCode, r.Reason, " +
            "r.QuantityLiters, r.DisposalMethod, r.RejectedBy, u.DisplayName AS RejectedByName, r.RejectedDate, r.Remarks " +
            "FROM `MilkRejection` r " +
            "JOIN `MilkCollection` mc ON mc.MilkCollectionId = r.MilkCollectionId " +
            "JOIN `Farmer` f ON f.FarmerId = r.FarmerId " +
            "LEFT JOIN `CollectionCenter` cc ON cc.CollectionCenterId = r.CollectionCenterId " +
            "LEFT JOIN `User` u ON u.UserId = r.RejectedBy " +
            "WHERE r.MilkCollectionId = @Id AND r.IsActive = 1 ORDER BY r.RejectedDate DESC, r.MilkRejectionId DESC",
            new { Id = id }, ct);

        return dto;
    }

    public async Task<CollectionStateDto?> GetStateAsync(long id, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        return await conn.QueryFirstOrNullAsync<CollectionStateDto>(
            "SELECT mc.MilkCollectionId AS Id, mc.FarmerId, mc.Status, mc.SettlementStatus, mc.Fat, mc.Snf " +
            "FROM `MilkCollection` mc WHERE mc.MilkCollectionId = @Id AND mc.IsActive = 1",
            new { Id = id }, ct);
    }

    public async Task<MilkCollectionCreatedDto> CreateAsync(CreateMilkCollectionRequest request, long userId, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        var row = await conn.QuerySpFirstOrDefaultAsync<MilkCollectionCreatedDto>("sp_CreateMilkCollection", new
        {
            p_CollectionDate = (request.CollectionDate ?? DateTime.Today).Date,
            p_ShiftId = request.ShiftId,
            p_CollectionCenterId = request.CollectionCenterId,
            p_FarmerId = request.FarmerId,
            p_MilkType = request.MilkType,
            p_QuantityLiters = request.QuantityLiters,
            p_Fat = request.Fat,
            p_Snf = request.Snf,
            p_Clr = request.Clr,
            p_Temperature = request.Temperature,
            p_CanCount = request.CanCount,
            p_MilkTankId = request.MilkTankId,
            p_Acidity = request.Acidity,
            p_WaterAdded = request.WaterAdded ? 1 : 0,
            p_Remarks = request.Remarks,
            p_UserId = userId
        }, ct);
        return row ?? throw new InvalidOperationException("sp_CreateMilkCollection did not return a row.");
    }

    public async Task AdjustAsync(long id, AdjustMilkCollectionRequest request, long userId, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        await conn.ExecuteSpAsync("sp_AdjustMilkCollection", new
        {
            p_MilkCollectionId = id,
            p_QuantityLiters = request.QuantityLiters,
            p_Fat = request.Fat,
            p_Snf = request.Snf,
            p_Clr = request.Clr,
            p_Reason = request.Reason,
            p_UserId = userId
        }, ct);
    }

    public async Task<MilkCollectionApprovedDto> ApproveAsync(long id, long userId, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        var row = await conn.QuerySpFirstOrDefaultAsync<MilkCollectionApprovedDto>("sp_ApproveMilkCollection",
            new { p_MilkCollectionId = id, p_UserId = userId }, ct);
        return row ?? throw new InvalidOperationException("sp_ApproveMilkCollection did not return a row.");
    }

    public async Task<MilkCollectionRejectedDto> RejectAsync(long id, string reason, decimal? quantityLiters, long userId, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        var row = await conn.QuerySpFirstOrDefaultAsync<MilkCollectionRejectedDto>("sp_RejectMilkCollection", new
        {
            p_MilkCollectionId = id,
            p_UserId = userId,
            p_Reason = reason,
            p_QuantityLiters = quantityLiters
        }, ct);
        return row ?? throw new InvalidOperationException("sp_RejectMilkCollection did not return a row.");
    }

    public async Task<List<MilkQualityTestDto>> GetQualityTestsAsync(long id, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        return await conn.QueryListAsync<MilkQualityTestDto>(
            QualitySelect + "WHERE q.MilkCollectionId = @Id AND q.IsActive = 1 ORDER BY q.TestDate DESC, q.MilkQualityTestId DESC",
            new { Id = id }, ct);
    }

    public async Task<QualityRecordResult> RecordQualityAsync(long id, RecordQualityRequest request, decimal fat, decimal snf,
        string result, string? failureReason, long userId, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        await using var tx = await conn.BeginTransactionAsync(ct);

        // serialize concurrent quality entries for the same collection
        await conn.ExecuteAsync(new CommandDefinition(
            "SELECT 1 FROM `MilkCollection` WHERE MilkCollectionId = @Id FOR UPDATE", new { Id = id }, tx, cancellationToken: ct));

        var previous = await conn.QueryFirstOrDefaultAsync<MilkQualityTestDto>(new CommandDefinition(
            QualitySelect + "WHERE q.MilkCollectionId = @Id AND q.IsActive = 1 ORDER BY q.TestDate DESC, q.MilkQualityTestId DESC LIMIT 1",
            new { Id = id }, tx, cancellationToken: ct));

        var p = new
        {
            Id = id,
            TestId = previous?.Id ?? 0L,
            Fat = fat,
            Snf = snf,
            request.Clr,
            request.Protein,
            request.Lactose,
            request.Temperature,
            request.Acidity,
            WaterAdded = request.WaterAdded ? 1 : 0,
            StarchDetected = request.StarchDetected ? 1 : 0,
            UreaDetected = request.UreaDetected ? 1 : 0,
            NeutralizerDetected = request.NeutralizerDetected ? 1 : 0,
            AntibioticDetected = request.AntibioticDetected ? 1 : 0,
            OtherAdulteration = string.IsNullOrWhiteSpace(request.OtherAdulteration) ? null : request.OtherAdulteration.Trim(),
            Result = result,
            FailureReason = failureReason,
            Remarks = string.IsNullOrWhiteSpace(request.Remarks) ? null : request.Remarks.Trim(),
            UserId = userId
        };

        long testId;
        if (previous is null)
        {
            testId = await conn.ExecuteScalarAsync<long>(new CommandDefinition(
                "INSERT INTO `MilkQualityTest` (MilkCollectionId, Fat, Snf, Clr, Protein, Lactose, Temperature, Acidity, WaterAdded, " +
                "StarchDetected, UreaDetected, NeutralizerDetected, AntibioticDetected, OtherAdulteration, Result, FailureReason, " +
                "TestedBy, TestDate, Remarks, CreatedBy) VALUES (@Id, @Fat, @Snf, @Clr, @Protein, @Lactose, @Temperature, @Acidity, " +
                "@WaterAdded, @StarchDetected, @UreaDetected, @NeutralizerDetected, @AntibioticDetected, @OtherAdulteration, @Result, " +
                "@FailureReason, @UserId, NOW(), @Remarks, @UserId); SELECT LAST_INSERT_ID();",
                p, tx, cancellationToken: ct));
        }
        else
        {
            testId = previous.Id;
            await conn.ExecuteAsync(new CommandDefinition(
                "UPDATE `MilkQualityTest` SET Fat = @Fat, Snf = @Snf, Clr = @Clr, Protein = @Protein, Lactose = @Lactose, " +
                "Temperature = @Temperature, Acidity = @Acidity, WaterAdded = @WaterAdded, StarchDetected = @StarchDetected, " +
                "UreaDetected = @UreaDetected, NeutralizerDetected = @NeutralizerDetected, AntibioticDetected = @AntibioticDetected, " +
                "OtherAdulteration = @OtherAdulteration, Result = @Result, FailureReason = @FailureReason, TestedBy = @UserId, " +
                "TestDate = NOW(), Remarks = @Remarks, ModifiedBy = @UserId WHERE MilkQualityTestId = @TestId",
                p, tx, cancellationToken: ct));
        }

        await conn.ExecuteAsync(new CommandDefinition(
            "UPDATE `MilkCollection` SET QualityResult = @Result, ModifiedBy = @UserId WHERE MilkCollectionId = @Id",
            new { Id = id, Result = result, UserId = userId }, tx, cancellationToken: ct));

        await tx.CommitAsync(ct);

        var current = await conn.QueryFirstOrDefaultAsync<MilkQualityTestDto>(new CommandDefinition(
            QualitySelect + "WHERE q.MilkQualityTestId = @TestId", new { TestId = testId }, cancellationToken: ct));

        return new QualityRecordResult
        {
            Previous = previous,
            Current = current ?? throw new InvalidOperationException("Quality test was not saved.")
        };
    }
}
