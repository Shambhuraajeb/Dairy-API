using Dairy.Application.Common;
using Dairy.Application.DTOs.Animals;
using Dairy.Application.Interfaces;
using Dairy.Application.Interfaces.Animals;
using Dairy.Application.Requests.Animals;
using Dapper;

namespace Dairy.Infrastructure.Data.Repositories.Animals;

public class AnimalRepository : IAnimalRepository
{
    private static readonly Dictionary<string, string> SortColumns = new(StringComparer.OrdinalIgnoreCase)
    {
        ["id"] = "a.AnimalId",
        ["tagNumber"] = "a.TagNumber",
        ["name"] = "a.Name",
        ["farmerName"] = "f.FullName",
        ["breedName"] = "b.Name",
        ["animalType"] = "a.AnimalType",
        ["gender"] = "a.Gender",
        ["milkingStatus"] = "a.MilkingStatus",
        ["pregnancyStatus"] = "a.PregnancyStatus",
        ["status"] = "a.Status",
        ["registrationDate"] = "a.RegistrationDate",
        ["dateOfBirth"] = "a.DateOfBirth"
    };

    private static readonly Dictionary<string, string> DueSortColumns = new(StringComparer.OrdinalIgnoreCase)
    {
        ["dueDate"] = "d.DueDate",
        ["tagNumber"] = "a.TagNumber",
        ["farmerName"] = "f.FullName",
        ["title"] = "d.Title"
    };

    private const string From =
        " FROM `Animal` a JOIN `Farmer` f ON f.FarmerId = a.FarmerId LEFT JOIN `Breed` b ON b.BreedId = a.BreedId";

    private const string ListColumns =
        "a.AnimalId AS Id, a.TagNumber, a.Name, a.FarmerId, f.FarmerCode, f.FullName AS FarmerName, a.BreedId, b.Name AS BreedName, " +
        "a.AnimalType, a.Gender, a.DateOfBirth, TIMESTAMPDIFF(MONTH, a.DateOfBirth, CURDATE()) AS AgeMonths, a.MilkingStatus, " +
        "a.PregnancyStatus, a.ExpectedDeliveryDate, a.Status, a.RegistrationDate, a.IsActive";

    private const string DetailColumns =
        ListColumns + ", a.Color, a.WeightKg, a.Source, a.LactationNo, a.LastCalvingDate, a.StatusDate, a.Remarks, a.CreatedDate";

    private const string Where =
        " WHERE (@Search IS NULL OR a.TagNumber LIKE @Search OR a.Name LIKE @Search OR f.FullName LIKE @Search OR f.FarmerCode LIKE @Search)" +
        " AND (@FarmerId IS NULL OR a.FarmerId = @FarmerId)" +
        " AND (@BreedId IS NULL OR a.BreedId = @BreedId)" +
        " AND (@Status IS NULL OR a.Status = @Status)" +
        " AND (@AnimalType IS NULL OR a.AnimalType = @AnimalType)" +
        " AND (@MilkingStatus IS NULL OR a.MilkingStatus = @MilkingStatus)" +
        " AND (@PregnancyStatus IS NULL OR a.PregnancyStatus = @PregnancyStatus)";

    private const string VaccinationDueInner =
        "SELECT v.AnimalId, 'Vaccination' AS DueType, v.VaccineName AS Title, v.NextDueDate AS DueDate, v.Status AS RecordStatus, v.VaccinationId AS RefId " +
        "FROM `Vaccination` v WHERE v.IsActive = 1 AND v.Status = 'Completed' AND v.NextDueDate IS NOT NULL " +
        "AND NOT EXISTS (SELECT 1 FROM `Vaccination` v2 WHERE v2.AnimalId = v.AnimalId AND v2.VaccineName = v.VaccineName " +
        "AND v2.IsActive = 1 AND v2.VaccinationId <> v.VaccinationId AND v2.VaccinationDate > v.VaccinationDate) " +
        "UNION ALL " +
        "SELECT v.AnimalId, 'Vaccination', v.VaccineName, v.VaccinationDate, v.Status, v.VaccinationId " +
        "FROM `Vaccination` v WHERE v.IsActive = 1 AND v.Status IN ('Scheduled', 'Missed')";

