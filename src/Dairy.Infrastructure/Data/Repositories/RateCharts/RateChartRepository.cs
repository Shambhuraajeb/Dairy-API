using Dairy.Application.Common;
using Dairy.Application.DTOs.Collection;
using Dairy.Application.DTOs.RateCharts;
using Dairy.Application.Interfaces;
using Dairy.Application.Interfaces.RateCharts;
using Dairy.Application.Requests.RateCharts;
using Dapper;

namespace Dairy.Infrastructure.Data.Repositories.RateCharts;

/// <summary>Row read while a chart is locked during activation.</summary>
public class RateChartLockRow
{
    public string Status { get; set; } = string.Empty;
    public string MilkType { get; set; } = string.Empty;
    public long? CollectionCenterId { get; set; }
    public DateTime EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
}

public class RateChartRepository : IRateChartRepository
{
    private static readonly Dictionary<string, string> SortColumns = new(StringComparer.OrdinalIgnoreCase)
    {
        ["id"] = "rc.RateChartId",
        ["name"] = "rc.Name",
        ["milkType"] = "rc.MilkType",
        ["collectionCenterName"] = "cc.Name",
        ["effectiveFrom"] = "rc.EffectiveFrom",
        ["effectiveTo"] = "rc.EffectiveTo",
        ["status"] = "rc.Status",
        ["createdDate"] = "rc.CreatedDate"
    };

    private const string ListColumns =
        "rc.RateChartId AS Id, rc.Name, rc.MilkType, rc.CollectionCenterId, cc.Name AS CollectionCenterName, " +
        "rc.EffectiveFrom, rc.EffectiveTo, rc.Status, rc.CalculationMethod, rc.BaseRatePerLiter, rc.BaseFat, rc.BaseSnf, " +
        "rc.FatRatePerPoint, rc.SnfRatePerPoint, rc.MinRatePerLiter, rc.MaxRatePerLiter, " +
        "(SELECT COUNT(*) FROM `RateChartDetail` d WHERE d.RateChartId = rc.RateChartId AND d.IsActive = 1) AS DetailCount, " +
        "rc.ActivatedDate, rc.IsActive, rc.CreatedDate";

    private const string FromJoins =
        " FROM `RateChart` rc LEFT JOIN `CollectionCenter` cc ON cc.CollectionCenterId = rc.CollectionCenterId";

    private const string Where =
        " WHERE (@Search IS NULL OR rc.Name LIKE @Search)" +
        " AND (@MilkType IS NULL OR rc.MilkType = @MilkType)" +
        " AND (@Status IS NULL OR rc.Status = @Status)" +
        " AND (@CenterId IS NULL OR rc.CollectionCenterId IS NULL OR rc.CollectionCenterId = @CenterId)" +
        " AND (@IsActive IS NULL OR rc.IsActive = @IsActive)";

    private const string InsertHeader =
        "INSERT INTO `RateChart` (Name, MilkType, CollectionCenterId, EffectiveFrom, EffectiveTo, Status, CalculationMethod, " +
        "BaseRatePerLiter, BaseFat, BaseSnf, FatRatePerPoint, SnfRatePerPoint, MinRatePerLiter, MaxRatePerLiter, BonusMinFat, " +
        "BonusMinSnf, QualityBonusPerLiter, PenaltyBelowFat, PenaltyBelowSnf, QualityPenaltyPerLiter, Description, CreatedBy) " +
        "VALUES (@Name, @MilkType, @CollectionCenterId, @EffectiveFrom, @EffectiveTo, 'Draft', @CalculationMethod, " +
        "@BaseRatePerLiter, @BaseFat, @BaseSnf, @FatRatePerPoint, @SnfRatePerPoint, @MinRatePerLiter, @MaxRatePerLiter, @BonusMinFat, " +
        "@BonusMinSnf, @QualityBonusPerLiter, @PenaltyBelowFat, @PenaltyBelowSnf, @QualityPenaltyPerLiter, @Description, @UserId); " +
        "SELECT LAST_INSERT_ID();";

    private const string UpdateHeader =
        "UPDATE `RateChart` SET Name = @Name, MilkType = @MilkType, CollectionCenterId = @CollectionCenterId, " +
        "EffectiveFrom = @EffectiveFrom, EffectiveTo = @EffectiveTo, CalculationMethod = @CalculationMethod, " +
        "BaseRatePerLiter = @BaseRatePerLiter, BaseFat = @BaseFat, BaseSnf = @BaseSnf, FatRatePerPoint = @FatRatePerPoint, " +
        "SnfRatePerPoint = @SnfRatePerPoint, MinRatePerLiter = @MinRatePerLiter, MaxRatePerLiter = @MaxRatePerLiter, " +
        "BonusMinFat = @BonusMinFat, BonusMinSnf = @BonusMinSnf, QualityBonusPerLiter = @QualityBonusPerLiter, " +
        "PenaltyBelowFat = @PenaltyBelowFat, PenaltyBelowSnf = @PenaltyBelowSnf, QualityPenaltyPerLiter = @QualityPenaltyPerLiter, " +
        "Description = @Description, ModifiedBy = @UserId WHERE RateChartId = @Id";

