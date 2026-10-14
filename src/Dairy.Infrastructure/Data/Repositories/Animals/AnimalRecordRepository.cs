using Dairy.Application.Common;
using Dairy.Application.Interfaces;
using Dairy.Application.Interfaces.Animals;
using Dairy.Application.Requests.Animals;
using Dapper;

namespace Dairy.Infrastructure.Data.Repositories.Animals;

/// <summary>
/// One generic engine for the six per-animal record tables. The kind maps to a fixed table definition;
/// request property names equal the column names and are bound as SQL parameters (never concatenated).
/// </summary>
public class AnimalRecordRepository : IAnimalRecordRepository
{
    private sealed record Def(string Table, string Pk, string DateCol, string[] Columns, bool HardDelete = false, bool StampsRecordedBy = false);

    private sealed record Built(Def Def, string Select, string Insert, string Update, IReadOnlyDictionary<string, string> Sort);

    private static readonly Dictionary<string, Def> Defs = new()
    {
        [AnimalRecordKinds.Health] = new("AnimalHealthRecord", "AnimalHealthRecordId", "RecordDate", new[]
        {
            "RecordDate", "HealthCondition", "Symptoms", "Diagnosis", "Treatment", "Medicines", "Severity", "VetName",
            "VetEmployeeId", "Cost", "Status", "FollowUpDate", "RecoveredDate", "Remarks"
        }),
        [AnimalRecordKinds.Vaccination] = new("Vaccination", "VaccinationId", "VaccinationDate", new[]
        {
            "VaccineName", "VaccinationDate", "NextDueDate", "DoseNumber", "BatchNumber", "AdministeredBy", "VetEmployeeId",
            "Cost", "Status", "Remarks"
        }),
        [AnimalRecordKinds.Deworming] = new("Deworming", "DewormingId", "DewormingDate", new[]
        {
            "DewormingDate", "MedicineName", "Dosage", "NextDueDate", "AdministeredBy", "Cost", "Status", "Remarks"
        }),
        [AnimalRecordKinds.Breeding] = new("BreedingRecord", "BreedingRecordId", "BreedingDate", new[]
        {
            "BreedingDate", "BreedingMethod", "BullName", "SemenCode", "TechnicianName", "HeatDate", "ExpectedDeliveryDate",
            "Result", "ConfirmedDate", "Cost", "Remarks"
        }),
        [AnimalRecordKinds.Pregnancy] = new("PregnancyRecord", "PregnancyRecordId", "ConfirmedDate", new[]
        {
            "BreedingRecordId", "ConfirmedDate", "ExpectedDeliveryDate", "ActualDeliveryDate", "Status", "DeliveryOutcome",
            "CalfCount", "CalfAnimalId", "CalfGender", "LastCheckupDate", "Remarks"
        }),
        // Unique (AnimalId, ProductionDate, ShiftName): hard delete so a corrected entry can be re-added.
        [AnimalRecordKinds.MilkProduction] = new("AnimalMilkProduction", "AnimalMilkProductionId", "ProductionDate", new[]
        {
            "ProductionDate", "ShiftName", "QuantityLiters", "Fat", "Snf", "Remarks"
        }, HardDelete: true, StampsRecordedBy: true)
    };

    private static readonly Dictionary<string, Built> Sql = Defs.ToDictionary(kv => kv.Key, kv => Build(kv.Value));

    private readonly IDbConnectionFactory _db;

    public AnimalRecordRepository(IDbConnectionFactory db) => _db = db;

    public async Task<PagedResult<T>> GetPagedAsync<T>(string kind, long animalId, AnimalRecordQuery query, CancellationToken ct = default)
    {
        var b = Get(kind);
        const string where =
            " WHERE t.AnimalId = @AnimalId AND t.IsActive = 1" +
            " AND (@DateFrom IS NULL OR t.{0} >= @DateFrom) AND (@DateTo IS NULL OR t.{0} <= @DateTo)";
        var whereSql = string.Format(where, b.Def.DateCol);

        await using var conn = await _db.OpenAsync(ct);
        var param = new { AnimalId = animalId, DateFrom = query.DateFrom?.Date, DateTo = query.DateTo?.Date };
        return await conn.QueryPagedAsync<T>(
            b.Select + whereSql,
            $"SELECT COUNT(*) FROM `{b.Def.Table}` t" + whereSql,
            param, query, b.Sort, $"t.{b.Def.DateCol} DESC, t.{b.Def.Pk} DESC", ct);
    }

    public async Task<T?> GetByIdAsync<T>(string kind, long animalId, long id, CancellationToken ct = default)
    {
        var b = Get(kind);
        await using var conn = await _db.OpenAsync(ct);
        return await conn.QueryFirstOrNullAsync<T>(
            b.Select + $" WHERE t.{b.Def.Pk} = @Id AND t.AnimalId = @AnimalId AND t.IsActive = 1",
            new { Id = id, AnimalId = animalId }, ct);
    }