    private const string DewormingDueInner =
        "SELECT w.AnimalId, 'Deworming' AS DueType, w.MedicineName AS Title, w.NextDueDate AS DueDate, w.Status AS RecordStatus, w.DewormingId AS RefId " +
        "FROM `Deworming` w WHERE w.IsActive = 1 AND w.Status = 'Completed' AND w.NextDueDate IS NOT NULL " +
        "AND NOT EXISTS (SELECT 1 FROM `Deworming` w2 WHERE w2.AnimalId = w.AnimalId AND w2.MedicineName = w.MedicineName " +
        "AND w2.IsActive = 1 AND w2.DewormingId <> w.DewormingId AND w2.DewormingDate > w.DewormingDate) " +
        "UNION ALL " +
        "SELECT w.AnimalId, 'Deworming', w.MedicineName, w.DewormingDate, w.Status, w.DewormingId " +
        "FROM `Deworming` w WHERE w.IsActive = 1 AND w.Status IN ('Scheduled', 'Missed')";

    private const string PregnancyDueInner =
        "SELECT p.AnimalId, 'Pregnancy' AS DueType, 'Expected delivery' AS Title, p.ExpectedDeliveryDate AS DueDate, p.Status AS RecordStatus, p.PregnancyRecordId AS RefId " +
        "FROM `PregnancyRecord` p WHERE p.IsActive = 1 AND p.Status = 'Pregnant'";

    private const string LifecycleSql =
        "SELECT e.EventDate, e.EventType, e.Title, e.Details, e.RefId FROM (" +
        "SELECT an.RegistrationDate AS EventDate, 'Registration' AS EventType, CONCAT('Registered (', an.Source, ')') AS Title, an.Remarks AS Details, an.AnimalId AS RefId " +
        "FROM `Animal` an WHERE an.AnimalId = @AnimalId " +
        "UNION ALL SELECT h.RecordDate, 'Health', h.HealthCondition, CONCAT_WS(' | ', CONCAT('Severity: ', h.Severity), h.Diagnosis, h.Treatment), h.AnimalHealthRecordId " +
        "FROM `AnimalHealthRecord` h WHERE h.AnimalId = @AnimalId AND h.IsActive = 1 " +
        "UNION ALL SELECT v.VaccinationDate, 'Vaccination', v.VaccineName, CONCAT_WS(' | ', CONCAT('Status: ', v.Status), v.Remarks), v.VaccinationId " +
        "FROM `Vaccination` v WHERE v.AnimalId = @AnimalId AND v.IsActive = 1 " +
        "UNION ALL SELECT w.DewormingDate, 'Deworming', w.MedicineName, CONCAT_WS(' | ', CONCAT('Status: ', w.Status), w.Dosage), w.DewormingId " +
        "FROM `Deworming` w WHERE w.AnimalId = @AnimalId AND w.IsActive = 1 " +
        "UNION ALL SELECT br.BreedingDate, 'Breeding', CONCAT('Breeding (', br.BreedingMethod, ')'), CONCAT_WS(' | ', CONCAT('Result: ', br.Result), br.BullName), br.BreedingRecordId " +
        "FROM `BreedingRecord` br WHERE br.AnimalId = @AnimalId AND br.IsActive = 1 " +
        "UNION ALL SELECT p.ConfirmedDate, 'Pregnancy', 'Pregnancy confirmed', CONCAT('Expected delivery: ', DATE_FORMAT(p.ExpectedDeliveryDate, '%Y-%m-%d')), p.PregnancyRecordId " +
        "FROM `PregnancyRecord` p WHERE p.AnimalId = @AnimalId AND p.IsActive = 1 " +
        "UNION ALL SELECT p2.ActualDeliveryDate, 'Delivery', CONCAT('Delivery: ', COALESCE(p2.DeliveryOutcome, p2.Status)), CONCAT('Calves: ', p2.CalfCount), p2.PregnancyRecordId " +
        "FROM `PregnancyRecord` p2 WHERE p2.AnimalId = @AnimalId AND p2.IsActive = 1 AND p2.ActualDeliveryDate IS NOT NULL " +
        "UNION ALL SELECT s.SaleDate, 'Sale', CONCAT('Sold to ', s.BuyerName), CONCAT('Price: ', s.SalePrice), s.AnimalSaleId " +
        "FROM `AnimalSale` s WHERE s.AnimalId = @AnimalId AND s.IsActive = 1 " +
        "UNION ALL SELECT dt.DeathDate, 'Death', 'Died', CONCAT_WS(' | ', dt.CauseCategory, dt.Cause), dt.AnimalDeathId " +
        "FROM `AnimalDeath` dt WHERE dt.AnimalId = @AnimalId AND dt.IsActive = 1 " +
        "UNION ALL SELECT DATE(ar.StatusDate), 'Archive', 'Archived', ar.Remarks, ar.AnimalId " +
        "FROM `Animal` ar WHERE ar.AnimalId = @AnimalId AND ar.Status = 'Archived' AND ar.StatusDate IS NOT NULL" +
        ") e ORDER BY e.EventDate DESC, e.RefId DESC";