    private const string InsertDetail =
        "INSERT INTO `RateChartDetail` (RateChartId, FatFrom, FatTo, SnfFrom, SnfTo, Rate, BonusPerLiter, PenaltyPerLiter, CreatedBy) " +
        "VALUES (@RateChartId, @FatFrom, @FatTo, @SnfFrom, @SnfTo, @Rate, @BonusPerLiter, @PenaltyPerLiter, @UserId)";

    private readonly IDbConnectionFactory _db;

    public RateChartRepository(IDbConnectionFactory db) => _db = db;

    public async Task<PagedResult<RateChartListDto>> GetPagedAsync(RateChartQuery query, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        var param = new
        {
            Search = query.SearchPattern,
            MilkType = string.IsNullOrWhiteSpace(query.MilkType) ? null : query.MilkType.Trim(),
            Status = string.IsNullOrWhiteSpace(query.Status) ? null : query.Status.Trim(),
            CenterId = query.CenterId,
            query.IsActive
        };
        return await conn.QueryPagedAsync<RateChartListDto>(
            "SELECT " + ListColumns + FromJoins + Where,
            "SELECT COUNT(*)" + FromJoins + Where,
            param, query, SortColumns, "rc.EffectiveFrom DESC, rc.RateChartId DESC", ct);
    }

    public async Task<RateChartDto?> GetByIdAsync(long id, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        var dto = await conn.QueryFirstOrNullAsync<RateChartDto>(
            "SELECT " + ListColumns + ", rc.BonusMinFat, rc.BonusMinSnf, rc.QualityBonusPerLiter, rc.PenaltyBelowFat, " +
            "rc.PenaltyBelowSnf, rc.QualityPenaltyPerLiter, rc.Description, rc.ActivatedBy" +
            FromJoins + " WHERE rc.RateChartId = @Id",
            new { Id = id }, ct);
        if (dto is null) return null;

        dto.Details = await conn.QueryListAsync<RateChartDetailDto>(
            "SELECT d.RateChartDetailId AS Id, d.FatFrom, d.FatTo, d.SnfFrom, d.SnfTo, d.Rate, d.BonusPerLiter, d.PenaltyPerLiter " +
            "FROM `RateChartDetail` d WHERE d.RateChartId = @Id AND d.IsActive = 1 ORDER BY d.FatFrom, d.SnfFrom, d.RateChartDetailId",
            new { Id = id }, ct);
        return dto;
    }

    public async Task<bool> CenterExistsAsync(long centerId, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        var count = await conn.ScalarAsync<int>(
            "SELECT COUNT(1) FROM `CollectionCenter` WHERE CollectionCenterId = @Id AND IsActive = 1", new { Id = centerId }, ct);
        return count > 0;
    }

    public async Task<long> CreateAsync(SaveRateChartRequest request, long userId, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        await using var tx = await conn.BeginTransactionAsync(ct);

        var id = await conn.ExecuteScalarAsync<long>(new CommandDefinition(InsertHeader, HeaderParams(request, userId, 0), tx, cancellationToken: ct));
        await InsertDetailsAsync(conn, tx, id, request, userId, ct);

        await tx.CommitAsync(ct);
        return id;
    }

    public async Task<bool> UpdateDraftAsync(long id, SaveRateChartRequest request, long userId, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        await using var tx = await conn.BeginTransactionAsync(ct);

        var status = await conn.ExecuteScalarAsync<string?>(new CommandDefinition(
            "SELECT Status FROM `RateChart` WHERE RateChartId = @Id AND IsActive = 1 FOR UPDATE", new { Id = id }, tx, cancellationToken: ct));
        if (status != "Draft") return false;

        await conn.ExecuteAsync(new CommandDefinition(UpdateHeader, HeaderParams(request, userId, id), tx, cancellationToken: ct));
        await conn.ExecuteAsync(new CommandDefinition(
            "DELETE FROM `RateChartDetail` WHERE RateChartId = @Id", new { Id = id }, tx, cancellationToken: ct));
        await InsertDetailsAsync(conn, tx, id, request, userId, ct);

        await tx.CommitAsync(ct);
        return true;
    }

