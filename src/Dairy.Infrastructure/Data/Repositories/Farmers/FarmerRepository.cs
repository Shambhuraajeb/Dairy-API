using Dairy.Application.Common;
using Dairy.Application.DTOs.Farmers;
using Dairy.Application.Interfaces;
using Dairy.Application.Interfaces.Farmers;
using Dairy.Application.Requests.Farmers;
using Dapper;
using MySqlConnector;

namespace Dairy.Infrastructure.Data.Repositories.Farmers;

public class FarmerRepository : IFarmerRepository
{
    private static readonly Dictionary<string, string> SortColumns = new(StringComparer.OrdinalIgnoreCase)
    {
        ["id"] = "f.FarmerId",
        ["farmerCode"] = "f.FarmerCode",
        ["fullName"] = "f.FullName",
        ["mobile"] = "f.Mobile",
        ["milkType"] = "f.MilkType",
        ["joinDate"] = "f.JoinDate",
        ["status"] = "f.Status",
        ["verificationStatus"] = "f.VerificationStatus",
        ["collectionCenterName"] = "cc.Name",
        ["createdDate"] = "f.CreatedDate"
    };

    private static readonly Dictionary<string, string> CollectionSortColumns = new(StringComparer.OrdinalIgnoreCase)
    {
        ["id"] = "m.MilkCollectionId",
        ["collectionDate"] = "m.CollectionDate",
        ["collectionNumber"] = "m.CollectionNumber",
        ["quantityLiters"] = "m.QuantityLiters",
        ["amount"] = "m.Amount",
        ["status"] = "m.Status"
    };

    private static readonly Dictionary<string, string> PaymentSortColumns = new(StringComparer.OrdinalIgnoreCase)
    {
        ["id"] = "p.FarmerPaymentId",
        ["paymentDate"] = "p.PaymentDate",
        ["paymentNumber"] = "p.PaymentNumber",
        ["netPayable"] = "p.NetPayable",
        ["status"] = "p.Status"
    };

    private const string AnimalCountSql =
        "(SELECT COUNT(*) FROM `Animal` an WHERE an.FarmerId = f.FarmerId AND an.Status IN ('Active', 'Sick'))";

    private const string ListFrom =
        " FROM `Farmer` f LEFT JOIN `CollectionCenter` cc ON cc.CollectionCenterId = f.CollectionCenterId";

    private const string ListSelect =
        "SELECT f.FarmerId AS Id, f.FarmerCode, f.FullName, f.FatherName, f.Mobile, f.Email, f.MilkType, " +
        "f.CollectionCenterId, cc.Name AS CollectionCenterName, f.BranchId, f.JoinDate, f.VerificationStatus, " +
        "f.Status, f.IsActive, " + AnimalCountSql + " AS AnimalCount" + ListFrom;

    private const string ListWhere =
        " WHERE (@Search IS NULL OR f.FullName LIKE @Search OR f.FarmerCode LIKE @Search OR f.Mobile LIKE @Search)" +
        " AND (@Status IS NULL OR f.Status = @Status)" +
        " AND (@VerificationStatus IS NULL OR f.VerificationStatus = @VerificationStatus)" +
        " AND (@CollectionCenterId IS NULL OR f.CollectionCenterId = @CollectionCenterId)" +
        " AND (@MilkType IS NULL OR f.MilkType = @MilkType)" +
        " AND (@FarmerId IS NULL OR f.FarmerId = @FarmerId)";

    private const string DetailSelect =
        "SELECT f.FarmerId AS Id, f.FarmerCode, f.FullName, f.FatherName, f.Mobile, f.Email, f.MilkType, " +
        "f.CollectionCenterId, cc.Name AS CollectionCenterName, f.BranchId, b.Name AS BranchName, f.JoinDate, " +
        "f.VerificationStatus, f.Status, f.IsActive, f.AlternateMobile, f.Gender, f.DateOfBirth, f.AadhaarNo, f.PanNo, " +
        "f.VerifiedBy, f.VerifiedDate, f.VerificationRemarks, f.StatusReason, f.ActivatedDate, f.DeactivatedDate, " +
        "f.Notes, f.CreatedDate, " + AnimalCountSql + " AS AnimalCount " +
        "FROM `Farmer` f LEFT JOIN `CollectionCenter` cc ON cc.CollectionCenterId = f.CollectionCenterId " +
        "LEFT JOIN `Branch` b ON b.BranchId = f.BranchId";

