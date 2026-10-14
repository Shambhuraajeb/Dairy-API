using Dairy.Application.DTOs.Admin;
using Dairy.Application.Interfaces;
using Dairy.Application.Interfaces.Admin;
using Dairy.Application.Requests.Admin;

namespace Dairy.Infrastructure.Data.Repositories.Admin;

public class SettingAdminRepository : ISettingAdminRepository
{
    private const string Select =
        "SELECT s.SystemSettingId AS Id, s.SettingKey AS `Key`, s.SettingValue AS `Value`, s.Description, s.SettingGroup AS `Group`, " +
        "s.DataType, s.IsActive, s.ModifiedDate FROM `SystemSetting` s";

    private readonly IDbConnectionFactory _db;

    public SettingAdminRepository(IDbConnectionFactory db) => _db = db;

    public async Task<List<SettingDto>> GetListAsync(SettingQuery query, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        var search = string.IsNullOrWhiteSpace(query.Search) ? null : "%" + EscapeLike(query.Search.Trim()) + "%";
        var group = string.IsNullOrWhiteSpace(query.Group) ? null : query.Group.Trim();
        return await conn.QueryListAsync<SettingDto>(
            Select +
            " WHERE s.IsActive = 1" +
            " AND (@Group IS NULL OR s.SettingGroup = @Group)" +
            " AND (@Search IS NULL OR s.SettingKey LIKE @Search OR s.Description LIKE @Search)" +
            " ORDER BY s.SettingGroup, s.SettingKey",
            new { Group = group, Search = search }, ct);
    }

    public async Task<SettingDto?> GetByKeyAsync(string key, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        return await conn.QueryFirstOrNullAsync<SettingDto>(
            Select + " WHERE s.SettingKey = @Key AND s.IsActive = 1", new { Key = key }, ct);
    }

    public async Task UpdateValueAsync(string key, string? value, long actorId, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        await conn.ExecAsync(
            "UPDATE `SystemSetting` SET SettingValue = @Value, ModifiedBy = @ActorId WHERE SettingKey = @Key",
            new { Key = key, Value = value, ActorId = actorId }, ct);
    }

    private static string EscapeLike(string s) => s.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_");
}