    private readonly IDbConnectionFactory _db;

    public AnimalRepository(IDbConnectionFactory db) => _db = db;

    public async Task<PagedResult<AnimalListDto>> GetPagedAsync(AnimalQuery query, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        var param = new
        {
            Search = query.SearchPattern,
            query.FarmerId,
            query.BreedId,
            Status = Clean(query.Status),
            AnimalType = Clean(query.AnimalType),
            MilkingStatus = Clean(query.MilkingStatus),
            PregnancyStatus = Clean(query.PregnancyStatus)
        };
        return await conn.QueryPagedAsync<AnimalListDto>(
            "SELECT " + ListColumns + From + Where,
            "SELECT COUNT(*)" + From + Where,
            param, query, SortColumns, "a.TagNumber ASC", ct);
    }

    public async Task<AnimalDetailDto?> GetByIdAsync(long id, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        var animal = await conn.QueryFirstOrNullAsync<AnimalDetailDto>(
            "SELECT " + DetailColumns + From + " WHERE a.AnimalId = @Id", new { Id = id }, ct);
        if (animal is null) return null;

        if (animal.Status == "Sold")
        {
            animal.Sale = await conn.QueryFirstOrNullAsync<AnimalSaleDto>(
                "SELECT s.AnimalSaleId AS Id, s.AnimalId, s.SaleDate, s.BuyerName, s.BuyerMobile, s.BuyerAddress, s.SalePrice, " +
                "s.PaymentModeId, s.ReferenceNo, s.Reason, s.Remarks FROM `AnimalSale` s " +
                "WHERE s.AnimalId = @Id AND s.IsActive = 1 ORDER BY s.AnimalSaleId DESC LIMIT 1", new { Id = id }, ct);
        }
        else if (animal.Status == "Dead")
        {
            animal.Death = await conn.QueryFirstOrNullAsync<AnimalDeathDto>(
                "SELECT d.AnimalDeathId AS Id, d.AnimalId, d.DeathDate, d.Cause, d.CauseCategory, d.VetName, d.PostMortemDone, " +
                "d.LossAmount, d.Remarks FROM `AnimalDeath` d " +
                "WHERE d.AnimalId = @Id AND d.IsActive = 1 ORDER BY d.AnimalDeathId DESC LIMIT 1", new { Id = id }, ct);
        }

        return animal;
    }

    public async Task<bool> TagExistsAsync(string tagNumber, long? excludeId, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        var count = await conn.ScalarAsync<int>(
            "SELECT COUNT(1) FROM `Animal` WHERE TagNumber = @Tag AND (@ExcludeId IS NULL OR AnimalId <> @ExcludeId)",
            new { Tag = tagNumber, ExcludeId = excludeId }, ct);
        return count > 0;
    }

    public async Task<long> CreateAsync(AnimalRequest request, DateTime registrationDate, long userId, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        return await conn.InsertAsync(
            "INSERT INTO `Animal` (TagNumber, FarmerId, BreedId, Name, AnimalType, Gender, DateOfBirth, Color, WeightKg, Source, " +
            "RegistrationDate, LactationNo, LastCalvingDate, MilkingStatus, PregnancyStatus, ExpectedDeliveryDate, Status, StatusDate, Remarks, CreatedBy) " +
            "VALUES (@TagNumber, @FarmerId, @BreedId, @Name, @AnimalType, @Gender, @DateOfBirth, @Color, @WeightKg, @Source, " +
            "@RegistrationDate, @LactationNo, @LastCalvingDate, @MilkingStatus, @PregnancyStatus, @ExpectedDeliveryDate, 'Active', NOW(), @Remarks, @UserId)",
            Params(0, request, registrationDate, userId), ct);
    }