    public async Task<List<long>> ActivateAsync(long id, long userId, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        await using var tx = await conn.BeginTransactionAsync(ct);

        var chart = await conn.QueryFirstOrDefaultAsync<RateChartLockRow>(new CommandDefinition(
            "SELECT Status, MilkType, CollectionCenterId, EffectiveFrom, EffectiveTo FROM `RateChart` " +
            "WHERE RateChartId = @Id AND IsActive = 1 FOR UPDATE",
            new { Id = id }, tx, cancellationToken: ct)) ?? throw new NotFoundException("Rate chart", id);
        if (chart.Status != "Draft")
            throw new BusinessRuleException("Only draft rate charts can be activated.");

        // Active charts of the same center scope + milk type whose period intersects the new chart's period.
        var overlapping = (await conn.QueryAsync<long>(new CommandDefinition(
            "SELECT RateChartId FROM `RateChart` WHERE RateChartId <> @Id AND IsActive = 1 AND Status = 'Active' " +
            "AND MilkType = @MilkType AND (CollectionCenterId <=> @CenterId) " +
            "AND (EffectiveTo IS NULL OR EffectiveTo >= @DateFrom) " +
            "AND (@DateTo IS NULL OR EffectiveFrom <= @DateTo) FOR UPDATE",
            new { Id = id, chart.MilkType, CenterId = chart.CollectionCenterId, DateFrom = chart.EffectiveFrom, DateTo = chart.EffectiveTo },
            tx, cancellationToken: ct))).ToList();

        if (overlapping.Count > 0)
        {
            // Charts starting before the new one are cut off the day before it starts (they stay Active so past collections can
            // still be re-rated on adjustment); charts starting on/after it are fully superseded and expire.
            await conn.ExecuteAsync(new CommandDefinition(
                "UPDATE `RateChart` SET " +
                "Status = CASE WHEN EffectiveFrom >= @DateFrom THEN 'Expired' ELSE Status END, " +
                "EffectiveTo = CASE WHEN EffectiveFrom < @DateFrom THEN DATE_SUB(@DateFrom, INTERVAL 1 DAY) ELSE EffectiveTo END, " +
                "ModifiedBy = @UserId WHERE RateChartId IN @Ids",
                new { DateFrom = chart.EffectiveFrom, UserId = userId, Ids = overlapping }, tx, cancellationToken: ct));
        }

        await conn.ExecuteAsync(new CommandDefinition(
            "UPDATE `RateChart` SET Status = 'Active', ActivatedBy = @UserId, ActivatedDate = NOW(), ModifiedBy = @UserId " +
            "WHERE RateChartId = @Id",
            new { Id = id, UserId = userId }, tx, cancellationToken: ct));

        await tx.CommitAsync(ct);
        return overlapping;
    }

    public async Task DeleteDraftAsync(long id, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        await conn.ExecAsync("DELETE FROM `RateChart` WHERE RateChartId = @Id AND Status = 'Draft'", new { Id = id }, ct);
    }

    public async Task DeactivateAsync(long id, long userId, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        await conn.ExecAsync(
            "UPDATE `RateChart` SET IsActive = 0, Status = CASE WHEN Status = 'Active' THEN 'Expired' ELSE Status END, " +
            "ModifiedBy = @UserId WHERE RateChartId = @Id",
            new { Id = id, UserId = userId }, ct);
    }

    public async Task<RatePreviewDto> PreviewRateAsync(long centerId, DateTime date, string milkType, decimal fat, decimal snf,
        decimal? clr, decimal? quantity, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        var row = await conn.QuerySpFirstOrDefaultAsync<RatePreviewDto>("sp_PreviewMilkRate", new
        {
            p_CollectionCenterId = centerId,
            p_CollectionDate = date.Date,
            p_MilkType = milkType,
            p_Fat = fat,
            p_Snf = snf,
            p_Clr = clr,
            p_QuantityLiters = quantity
        }, ct);
        return row ?? throw new InvalidOperationException("sp_PreviewMilkRate did not return a row.");
    }

    private static object HeaderParams(SaveRateChartRequest r, long userId, long id) => new
    {
        Id = id,
        r.Name,
        r.MilkType,
        r.CollectionCenterId,
        r.EffectiveFrom,
        r.EffectiveTo,
        r.CalculationMethod,
        r.BaseRatePerLiter,
        r.BaseFat,
        r.BaseSnf,
        r.FatRatePerPoint,
        r.SnfRatePerPoint,
        r.MinRatePerLiter,
        r.MaxRatePerLiter,
        r.BonusMinFat,
        r.BonusMinSnf,
        r.QualityBonusPerLiter,
        r.PenaltyBelowFat,
        r.PenaltyBelowSnf,
        r.QualityPenaltyPerLiter,
        r.Description,
        UserId = userId
    };

    private static async Task InsertDetailsAsync(System.Data.Common.DbConnection conn, System.Data.Common.DbTransaction tx,
        long chartId, SaveRateChartRequest request, long userId, CancellationToken ct)
    {
        if (request.Details.Count == 0) return;
        var rows = request.Details.Select(d => new
        {
            RateChartId = chartId,
            d.FatFrom,
            d.FatTo,
            d.SnfFrom,
            d.SnfTo,
            d.Rate,
            d.BonusPerLiter,
            d.PenaltyPerLiter,
            UserId = userId
        }).ToList();
        await conn.ExecuteAsync(new CommandDefinition(InsertDetail, rows, tx, cancellationToken: ct));
    }
}
