using Dairy.Application.Common;
using Dairy.Application.DTOs.Animals;
using Dairy.Application.Interfaces;
using Dairy.Application.Interfaces.Animals;
using Dairy.Application.Requests.Animals;

namespace Dairy.Infrastructure.Data.Repositories.Animals;

public class BreedRepository : IBreedRepository
{
    private static readonly Dictionary<string, string> SortColumns = new(StringComparer.OrdinalIgnoreCase)
    {
        ["id"] = "b.BreedId",
        ["code"] = "b.Code",
        ["name"] = "b.Name",
        ["animalType"] = "b.AnimalType",
        ["averageDailyYield"] = "b.AverageDailyYield",
        ["isActive"] = "b.IsActive"
    };

    private const string Select =
        "SELECT b.BreedId AS Id, b.Code, b.Name, b.AnimalType, b.AverageDailyYield, b.Description, b.IsActive FROM `Breed` b";

    private const string Where =
        " WHERE (@Search IS NULL OR b.Code LIKE @Search OR b.Name LIKE @Search)" +
        " AND (@AnimalType IS NULL OR b.AnimalType = @AnimalType)" +
        " AND (@IsActive IS NULL OR b.IsActive = @IsActive)";

    private readonly IDbConnectionFactory _db;

    public BreedRepository(IDbConnectionFactory db) => _db = db;

    public async Task<PagedResult<BreedDto>> GetPagedAsync(BreedQuery query, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        var param = new
        {
            Search = query.SearchPattern,
            AnimalType = string.IsNullOrWhiteSpace(query.AnimalType) ? null : query.AnimalType.Trim(),
            query.IsActive
        };
        return await conn.QueryPagedAsync<BreedDto>(
            Select + Where,
            "SELECT COUNT(*) FROM `Breed` b" + Where,
            param, query, SortColumns, "b.Name ASC", ct);
    }

    public async Task<BreedDto?> GetByIdAsync(long id, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        return await conn.QueryFirstOrNullAsync<BreedDto>(Select + " WHERE b.BreedId = @Id", new { Id = id }, ct);
    }

    public async Task<bool> CodeOrNameExistsAsync(string code, string name, long? excludeId, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        var count = await conn.ScalarAsync<int>(
            "SELECT COUNT(1) FROM `Breed` WHERE (Code = @Code OR Name = @Name) AND (@ExcludeId IS NULL OR BreedId <> @ExcludeId)",
            new { Code = code, Name = name, ExcludeId = excludeId }, ct);
        return count > 0;
    }

    public async Task<long> CreateAsync(CreateBreedRequest request, long userId, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        return await conn.InsertAsync(
            "INSERT INTO `Breed` (Code, Name, AnimalType, AverageDailyYield, Description, IsActive, CreatedBy) " +
            "VALUES (@Code, @Name, @AnimalType, @AverageDailyYield, @Description, 1, @UserId)",
            new
            {
                Code = request.Code.Trim(),
                Name = request.Name.Trim(),
                AnimalType = request.AnimalType.Trim(),
                request.AverageDailyYield,
                Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
                UserId = userId
            }, ct);
    }

    public async Task UpdateAsync(long id, UpdateBreedRequest request, bool isActive, long userId, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        await conn.ExecAsync(
            "UPDATE `Breed` SET Code = @Code, Name = @Name, AnimalType = @AnimalType, AverageDailyYield = @AverageDailyYield, " +
            "Description = @Description, IsActive = @IsActive, ModifiedBy = @UserId WHERE BreedId = @Id",
            new
            {
                Id = id,
                Code = request.Code.Trim(),
                Name = request.Name.Trim(),
                AnimalType = request.AnimalType.Trim(),
                request.AverageDailyYield,
                Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
                IsActive = isActive,
                UserId = userId
            }, ct);
    }
}