    public async Task UpdateAsync(long id, AnimalRequest request, DateTime registrationDate, long userId, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        await conn.ExecAsync(
            "UPDATE `Animal` SET TagNumber = @TagNumber, FarmerId = @FarmerId, BreedId = @BreedId, Name = @Name, AnimalType = @AnimalType, " +
            "Gender = @Gender, DateOfBirth = @DateOfBirth, Color = @Color, WeightKg = @WeightKg, Source = @Source, " +
            "RegistrationDate = @RegistrationDate, LactationNo = @LactationNo, LastCalvingDate = @LastCalvingDate, " +
            "MilkingStatus = @MilkingStatus, PregnancyStatus = @PregnancyStatus, ExpectedDeliveryDate = @ExpectedDeliveryDate, " +
            "Remarks = @Remarks, ModifiedBy = @UserId WHERE AnimalId = @Id",
            Params(id, request, registrationDate, userId), ct);
    }

    public async Task SellAsync(long animalId, AnimalSaleRequest request, DateTime saleDate, long userId, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        await using var tx = await conn.BeginTransactionAsync(ct);
        try
        {
            await conn.ExecuteAsync(new CommandDefinition(
                "INSERT INTO `AnimalSale` (AnimalId, SaleDate, BuyerName, BuyerMobile, BuyerAddress, SalePrice, PaymentModeId, ReferenceNo, Reason, Remarks, CreatedBy) " +
                "VALUES (@AnimalId, @SaleDate, @BuyerName, @BuyerMobile, @BuyerAddress, @SalePrice, @PaymentModeId, @ReferenceNo, @Reason, @Remarks, @UserId)",
                new
                {
                    AnimalId = animalId,
                    SaleDate = saleDate.Date,
                    BuyerName = request.BuyerName.Trim(),
                    BuyerMobile = Clean(request.BuyerMobile),
                    BuyerAddress = Clean(request.BuyerAddress),
                    request.SalePrice,
                    request.PaymentModeId,
                    ReferenceNo = Clean(request.ReferenceNo),
                    Reason = Clean(request.Reason),
                    Remarks = Clean(request.Remarks),
                    UserId = userId
                }, tx, cancellationToken: ct));

            await conn.ExecuteAsync(new CommandDefinition(
                "UPDATE `Animal` SET Status = 'Sold', StatusDate = NOW(), ModifiedBy = @UserId WHERE AnimalId = @AnimalId",
                new { AnimalId = animalId, UserId = userId }, tx, cancellationToken: ct));

            await tx.CommitAsync(ct);
        }
        catch
        {
            await tx.RollbackAsync(ct);
            throw;
        }
    }

    public async Task RecordDeathAsync(long animalId, AnimalDeathRequest request, DateTime deathDate, long userId, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        await using var tx = await conn.BeginTransactionAsync(ct);
        try
        {
            await conn.ExecuteAsync(new CommandDefinition(
                "INSERT INTO `AnimalDeath` (AnimalId, DeathDate, Cause, CauseCategory, VetName, PostMortemDone, LossAmount, Remarks, CreatedBy) " +
                "VALUES (@AnimalId, @DeathDate, @Cause, @CauseCategory, @VetName, @PostMortemDone, @LossAmount, @Remarks, @UserId)",
                new
                {
                    AnimalId = animalId,
                    DeathDate = deathDate.Date,
                    Cause = Clean(request.Cause),
                    CauseCategory = request.CauseCategory.Trim(),
                    VetName = Clean(request.VetName),
                    request.PostMortemDone,
                    request.LossAmount,
                    Remarks = Clean(request.Remarks),
                    UserId = userId
                }, tx, cancellationToken: ct));

            await conn.ExecuteAsync(new CommandDefinition(
                "UPDATE `Animal` SET Status = 'Dead', StatusDate = NOW(), MilkingStatus = 'NotApplicable', PregnancyStatus = 'NotPregnant', " +
                "ExpectedDeliveryDate = NULL, ModifiedBy = @UserId WHERE AnimalId = @AnimalId",
                new { AnimalId = animalId, UserId = userId }, tx, cancellationToken: ct));

            await tx.CommitAsync(ct);
        }
        catch
        {
            await tx.RollbackAsync(ct);
            throw;
        }
    }

