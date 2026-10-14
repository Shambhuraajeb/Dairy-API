using Dairy.Application.Common;
using Dairy.Application.DTOs.Collection;
using Dairy.Application.Interfaces;
using Dairy.Application.Interfaces.Collection;
using Dairy.Application.Requests.Collection;
using Dapper;

namespace Dairy.Infrastructure.Data.Repositories.Collection;

/// <summary>Row read while a tank is locked (FOR UPDATE).</summary>
public class MilkTankBalanceRow
{
    public decimal CurrentQuantity { get; set; }
    public decimal CapacityLiters { get; set; }
}

public class MilkTankRepository : IMilkTankRepository
{
    private static readonly Dictionary<string, string> SortColumns = new(StringComparer.OrdinalIgnoreCase)
    {
        ["id"] = "t.MilkTankId",
        ["code"] = "t.Code",
        ["name"] = "t.Name",
        ["collectionCenterName"] = "cc.Name",
        ["capacityLiters"] = "t.CapacityLiters",
        ["currentQuantity"] = "t.CurrentQuantity",
        ["status"] = "t.Status",
        ["createdDate"] = "t.CreatedDate"
    };

    private static readonly Dictionary<string, string> TxnSortColumns = new(StringComparer.OrdinalIgnoreCase)
    {
        ["id"] = "tt.TankTransactionId",
        ["transactionDate"] = "tt.TransactionDate",
        ["transactionType"] = "tt.TransactionType",
        ["quantity"] = "tt.Quantity",
        ["balanceAfter"] = "tt.BalanceAfter"
    };

    private const string Select =
        "SELECT t.MilkTankId AS Id, t.Code, t.Name, t.CollectionCenterId, cc.Name AS CollectionCenterName, " +
        "t.CapacityLiters, t.CurrentQuantity, GREATEST(t.CapacityLiters - t.CurrentQuantity, 0) AS AvailableCapacity, " +
        "COALESCE(ROUND(t.CurrentQuantity * 100 / NULLIF(t.CapacityLiters, 0), 2), 0) AS FillPercent, " +
        "t.MilkType, t.MinTemperature, t.MaxTemperature, t.CurrentTemperature, t.Status, t.LastCleanedDate, t.IsActive, t.CreatedDate";

    private const string FromJoins =
        " FROM `MilkTank` t JOIN `CollectionCenter` cc ON cc.CollectionCenterId = t.CollectionCenterId";

    private const string Where =
        " WHERE (@Search IS NULL OR t.Code LIKE @Search OR t.Name LIKE @Search)" +
        " AND (@CenterId IS NULL OR t.CollectionCenterId = @CenterId)" +
        " AND (@Status IS NULL OR t.Status = @Status)" +
        " AND (@IsActive IS NULL OR t.IsActive = @IsActive)";

    private const string TxnSelect =
        "SELECT tt.TankTransactionId AS Id, tt.TankId, tt.TransactionType, tt.Quantity, tt.BalanceAfter, tt.MilkCollectionId, " +
        "mc.CollectionNumber, tt.TransactionDate, tt.Destination, tt.ReferenceType, tt.ReferenceId, tt.Remarks, tt.CreatedBy, " +
        "u.DisplayName AS CreatedByName";

    private const string TxnFromJoins =
        " FROM `TankTransaction` tt" +
        " LEFT JOIN `MilkCollection` mc ON mc.MilkCollectionId = tt.MilkCollectionId" +
        " LEFT JOIN `User` u ON u.UserId = tt.CreatedBy";

    private const string TxnWhere =
        " WHERE tt.TankId = @TankId AND tt.IsActive = 1" +
        " AND (@DateFrom IS NULL OR tt.TransactionDate >= @DateFrom)" +
        " AND (@DateToExclusive IS NULL OR tt.TransactionDate < @DateToExclusive)" +
        " AND (@Type IS NULL OR tt.TransactionType = @Type)";

    private readonly IDbConnectionFactory _db;

    public MilkTankRepository(IDbConnectionFactory db) => _db = db;

    public async Task<PagedResult<MilkTankDto>> GetPagedAsync(MilkTankQuery query, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        var param = new
        {
            Search = query.SearchPattern,
            CenterId = query.CenterId,
            Status = string.IsNullOrWhiteSpace(query.Status) ? null : query.Status.Trim(),
            query.IsActive
        };
        return await conn.QueryPagedAsync<MilkTankDto>(
            Select + FromJoins + Where,
            "SELECT COUNT(*)" + FromJoins + Where,
            param, query, SortColumns, "cc.Name ASC, t.Name ASC", ct);
    }

    public async Task<MilkTankDto?> GetByIdAsync(long id, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        return await conn.QueryFirstOrNullAsync<MilkTankDto>(Select + FromJoins + " WHERE t.MilkTankId = @Id", new { Id = id }, ct);
    }

    public async Task<bool> CodeExistsAsync(string code, long? excludeId, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        var count = await conn.ScalarAsync<int>(
            "SELECT COUNT(1) FROM `MilkTank` WHERE Code = @Code AND (@ExcludeId IS NULL OR MilkTankId <> @ExcludeId)",
            new { Code = code, ExcludeId = excludeId }, ct);
        return count > 0;
    }

    public async Task<bool> CenterExistsAsync(long centerId, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        var count = await conn.ScalarAsync<int>(
            "SELECT COUNT(1) FROM `CollectionCenter` WHERE CollectionCenterId = @Id AND IsActive = 1", new { Id = centerId }, ct);
        return count > 0;
    }

