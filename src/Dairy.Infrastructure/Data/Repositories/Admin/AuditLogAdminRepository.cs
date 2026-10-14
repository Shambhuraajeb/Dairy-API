using Dairy.Application.Common;
using Dairy.Application.DTOs.Admin;
using Dairy.Application.Interfaces;
using Dairy.Application.Interfaces.Admin;
using Dairy.Application.Requests.Admin;

namespace Dairy.Infrastructure.Data.Repositories.Admin;

public class AuditLogAdminRepository : IAuditLogAdminRepository
{
    private static readonly Dictionary<string, string> SortColumns = new(StringComparer.OrdinalIgnoreCase)
    {
        ["id"] = "a.AuditLogId",
        ["createdDate"] = "a.CreatedDate",
        ["username"] = "a.Username",
        ["action"] = "a.Action",
        ["entityName"] = "a.EntityName"
    };

    private const string Where =
        " WHERE (@FromDate IS NULL OR a.CreatedDate >= @FromDate)" +
        " AND (@ToDate IS NULL OR a.CreatedDate < DATE_ADD(@ToDate, INTERVAL 1 DAY))" +
        " AND (@UserLike IS NULL OR a.Username LIKE @UserLike)" +
        " AND (@UserId IS NULL OR a.UserId = @UserId)" +
        " AND (@ActionName IS NULL OR a.Action = @ActionName)" +
        " AND (@Entity IS NULL OR a.EntityName = @Entity)" +
        " AND (@EntityId IS NULL OR a.EntityId = @EntityId)" +
        " AND (@Search IS NULL OR a.Username LIKE @Search OR a.Action LIKE @Search OR a.EntityName LIKE @Search OR a.EntityId LIKE @Search)";

    private readonly IDbConnectionFactory _db;

    public AuditLogAdminRepository(IDbConnectionFactory db) => _db = db;

    public async Task<PagedResult<AuditLogListDto>> GetPagedAsync(AuditLogQuery query, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        var param = new
        {
            FromDate = query.From?.Date,
            ToDate = query.To?.Date,
            UserLike = LikeOrNull(query.User),
            query.UserId,
            ActionName = Clean(query.Action),
            Entity = Clean(query.Entity),
            EntityId = Clean(query.EntityId),
            Search = query.SearchPattern
        };
        return await conn.QueryPagedAsync<AuditLogListDto>(
            "SELECT a.AuditLogId AS Id, a.UserId, a.Username, a.Action, a.EntityName, a.EntityId, a.IpAddress, a.CreatedDate " +
            "FROM `AuditLog` a" + Where,
            "SELECT COUNT(*) FROM `AuditLog` a" + Where,
            param, query, SortColumns, "a.CreatedDate DESC, a.AuditLogId DESC", ct);
    }

    public async Task<AuditLogDto?> GetByIdAsync(long id, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        return await conn.QueryFirstOrNullAsync<AuditLogDto>(
            "SELECT a.AuditLogId AS Id, a.UserId, a.Username, a.Action, a.EntityName, a.EntityId, a.IpAddress, a.CreatedDate, " +
            "a.OldValues, a.NewValues, a.UserAgent FROM `AuditLog` a WHERE a.AuditLogId = @Id",
            new { Id = id }, ct);
    }

    public async Task<AuditFilterOptionsDto> GetFilterOptionsAsync(CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        return new AuditFilterOptionsDto
        {
            Actions = await conn.QueryListAsync<string>("SELECT DISTINCT Action FROM `AuditLog` ORDER BY Action", null, ct),
            Entities = await conn.QueryListAsync<string>("SELECT DISTINCT EntityName FROM `AuditLog` ORDER BY EntityName", null, ct)
        };
    }

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string? LikeOrNull(string? value)
    {
        var s = Clean(value);
        if (s == null) return null;
        return "%" + s.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_") + "%";
    }
}