    public async Task ArchiveAsync(long animalId, string? reason, long userId, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        await conn.ExecAsync(
            "UPDATE `Animal` SET Status = 'Archived', StatusDate = NOW(), ModifiedBy = @UserId, " +
            "Remarks = CASE WHEN @Reason IS NULL THEN Remarks ELSE LEFT(CONCAT_WS(' | ', Remarks, CONCAT('Archived: ', @Reason)), 500) END " +
            "WHERE AnimalId = @AnimalId",
            new { AnimalId = animalId, Reason = Clean(reason), UserId = userId }, ct);
    }

    public async Task<PagedResult<AnimalDueDto>> GetDuePagedAsync(string type, AnimalDueQuery query, CancellationToken ct = default)
    {
        var inner = type switch
        {
            "vaccination" => VaccinationDueInner,
            "deworming" => DewormingDueInner,
            "pregnancy" => PregnancyDueInner,
            _ => throw new RequestValidationException("type", "Type must be vaccination, deworming or pregnancy.")
        };

        var fromSql =
            " FROM (" + inner + ") d JOIN `Animal` a ON a.AnimalId = d.AnimalId JOIN `Farmer` f ON f.FarmerId = a.FarmerId" +
            " WHERE a.IsActive = 1 AND a.Status IN ('Active', 'Sick')" +
            " AND d.DueDate <= DATE_ADD(CURDATE(), INTERVAL @Days DAY)" +
            " AND (@FarmerId IS NULL OR a.FarmerId = @FarmerId)" +
            " AND (@Search IS NULL OR a.TagNumber LIKE @Search OR a.Name LIKE @Search OR f.FullName LIKE @Search OR d.Title LIKE @Search)";

        await using var conn = await _db.OpenAsync(ct);
        var param = new
        {
            Days = Math.Clamp(query.Days, 0, 3650),
            query.FarmerId,
            Search = query.SearchPattern
        };
        return await conn.QueryPagedAsync<AnimalDueDto>(
            "SELECT d.AnimalId, a.TagNumber, a.Name AS AnimalName, a.FarmerId, f.FullName AS FarmerName, d.DueType, d.Title, d.DueDate, " +
            "DATEDIFF(d.DueDate, CURDATE()) AS DaysRemaining, d.RecordStatus AS Status, d.RefId" + fromSql,
            "SELECT COUNT(*)" + fromSql,
            param, query, DueSortColumns, "d.DueDate ASC, a.TagNumber ASC", ct);
    }

    public async Task<List<AnimalLifecycleEventDto>> GetLifecycleAsync(long animalId, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        return await conn.QueryListAsync<AnimalLifecycleEventDto>(LifecycleSql, new { AnimalId = animalId }, ct);
    }

    private static object Params(long id, AnimalRequest r, DateTime registrationDate, long userId)
    {
        var pregnant = r.PregnancyStatus == "Pregnant";
        return new
        {
            Id = id,
            TagNumber = r.TagNumber.Trim(),
            r.FarmerId,
            r.BreedId,
            Name = Clean(r.Name),
            AnimalType = r.AnimalType.Trim(),
            Gender = r.Gender.Trim(),
            r.DateOfBirth,
            Color = Clean(r.Color),
            r.WeightKg,
            Source = r.Source.Trim(),
            RegistrationDate = registrationDate.Date,
            r.LactationNo,
            r.LastCalvingDate,
            MilkingStatus = r.MilkingStatus.Trim(),
            PregnancyStatus = r.PregnancyStatus.Trim(),
            ExpectedDeliveryDate = pregnant ? r.ExpectedDeliveryDate : null,
            Remarks = Clean(r.Remarks),
            UserId = userId
        };
    }

    private static string? Clean(string? value)
    {
        var v = value?.Trim();
        return string.IsNullOrEmpty(v) ? null : v;
    }
}