    public async Task<long> CreateAsync(CreateMilkTankRequest request, long userId, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        return await conn.InsertAsync(
            "INSERT INTO `MilkTank` (Code, Name, CollectionCenterId, CapacityLiters, CurrentQuantity, MilkType, MinTemperature, " +
            "MaxTemperature, CurrentTemperature, Status, IsActive, CreatedBy) " +
            "VALUES (@Code, @Name, @CollectionCenterId, @CapacityLiters, 0, @MilkType, @MinTemperature, @MaxTemperature, " +
            "@CurrentTemperature, 'Active', 1, @UserId)",
            new
            {
                request.Code,
                request.Name,
                request.CollectionCenterId,
                request.CapacityLiters,
                request.MilkType,
                request.MinTemperature,
                request.MaxTemperature,
                request.CurrentTemperature,
                UserId = userId
            }, ct);
    }

    public async Task UpdateAsync(long id, UpdateMilkTankRequest request, bool isActive, long userId, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        await conn.ExecAsync(
            "UPDATE `MilkTank` SET Name = @Name, CollectionCenterId = @CollectionCenterId, CapacityLiters = @CapacityLiters, " +
            "MilkType = @MilkType, MinTemperature = @MinTemperature, MaxTemperature = @MaxTemperature, " +
            "CurrentTemperature = @CurrentTemperature, Status = @Status, LastCleanedDate = @LastCleanedDate, " +
            "IsActive = @IsActive, ModifiedBy = @UserId WHERE MilkTankId = @Id",
            new
            {
                Id = id,
                request.Name,
                request.CollectionCenterId,
                request.CapacityLiters,
                request.MilkType,
                request.MinTemperature,
                request.MaxTemperature,
                request.CurrentTemperature,
                request.Status,
                request.LastCleanedDate,
                IsActive = isActive,
                UserId = userId
            }, ct);
    }

    public async Task<PagedResult<TankTransactionDto>> GetTransactionsAsync(long tankId, TankTransactionQuery query, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        var param = new
        {
            TankId = tankId,
            DateFrom = query.From?.Date,
            DateToExclusive = query.To?.Date.AddDays(1),
            Type = string.IsNullOrWhiteSpace(query.Type) ? null : query.Type.Trim()
        };
        return await conn.QueryPagedAsync<TankTransactionDto>(
            TxnSelect + TxnFromJoins + TxnWhere,
            "SELECT COUNT(*)" + TxnFromJoins + TxnWhere,
            param, query, TxnSortColumns, "tt.TransactionDate DESC, tt.TankTransactionId DESC", ct);
    }

    public async Task<TankTransactionDto> AddTransactionAsync(long tankId, string type, decimal quantity, string? destination,
        string? remarks, long userId, CancellationToken ct = default)
    {
        quantity = Math.Round(quantity, 3);

        await using var conn = await _db.OpenAsync(ct);
        await using var tx = await conn.BeginTransactionAsync(ct);

        var tank = await conn.QueryFirstOrDefaultAsync<MilkTankBalanceRow>(new CommandDefinition(
            "SELECT CurrentQuantity, CapacityLiters FROM `MilkTank` WHERE MilkTankId = @Id FOR UPDATE",
            new { Id = tankId }, tx, cancellationToken: ct)) ?? throw new NotFoundException("Milk tank", tankId);

        var delta = type == "Out" ? -quantity : quantity;
        var newBalance = tank.CurrentQuantity + delta;
        if (type == "Out" && quantity > tank.CurrentQuantity)
            throw new BusinessRuleException($"Cannot remove more milk than the tank holds ({tank.CurrentQuantity:0.###} L).");
        if (newBalance < 0)
            throw new BusinessRuleException($"The adjustment would make the tank balance negative (current {tank.CurrentQuantity:0.###} L).");
        if (newBalance > tank.CapacityLiters)
            throw new BusinessRuleException($"The tank capacity ({tank.CapacityLiters:0.###} L) would be exceeded.");

        var txnId = await conn.ExecuteScalarAsync<long>(new CommandDefinition(
            "INSERT INTO `TankTransaction` (TankId, TransactionType, Quantity, BalanceAfter, TransactionDate, Destination, " +
            "ReferenceType, Remarks, CreatedBy) VALUES (@TankId, @Type, @Quantity, @BalanceAfter, NOW(), @Destination, 'Manual', " +
            "@Remarks, @UserId); SELECT LAST_INSERT_ID();",
            new { TankId = tankId, Type = type, Quantity = quantity, BalanceAfter = newBalance, Destination = destination, Remarks = remarks, UserId = userId },
            tx, cancellationToken: ct));

        await conn.ExecuteAsync(new CommandDefinition(
            "UPDATE `MilkTank` SET CurrentQuantity = @Balance, ModifiedBy = @UserId WHERE MilkTankId = @Id",
            new { Id = tankId, Balance = newBalance, UserId = userId }, tx, cancellationToken: ct));

        await tx.CommitAsync(ct);

        var row = await conn.QueryFirstOrDefaultAsync<TankTransactionDto>(new CommandDefinition(
            TxnSelect + TxnFromJoins + " WHERE tt.TankTransactionId = @Id", new { Id = txnId }, cancellationToken: ct));
        return row ?? throw new InvalidOperationException("Tank transaction was not saved.");
    }
}