    private const string AddressSelect =
        "SELECT a.FarmerAddressId AS Id, a.FarmerId, a.AddressType, a.Line1, a.Line2, a.Village, a.Taluka, a.District, " +
        "a.City, a.State, a.Pincode, a.Latitude, a.Longitude, a.IsDefault FROM `FarmerAddress` a";

    private const string BankSelect =
        "SELECT a.FarmerBankAccountId AS Id, a.FarmerId, a.AccountHolderName, a.BankName, a.BranchName, a.AccountNumber, " +
        "a.IfscCode, a.UpiId, a.IsPrimary FROM `FarmerBankAccount` a";

    private const string DocumentSelect =
        "SELECT d.FarmerDocumentId AS Id, d.FarmerId, d.DocumentTypeId, dt.Name AS DocumentTypeName, d.DocumentNumber, " +
        "d.FileName, d.FilePath, (d.FilePath <> '') AS HasFile, d.ContentType, d.FileSize, d.ExpiryDate, " +
        "d.VerificationStatus, d.VerifiedDate, d.Remarks, d.CreatedDate " +
        "FROM `FarmerDocument` d LEFT JOIN `DocumentType` dt ON dt.DocumentTypeId = d.DocumentTypeId";

    private readonly IDbConnectionFactory _db;

    public FarmerRepository(IDbConnectionFactory db) => _db = db;

    // ------------------------------------------------------------------ farmers

    public async Task<PagedResult<FarmerListDto>> GetPagedAsync(FarmerQuery query, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        var param = new
        {
            Search = query.SearchPattern,
            Status = Clean(query.Status),
            VerificationStatus = Clean(query.VerificationStatus),
            query.CollectionCenterId,
            MilkType = Clean(query.MilkType),
            query.FarmerId
        };
        return await conn.QueryPagedAsync<FarmerListDto>(
            ListSelect + ListWhere,
            "SELECT COUNT(*)" + ListFrom + ListWhere,
            param, query, SortColumns, "f.FullName ASC", ct);
    }

    public async Task<FarmerListDto?> GetBasicAsync(long id, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        return await conn.QueryFirstOrNullAsync<FarmerListDto>(ListSelect + " WHERE f.FarmerId = @Id", new { Id = id }, ct);
    }

    public async Task<FarmerDetailDto?> GetDetailAsync(long id, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        var farmer = await conn.QueryFirstOrNullAsync<FarmerDetailDto>(DetailSelect + " WHERE f.FarmerId = @Id", new { Id = id }, ct);
        if (farmer is null) return null;

        farmer.Addresses = await LoadAddressesAsync(conn, id, ct);
        farmer.BankAccounts = await LoadBankAccountsAsync(conn, id, ct);
        farmer.Documents = await LoadDocumentsAsync(conn, id, ct);
        return farmer;
    }

    public async Task<bool> CodeExistsAsync(string code, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        var count = await conn.ScalarAsync<int>("SELECT COUNT(1) FROM `Farmer` WHERE FarmerCode = @Code", new { Code = code }, ct);
        return count > 0;
    }

    public async Task<string> GenerateCodeAsync(CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        var next = await conn.ScalarAsync<long>(
            "SELECT COALESCE(MAX(CAST(SUBSTRING(FarmerCode, 5) AS UNSIGNED)), 0) + 1 FROM `Farmer` WHERE FarmerCode REGEXP '^FRM-[0-9]+$'",
            null, ct);
        return "FRM-" + next.ToString("D6");
    }

    public async Task<long> CreateAsync(CreateFarmerRequest request, string farmerCode, DateTime joinDate, long userId, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        await using var tx = await conn.BeginTransactionAsync(ct);
        try
        {
            var id = await ScalarLongAsync(conn, tx,
                "INSERT INTO `Farmer` (FarmerCode, FullName, FatherName, Mobile, AlternateMobile, Email, Gender, DateOfBirth, " +
                "AadhaarNo, PanNo, CollectionCenterId, BranchId, MilkType, JoinDate, VerificationStatus, Status, Notes, CreatedBy) " +
                "VALUES (@FarmerCode, @FullName, @FatherName, @Mobile, @AlternateMobile, @Email, @Gender, @DateOfBirth, " +
                "@AadhaarNo, @PanNo, @CollectionCenterId, " + BranchExpr + ", @MilkType, @JoinDate, 'Pending', 'Pending', @Notes, @UserId); " +
                "SELECT LAST_INSERT_ID();",
                new
                {
                    FarmerCode = farmerCode,
                    FullName = request.FullName.Trim(),
                    FatherName = Clean(request.FatherName),
                    Mobile = request.Mobile.Trim(),
                    AlternateMobile = Clean(request.AlternateMobile),
                    Email = Clean(request.Email),
                    Gender = Clean(request.Gender),
                    request.DateOfBirth,
                    AadhaarNo = Clean(request.AadhaarNo),
                    PanNo = Clean(request.PanNo)?.ToUpperInvariant(),
                    request.CollectionCenterId,
                    request.BranchId,
                    MilkType = request.MilkType.Trim(),
                    JoinDate = joinDate,
                    Notes = Clean(request.Notes),
                    UserId = userId
                }, ct);

            if (request.Address is not null)
                await InsertAddressAsync(conn, tx, id, request.Address, userId, ct);
            if (request.BankAccount is not null)
                await InsertBankAccountAsync(conn, tx, id, request.BankAccount, userId, ct);

            await tx.CommitAsync(ct);
            return id;
        }
        catch
        {
            await tx.RollbackAsync(ct);
            throw;
        }
    }

