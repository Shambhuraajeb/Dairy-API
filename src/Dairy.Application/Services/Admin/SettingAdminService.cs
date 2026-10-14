using System.Globalization;
using System.Text.Json;
using Dairy.Application.Common;
using Dairy.Application.DTOs.Admin;
using Dairy.Application.Interfaces;
using Dairy.Application.Interfaces.Admin;
using Dairy.Application.Requests.Admin;
using Dairy.Domain.Constants;

namespace Dairy.Application.Services.Admin;

public class SettingAdminService : ISettingAdminService
{
    private readonly ISettingAdminRepository _repo;
    private readonly IAuditService _audit;
    private readonly ICurrentUser _user;

    public SettingAdminService(ISettingAdminRepository repo, IAuditService audit, ICurrentUser user)
    {
        _repo = repo;
        _audit = audit;
        _user = user;
    }

    public Task<List<SettingDto>> GetListAsync(SettingQuery query, CancellationToken ct = default) =>
        _repo.GetListAsync(query, ct);

    public async Task<SettingDto> UpdateAsync(string key, UpdateSettingRequest request, CancellationToken ct = default)
    {
        var existing = await _repo.GetByKeyAsync(key.Trim(), ct) ?? throw new NotFoundException($"Setting '{key}' was not found.");
        var value = NormalizeValue(existing, request.Value ?? string.Empty, "value");
        return await ApplyAsync(existing, value, ct);
    }

    public async Task<List<SettingDto>> BulkUpdateAsync(BulkUpdateSettingsRequest request, CancellationToken ct = default)
    {
        // Validate everything first so a bad item changes nothing.
        var plan = new List<(SettingDto Existing, string Value)>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < request.Items.Count; i++)
        {
            var item = request.Items[i];
            var key = item.Key.Trim();
            if (!seen.Add(key))
                throw new RequestValidationException($"items[{i}].key", $"Setting '{key}' appears more than once.");

            var existing = await _repo.GetByKeyAsync(key, ct)
                           ?? throw new RequestValidationException($"items[{i}].key", $"Setting '{key}' was not found.");
            plan.Add((existing, NormalizeValue(existing, item.Value ?? string.Empty, $"items[{i}].value")));
        }

        var results = new List<SettingDto>();
        foreach (var (existing, value) in plan)
            results.Add(await ApplyAsync(existing, value, ct));
        return results;
    }

    private async Task<SettingDto> ApplyAsync(SettingDto existing, string value, CancellationToken ct)
    {
        if (string.Equals(existing.Value ?? string.Empty, value, StringComparison.Ordinal))
            return existing;

        await _repo.UpdateValueAsync(existing.Key, value, _user.UserId, ct);
        var updated = await _repo.GetByKeyAsync(existing.Key, ct) ?? existing;
        await _audit.LogAsync(AuditActions.SettingsChange, "SystemSetting", existing.Key,
            new { existing.Key, Value = existing.Value }, new { updated.Key, Value = updated.Value }, ct);
        return updated;
    }

    private static string NormalizeValue(SettingDto setting, string value, string field)
    {
        var text = value.Trim();
        switch ((setting.DataType ?? "string").ToLowerInvariant())
        {
            case "number":
                if (!decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out _))
                    throw new RequestValidationException(field, $"'{setting.Key}' must be a number.");
                return text;
            case "bool":
                if (text.Equals("true", StringComparison.OrdinalIgnoreCase)) return "true";
                if (text.Equals("false", StringComparison.OrdinalIgnoreCase)) return "false";
                throw new RequestValidationException(field, $"'{setting.Key}' must be true or false.");
            case "json":
                try
                {
                    using var _ = JsonDocument.Parse(text);
                }
                catch (JsonException)
                {
                    throw new RequestValidationException(field, $"'{setting.Key}' must be valid JSON.");
                }
                return text;
            default:
                return value;
        }
    }
}