    public async Task<long> InsertAsync(string kind, long animalId, object request, long userId, CancellationToken ct = default)
    {
        var b = Get(kind);
        var p = new DynamicParameters(request);
        p.Add("AnimalId", animalId);
        p.Add("UserId", userId);

        await using var conn = await _db.OpenAsync(ct);
        return await conn.InsertAsync(b.Insert, p, ct);
    }

    public async Task UpdateAsync(string kind, long animalId, long id, object request, long userId, CancellationToken ct = default)
    {
        var b = Get(kind);
        var p = new DynamicParameters(request);
        p.Add("AnimalId", animalId);
        p.Add("Id", id);
        p.Add("UserId", userId);

        await using var conn = await _db.OpenAsync(ct);
        await conn.ExecAsync(b.Update, p, ct);
    }

    public async Task<bool> DeleteAsync(string kind, long animalId, long id, long userId, CancellationToken ct = default)
    {
        var b = Get(kind);
        var sql = b.Def.HardDelete
            ? $"DELETE FROM `{b.Def.Table}` WHERE {b.Def.Pk} = @Id AND AnimalId = @AnimalId"
            : $"UPDATE `{b.Def.Table}` SET IsActive = 0, ModifiedBy = @UserId WHERE {b.Def.Pk} = @Id AND AnimalId = @AnimalId AND IsActive = 1";

        await using var conn = await _db.OpenAsync(ct);
        return await conn.ExecAsync(sql, new { Id = id, AnimalId = animalId, UserId = userId }, ct) > 0;
    }

    public async Task SyncSickStatusAsync(long animalId, CancellationToken ct = default)
    {
        const string sickCase =
            "CASE WHEN EXISTS (SELECT 1 FROM `AnimalHealthRecord` h WHERE h.AnimalId = a.AnimalId AND h.IsActive = 1 " +
            "AND h.Status IN ('Open', 'Ongoing')) THEN 'Sick' ELSE 'Active' END";

        await using var conn = await _db.OpenAsync(ct);
        await conn.ExecAsync(
            "UPDATE `Animal` a SET a.StatusDate = IF(a.Status <> (" + sickCase + "), NOW(), a.StatusDate), a.Status = " + sickCase +
            " WHERE a.AnimalId = @AnimalId AND a.Status IN ('Active', 'Sick')",
            new { AnimalId = animalId }, ct);
    }

    public async Task SyncPregnancyStatusAsync(long animalId, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        await conn.ExecAsync(
            "UPDATE `Animal` a SET " +
            "a.PregnancyStatus = CASE WHEN EXISTS (SELECT 1 FROM `PregnancyRecord` p WHERE p.AnimalId = a.AnimalId AND p.IsActive = 1 AND p.Status = 'Pregnant') " +
            "THEN 'Pregnant' ELSE 'NotPregnant' END, " +
            "a.ExpectedDeliveryDate = (SELECT MAX(p2.ExpectedDeliveryDate) FROM `PregnancyRecord` p2 WHERE p2.AnimalId = a.AnimalId AND p2.IsActive = 1 AND p2.Status = 'Pregnant') " +
            "WHERE a.AnimalId = @AnimalId AND a.Status IN ('Active', 'Sick')",
            new { AnimalId = animalId }, ct);
    }

    public async Task ApplyDeliveryAsync(long animalId, DateTime deliveryDate, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        await conn.ExecAsync(
            "UPDATE `Animal` SET LactationNo = LactationNo + 1, LastCalvingDate = @DeliveryDate, MilkingStatus = 'Milking' " +
            "WHERE AnimalId = @AnimalId AND Status IN ('Active', 'Sick')",
            new { AnimalId = animalId, DeliveryDate = deliveryDate.Date }, ct);
    }

    private static Built Get(string kind) =>
        Sql.TryGetValue(kind, out var built) ? built : throw new ArgumentException($"Unknown animal record kind '{kind}'.", nameof(kind));

    private static Built Build(Def d)
    {
        var selectCols = string.Join(", ", d.Columns.Select(c => "t." + c));
        var select = $"SELECT t.{d.Pk} AS Id, t.AnimalId, {selectCols} FROM `{d.Table}` t";

        var insertCols = "AnimalId, " + string.Join(", ", d.Columns) + (d.StampsRecordedBy ? ", RecordedBy" : string.Empty) + ", CreatedBy";
        var insertVals = "@AnimalId, " + string.Join(", ", d.Columns.Select(c => "@" + c)) + (d.StampsRecordedBy ? ", @UserId" : string.Empty) + ", @UserId";
        var insert = $"INSERT INTO `{d.Table}` ({insertCols}) VALUES ({insertVals})";

        var sets = string.Join(", ", d.Columns.Select(c => $"{c} = @{c}"));
        var update = $"UPDATE `{d.Table}` SET {sets}, ModifiedBy = @UserId WHERE {d.Pk} = @Id AND AnimalId = @AnimalId AND IsActive = 1";

        var sort = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["id"] = "t." + d.Pk,
            ["date"] = "t." + d.DateCol
        };
        return new Built(d, select, insert, update, sort);
    }
}