    private const string BranchExpr =
        "COALESCE(@BranchId, (SELECT cc2.BranchId FROM `CollectionCenter` cc2 WHERE cc2.CollectionCenterId = @CollectionCenterId))";

    public async Task UpdateAsync(long id, UpdateFarmerRequest request, DateTime? joinDate, long userId, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        await conn.ExecAsync(
            "UPDATE `Farmer` SET FullName = @FullName, FatherName = @FatherName, Mobile = @Mobile, AlternateMobile = @AlternateMobile, " +
            "Email = @Email, Gender = @Gender, DateOfBirth = @DateOfBirth, AadhaarNo = @AadhaarNo, PanNo = @PanNo, " +
            "CollectionCenterId = @CollectionCenterId, BranchId = " + BranchExpr + ", MilkType = @MilkType, " +
            "JoinDate = COALESCE(@JoinDate, JoinDate), Notes = @Notes, ModifiedBy = @UserId WHERE FarmerId = @Id",
            new
            {
                Id = id,
                FullName = request.FullName.Trim(),
                FatherName = Clean(request.FatherName),
                Mobile = request.Mobile.Trim(),
                AlternateMobile = Clean(request.AlternateMobile),
                Email = Clean(request.Email),
                Gender = Clean(request.Gender),
                request.DateOfBirth,
                AadhaarNo = Clean(request.AadhaarNo),
                PanNo = Clean(request.PanNo)?.ToUpperInvariant(),
                request.CollectionCenterId,
                request.BranchId,
                MilkType = request.MilkType.Trim(),
                JoinDate = joinDate,
                Notes = Clean(request.Notes),
                UserId = userId
            }, ct);
    }

    public async Task SetVerificationAsync(long id, string verificationStatus, string? remarks, long userId, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        await conn.ExecAsync(
            "UPDATE `Farmer` SET VerificationStatus = @VerificationStatus, VerifiedBy = @UserId, VerifiedDate = NOW(), " +
            "VerificationRemarks = @Remarks, ModifiedBy = @UserId WHERE FarmerId = @Id",
            new { Id = id, VerificationStatus = verificationStatus, Remarks = Clean(remarks), UserId = userId }, ct);
    }

    public async Task ActivateAsync(long id, long userId, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        await conn.ExecAsync(
            "UPDATE `Farmer` SET Status = 'Active', StatusReason = NULL, ActivatedDate = NOW(), ModifiedBy = @UserId WHERE FarmerId = @Id",
            new { Id = id, UserId = userId }, ct);
    }

    public async Task DeactivateAsync(long id, string reason, long userId, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        await conn.ExecAsync(
            "UPDATE `Farmer` SET Status = 'Inactive', StatusReason = @Reason, DeactivatedDate = NOW(), ModifiedBy = @UserId WHERE FarmerId = @Id",
            new { Id = id, Reason = reason, UserId = userId }, ct);
    }

    public async Task<int> CountActiveBankAccountsAsync(long farmerId, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        return await conn.ScalarAsync<int>(
            "SELECT COUNT(1) FROM `FarmerBankAccount` WHERE FarmerId = @FarmerId AND IsActive = 1",
            new { FarmerId = farmerId }, ct);
    }

    // ------------------------------------------------------------------ summary & sub-lists

    public async Task<FarmerSummaryDto?> GetSummaryAsync(long farmerId, DateTime monthStart, DateTime monthEnd, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        return await conn.QueryFirstOrNullAsync<FarmerSummaryDto>(
            "SELECT f.FarmerId AS FarmerId, " +
            "(SELECT COUNT(*) FROM `Animal` a1 WHERE a1.FarmerId = f.FarmerId AND a1.Status IN ('Active', 'Sick')) AS AnimalCount, " +
            "(SELECT COUNT(*) FROM `Animal` a2 WHERE a2.FarmerId = f.FarmerId AND a2.Status IN ('Active', 'Sick') AND a2.MilkingStatus = 'Milking') AS MilkingAnimalCount, " +
            "(SELECT COUNT(*) FROM `MilkCollection` m1 WHERE m1.FarmerId = f.FarmerId AND m1.IsActive = 1 AND m1.Status <> 'Rejected' " +
            "   AND m1.CollectionDate BETWEEN @MonthStart AND @MonthEnd) AS CollectionsThisMonth, " +
            "(SELECT COALESCE(SUM(m2.QuantityLiters), 0) FROM `MilkCollection` m2 WHERE m2.FarmerId = f.FarmerId AND m2.IsActive = 1 AND m2.Status <> 'Rejected' " +
            "   AND m2.CollectionDate BETWEEN @MonthStart AND @MonthEnd) AS QuantityThisMonth, " +
            "(SELECT COALESCE(SUM(m3.Amount), 0) FROM `MilkCollection` m3 WHERE m3.FarmerId = f.FarmerId AND m3.IsActive = 1 AND m3.Status <> 'Rejected' " +
            "   AND m3.CollectionDate BETWEEN @MonthStart AND @MonthEnd) AS AmountThisMonth, " +
            "(SELECT COUNT(*) FROM `MilkCollection` m4 WHERE m4.FarmerId = f.FarmerId AND m4.IsActive = 1 AND m4.Status = 'Pending') AS PendingApprovalCount, " +
            "(SELECT COALESCE(SUM(m5.Amount), 0) FROM `MilkCollection` m5 WHERE m5.FarmerId = f.FarmerId AND m5.IsActive = 1 " +
            "   AND m5.Status = 'Approved' AND m5.SettlementStatus = 'Unsettled') AS PayableAmount, " +
            "(SELECT COALESCE(SUM(l.OutstandingAmount), 0) FROM `FarmerLoan` l WHERE l.FarmerId = f.FarmerId AND l.IsActive = 1 AND l.Status = 'Active') AS LoanBalance, " +
            "(SELECT COALESCE(SUM(ad.BalanceAmount), 0) FROM `FarmerAdvance` ad WHERE ad.FarmerId = f.FarmerId AND ad.IsActive = 1 AND ad.Status = 'Open') AS AdvanceBalance, " +
            "(SELECT MAX(m6.CollectionDate) FROM `MilkCollection` m6 WHERE m6.FarmerId = f.FarmerId AND m6.IsActive = 1) AS LastCollectionDate " +
            "FROM `Farmer` f WHERE f.FarmerId = @FarmerId",
            new { FarmerId = farmerId, MonthStart = monthStart.Date, MonthEnd = monthEnd.Date }, ct);
    }

    public async Task<PagedResult<FarmerCollectionRowDto>> GetCollectionsPagedAsync(long farmerId, FarmerSubListQuery query, CancellationToken ct = default)
    {
        const string fromSql =
            " FROM `MilkCollection` m JOIN `CollectionShift` s ON s.CollectionShiftId = m.ShiftId " +
            "JOIN `CollectionCenter` cc ON cc.CollectionCenterId = m.CollectionCenterId " +
            "WHERE m.FarmerId = @FarmerId AND m.IsActive = 1" +
            " AND (@Search IS NULL OR m.CollectionNumber LIKE @Search)" +
            " AND (@DateFrom IS NULL OR m.CollectionDate >= @DateFrom)" +
            " AND (@DateTo IS NULL OR m.CollectionDate <= @DateTo)" +
            " AND (@Status IS NULL OR m.Status = @Status)";

        await using var conn = await _db.OpenAsync(ct);
        var param = new
        {
            FarmerId = farmerId,
            Search = query.SearchPattern,
            DateFrom = query.DateFrom?.Date,
            DateTo = query.DateTo?.Date,
            Status = Clean(query.Status)
        };
        return await conn.QueryPagedAsync<FarmerCollectionRowDto>(
            "SELECT m.MilkCollectionId AS Id, m.CollectionNumber, m.CollectionDate, s.ShiftName, cc.Name AS CollectionCenterName, " +
            "m.QuantityLiters, m.Fat, m.Snf, m.Rate, m.Amount, m.Status, m.SettlementStatus" + fromSql,
            "SELECT COUNT(*)" + fromSql,
            param, query, CollectionSortColumns, "m.CollectionDate DESC, m.MilkCollectionId DESC", ct);
    }

    public async Task<PagedResult<FarmerPaymentRowDto>> GetPaymentsPagedAsync(long farmerId, FarmerSubListQuery query, CancellationToken ct = default)
    {
        const string fromSql =
            " FROM `FarmerPayment` p WHERE p.FarmerId = @FarmerId AND p.IsActive = 1" +
            " AND (@Search IS NULL OR p.PaymentNumber LIKE @Search)" +
            " AND (@DateFrom IS NULL OR p.PaymentDate >= @DateFrom)" +
            " AND (@DateTo IS NULL OR p.PaymentDate <= @DateTo)" +
            " AND (@Status IS NULL OR p.Status = @Status)";

        await using var conn = await _db.OpenAsync(ct);
        var param = new
        {
            FarmerId = farmerId,
            Search = query.SearchPattern,
            DateFrom = query.DateFrom?.Date,
            DateTo = query.DateTo?.Date,
            Status = Clean(query.Status)
        };
        return await conn.QueryPagedAsync<FarmerPaymentRowDto>(
            "SELECT p.FarmerPaymentId AS Id, p.PaymentNumber, p.PaymentDate, p.PeriodFrom, p.PeriodTo, p.TotalQuantity, " +
            "p.GrossAmount, p.DeductionAmount, p.NetPayable, p.PaidAmount, p.BalanceAmount, p.Status, p.PaidDate" + fromSql,
            "SELECT COUNT(*)" + fromSql,
            param, query, PaymentSortColumns, "p.PaymentDate DESC, p.FarmerPaymentId DESC", ct);
    }

    // ------------------------------------------------------------------ addresses

    public async Task<List<FarmerAddressDto>> GetAddressesAsync(long farmerId, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        return await LoadAddressesAsync(conn, farmerId, ct);
    }

    public async Task<FarmerAddressDto?> GetAddressAsync(long farmerId, long addressId, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        return await conn.QueryFirstOrNullAsync<FarmerAddressDto>(
            AddressSelect + " WHERE a.FarmerAddressId = @AddressId AND a.FarmerId = @FarmerId AND a.IsActive = 1",
            new { AddressId = addressId, FarmerId = farmerId }, ct);
    }

    public async Task<long> AddAddressAsync(long farmerId, FarmerAddressRequest request, long userId, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        return await InsertAddressAsync(conn, null, farmerId, request, userId, ct);
    }

    public async Task UpdateAddressAsync(long farmerId, long addressId, FarmerAddressRequest request, long userId, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        if (request.IsDefault)
        {
            await conn.ExecAsync(
                "UPDATE `FarmerAddress` SET IsDefault = 0 WHERE FarmerId = @FarmerId AND FarmerAddressId <> @AddressId",
                new { FarmerId = farmerId, AddressId = addressId }, ct);
        }

        await conn.ExecAsync(
            "UPDATE `FarmerAddress` SET AddressType = @AddressType, Line1 = @Line1, Line2 = @Line2, Village = @Village, " +
            "Taluka = @Taluka, District = @District, City = @City, State = @State, Pincode = @Pincode, Latitude = @Latitude, " +
            "Longitude = @Longitude, IsDefault = @IsDefault, ModifiedBy = @UserId " +
            "WHERE FarmerAddressId = @AddressId AND FarmerId = @FarmerId AND IsActive = 1",
            AddressParams(farmerId, addressId, request, request.IsDefault, userId), ct);

        await EnsureOneFlaggedAsync(conn, "FarmerAddress", "FarmerAddressId", "IsDefault", farmerId, ct);
    }

    public async Task DeleteAddressAsync(long farmerId, long addressId, long userId, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        await conn.ExecAsync(
            "UPDATE `FarmerAddress` SET IsActive = 0, IsDefault = 0, ModifiedBy = @UserId WHERE FarmerAddressId = @AddressId AND FarmerId = @FarmerId",
            new { AddressId = addressId, FarmerId = farmerId, UserId = userId }, ct);
        await EnsureOneFlaggedAsync(conn, "FarmerAddress", "FarmerAddressId", "IsDefault", farmerId, ct);
    }

    // ------------------------------------------------------------------ bank accounts

    public async Task<List<FarmerBankAccountDto>> GetBankAccountsAsync(long farmerId, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        return await LoadBankAccountsAsync(conn, farmerId, ct);
    }

    public async Task<FarmerBankAccountDto?> GetBankAccountAsync(long farmerId, long bankAccountId, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        return await conn.QueryFirstOrNullAsync<FarmerBankAccountDto>(
            BankSelect + " WHERE a.FarmerBankAccountId = @AccountId AND a.FarmerId = @FarmerId AND a.IsActive = 1",
            new { AccountId = bankAccountId, FarmerId = farmerId }, ct);
    }

    public async Task<bool> BankAccountNumberExistsAsync(long farmerId, string accountNumber, long? excludeId, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        var count = await conn.ScalarAsync<int>(
            "SELECT COUNT(1) FROM `FarmerBankAccount` WHERE FarmerId = @FarmerId AND AccountNumber = @AccountNumber " +
            "AND (@ExcludeId IS NULL OR FarmerBankAccountId <> @ExcludeId)",
            new { FarmerId = farmerId, AccountNumber = accountNumber, ExcludeId = excludeId }, ct);
        return count > 0;
    }

    public async Task<long> AddBankAccountAsync(long farmerId, FarmerBankAccountRequest request, long userId, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        return await InsertBankAccountAsync(conn, null, farmerId, request, userId, ct);
    }

    public async Task UpdateBankAccountAsync(long farmerId, long bankAccountId, FarmerBankAccountRequest request, long userId, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        if (request.IsPrimary)
        {
            await conn.ExecAsync(
                "UPDATE `FarmerBankAccount` SET IsPrimary = 0 WHERE FarmerId = @FarmerId AND FarmerBankAccountId <> @AccountId",
                new { FarmerId = farmerId, AccountId = bankAccountId }, ct);
        }

        await conn.ExecAsync(
            "UPDATE `FarmerBankAccount` SET AccountHolderName = @AccountHolderName, BankName = @BankName, BranchName = @BranchName, " +
            "AccountNumber = @AccountNumber, IfscCode = @IfscCode, UpiId = @UpiId, IsPrimary = @IsPrimary, ModifiedBy = @UserId " +
            "WHERE FarmerBankAccountId = @AccountId AND FarmerId = @FarmerId AND IsActive = 1",
            BankParams(farmerId, bankAccountId, request, request.IsPrimary, userId), ct);

        await EnsureOneFlaggedAsync(conn, "FarmerBankAccount", "FarmerBankAccountId", "IsPrimary", farmerId, ct);
    }

    public async Task DeleteBankAccountAsync(long farmerId, long bankAccountId, long userId, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        var param = new { AccountId = bankAccountId, FarmerId = farmerId, UserId = userId };
        try
        {
            await conn.ExecAsync(
                "DELETE FROM `FarmerBankAccount` WHERE FarmerBankAccountId = @AccountId AND FarmerId = @FarmerId", param, ct);
        }
        catch (MySqlException ex) when ((int)ex.ErrorCode == 1451)
        {
            // Referenced by a farmer payment: keep the row for history, just retire it.
            await conn.ExecAsync(
                "UPDATE `FarmerBankAccount` SET IsActive = 0, IsPrimary = 0, ModifiedBy = @UserId " +
                "WHERE FarmerBankAccountId = @AccountId AND FarmerId = @FarmerId", param, ct);
        }

        await EnsureOneFlaggedAsync(conn, "FarmerBankAccount", "FarmerBankAccountId", "IsPrimary", farmerId, ct);
    }

    // ------------------------------------------------------------------ documents

    public async Task<List<FarmerDocumentDto>> GetDocumentsAsync(long farmerId, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        return await LoadDocumentsAsync(conn, farmerId, ct);
    }

    public async Task<FarmerDocumentDto?> GetDocumentAsync(long farmerId, long documentId, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        return await conn.QueryFirstOrNullAsync<FarmerDocumentDto>(
            DocumentSelect + " WHERE d.FarmerDocumentId = @DocumentId AND d.FarmerId = @FarmerId AND d.IsActive = 1",
            new { DocumentId = documentId, FarmerId = farmerId }, ct);
    }

    public async Task<long> AddDocumentAsync(FarmerDocumentInsert document, long userId, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        return await conn.InsertAsync(
            "INSERT INTO `FarmerDocument` (FarmerId, DocumentTypeId, DocumentNumber, FileName, FilePath, ContentType, FileSize, " +
            "ExpiryDate, VerificationStatus, Remarks, CreatedBy) " +
            "VALUES (@FarmerId, @DocumentTypeId, @DocumentNumber, @FileName, @FilePath, @ContentType, @FileSize, " +
            "@ExpiryDate, 'Pending', @Remarks, @UserId)",
            new
            {
                document.FarmerId,
                document.DocumentTypeId,
                DocumentNumber = Clean(document.DocumentNumber),
                document.FileName,
                document.FilePath,
                document.ContentType,
                document.FileSize,
                document.ExpiryDate,
                Remarks = Clean(document.Remarks),
                UserId = userId
            }, ct);
    }

    public async Task UpdateDocumentAsync(long farmerId, long documentId, UpdateFarmerDocumentRequest request, long userId, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        var param = new
        {
            DocumentId = documentId,
            FarmerId = farmerId,
            request.DocumentTypeId,
            DocumentNumber = Clean(request.DocumentNumber),
            request.ExpiryDate,
            Remarks = Clean(request.Remarks),
            VerificationStatus = Clean(request.VerificationStatus),
            UserId = userId
        };

        await conn.ExecAsync(
            "UPDATE `FarmerDocument` SET DocumentTypeId = @DocumentTypeId, DocumentNumber = @DocumentNumber, " +
            "ExpiryDate = @ExpiryDate, Remarks = @Remarks, ModifiedBy = @UserId " +
            "WHERE FarmerDocumentId = @DocumentId AND FarmerId = @FarmerId AND IsActive = 1", param, ct);

        if (param.VerificationStatus is not null)
        {
            await conn.ExecAsync(
                "UPDATE `FarmerDocument` SET VerificationStatus = @VerificationStatus, VerifiedBy = @UserId, VerifiedDate = NOW() " +
                "WHERE FarmerDocumentId = @DocumentId AND FarmerId = @FarmerId AND IsActive = 1", param, ct);
        }
    }

    public async Task DeleteDocumentAsync(long farmerId, long documentId, long userId, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        await conn.ExecAsync(
            "UPDATE `FarmerDocument` SET IsActive = 0, ModifiedBy = @UserId WHERE FarmerDocumentId = @DocumentId AND FarmerId = @FarmerId",
            new { DocumentId = documentId, FarmerId = farmerId, UserId = userId }, ct);
    }

    // ------------------------------------------------------------------ helpers

    private static string? Clean(string? value)
    {
        var v = value?.Trim();
        return string.IsNullOrEmpty(v) ? null : v;
    }

    private static Task<List<FarmerAddressDto>> LoadAddressesAsync(MySqlConnection conn, long farmerId, CancellationToken ct) =>
        conn.QueryListAsync<FarmerAddressDto>(
            AddressSelect + " WHERE a.FarmerId = @FarmerId AND a.IsActive = 1 ORDER BY a.IsDefault DESC, a.FarmerAddressId",
            new { FarmerId = farmerId }, ct);

    private static Task<List<FarmerBankAccountDto>> LoadBankAccountsAsync(MySqlConnection conn, long farmerId, CancellationToken ct) =>
        conn.QueryListAsync<FarmerBankAccountDto>(
            BankSelect + " WHERE a.FarmerId = @FarmerId AND a.IsActive = 1 ORDER BY a.IsPrimary DESC, a.FarmerBankAccountId",
            new { FarmerId = farmerId }, ct);

    private static Task<List<FarmerDocumentDto>> LoadDocumentsAsync(MySqlConnection conn, long farmerId, CancellationToken ct) =>
        conn.QueryListAsync<FarmerDocumentDto>(
            DocumentSelect + " WHERE d.FarmerId = @FarmerId AND d.IsActive = 1 ORDER BY d.FarmerDocumentId DESC",
            new { FarmerId = farmerId }, ct);

    private static async Task<long> ScalarLongAsync(MySqlConnection conn, MySqlTransaction? tx, string sql, object? param, CancellationToken ct) =>
        await conn.ExecuteScalarAsync<long>(new CommandDefinition(sql, param, tx, cancellationToken: ct));

    private static Task<int> ExecTxAsync(MySqlConnection conn, MySqlTransaction? tx, string sql, object? param, CancellationToken ct) =>
        conn.ExecuteAsync(new CommandDefinition(sql, param, tx, cancellationToken: ct));

    private static object AddressParams(long farmerId, long addressId, FarmerAddressRequest r, bool isDefault, long userId) => new
    {
        AddressId = addressId,
        FarmerId = farmerId,
        AddressType = r.AddressType.Trim(),
        Line1 = r.Line1.Trim(),
        Line2 = Clean(r.Line2),
        Village = Clean(r.Village),
        Taluka = Clean(r.Taluka),
        District = Clean(r.District),
        City = Clean(r.City),
        State = Clean(r.State),
        Pincode = Clean(r.Pincode),
        r.Latitude,
        r.Longitude,
        IsDefault = isDefault,
        UserId = userId
    };

    private static object BankParams(long farmerId, long accountId, FarmerBankAccountRequest r, bool isPrimary, long userId) => new
    {
        AccountId = accountId,
        FarmerId = farmerId,
        AccountHolderName = r.AccountHolderName.Trim(),
        BankName = r.BankName.Trim(),
        BranchName = Clean(r.BranchName),
        AccountNumber = r.AccountNumber.Trim(),
        IfscCode = r.IfscCode.Trim().ToUpperInvariant(),
        UpiId = Clean(r.UpiId),
        IsPrimary = isPrimary,
        UserId = userId
    };

    private static async Task<long> InsertAddressAsync(MySqlConnection conn, MySqlTransaction? tx, long farmerId, FarmerAddressRequest r, long userId, CancellationToken ct)
    {
        var existing = await ScalarLongAsync(conn, tx,
            "SELECT COUNT(*) FROM `FarmerAddress` WHERE FarmerId = @FarmerId AND IsActive = 1", new { FarmerId = farmerId }, ct);
        var isDefault = r.IsDefault || existing == 0;
        if (isDefault && existing > 0)
            await ExecTxAsync(conn, tx, "UPDATE `FarmerAddress` SET IsDefault = 0 WHERE FarmerId = @FarmerId", new { FarmerId = farmerId }, ct);

        return await ScalarLongAsync(conn, tx,
            "INSERT INTO `FarmerAddress` (FarmerId, AddressType, Line1, Line2, Village, Taluka, District, City, State, Pincode, " +
            "Latitude, Longitude, IsDefault, CreatedBy) " +
            "VALUES (@FarmerId, @AddressType, @Line1, @Line2, @Village, @Taluka, @District, @City, @State, @Pincode, " +
            "@Latitude, @Longitude, @IsDefault, @UserId); SELECT LAST_INSERT_ID();",
            AddressParams(farmerId, 0, r, isDefault, userId), ct);
    }

    private static async Task<long> InsertBankAccountAsync(MySqlConnection conn, MySqlTransaction? tx, long farmerId, FarmerBankAccountRequest r, long userId, CancellationToken ct)
    {
        var existing = await ScalarLongAsync(conn, tx,
            "SELECT COUNT(*) FROM `FarmerBankAccount` WHERE FarmerId = @FarmerId AND IsActive = 1", new { FarmerId = farmerId }, ct);
        var isPrimary = r.IsPrimary || existing == 0;
        if (isPrimary && existing > 0)
            await ExecTxAsync(conn, tx, "UPDATE `FarmerBankAccount` SET IsPrimary = 0 WHERE FarmerId = @FarmerId", new { FarmerId = farmerId }, ct);

        return await ScalarLongAsync(conn, tx,
            "INSERT INTO `FarmerBankAccount` (FarmerId, AccountHolderName, BankName, BranchName, AccountNumber, IfscCode, UpiId, " +
            "IsPrimary, CreatedBy) " +
            "VALUES (@FarmerId, @AccountHolderName, @BankName, @BranchName, @AccountNumber, @IfscCode, @UpiId, @IsPrimary, @UserId); " +
            "SELECT LAST_INSERT_ID();",
            BankParams(farmerId, 0, r, isPrimary, userId), ct);
    }

    /// <summary>Makes sure one active row of the farmer carries the flag (default address / primary account). Table and column names are internal constants.</summary>
    private static async Task EnsureOneFlaggedAsync(MySqlConnection conn, string table, string pk, string flag, long farmerId, CancellationToken ct)
    {
        var flagged = await conn.ScalarAsync<long>(
            $"SELECT COUNT(*) FROM `{table}` WHERE FarmerId = @FarmerId AND IsActive = 1 AND {flag} = 1",
            new { FarmerId = farmerId }, ct);
        if (flagged > 0) return;

        var firstId = await conn.ScalarAsync<long?>(
            $"SELECT MIN({pk}) FROM `{table}` WHERE FarmerId = @FarmerId AND IsActive = 1",
            new { FarmerId = farmerId }, ct);
        if (firstId is null) return;

        await conn.ExecAsync($"UPDATE `{table}` SET {flag} = 1 WHERE {pk} = @Id", new { Id = firstId.Value }, ct);
    }
}
