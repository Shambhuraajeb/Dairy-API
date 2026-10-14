using System.Text.Json;
using System.Text.Json.Serialization;
using Dairy.Application.Interfaces;
using Dapper;
using Microsoft.Extensions.Logging;

namespace Dairy.Infrastructure.Security;

public class AuditService : IAuditService
{
    private const string InsertSql =
        "INSERT INTO `AuditLog` (UserId, Username, Action, EntityName, EntityId, OldValues, NewValues, IpAddress, UserAgent, CreatedDate) " +
        "VALUES (@UserId, @Username, @Action, @EntityName, @EntityId, @OldValues, @NewValues, @IpAddress, @UserAgent, NOW())";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        ReferenceHandler = ReferenceHandler.IgnoreCycles,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly IDbConnectionFactory _db;
    private readonly ICurrentUser _user;
    private readonly ILogger<AuditService> _logger;

    public AuditService(IDbConnectionFactory db, ICurrentUser user, ILogger<AuditService> logger)
    {
        _db = db;
        _user = user;
        _logger = logger;
    }

    public Task LogAsync(string action, string entity, object? entityId, object? oldValues, object? newValues, CancellationToken ct = default)
    {
        long? userId = _user.IsAuthenticated ? _user.UserId : null;
        string? username = _user.IsAuthenticated ? _user.Username : null;
        return LogForUserAsync(userId, username, action, entity, entityId, oldValues, newValues, ct);
    }

    public async Task LogForUserAsync(long? userId, string? username, string action, string entity, object? entityId, object? oldValues, object? newValues, CancellationToken ct = default)
    {
        try
        {
            // Deliberately not tied to the request token: the audit row must survive client disconnects.
            await using var conn = await _db.OpenAsync(CancellationToken.None);
            await conn.ExecuteAsync(new CommandDefinition(InsertSql, new
            {
                UserId = userId,
                Username = Truncate(username, 100),
                Action = Truncate(action, 100),
                EntityName = Truncate(entity, 100),
                EntityId = Truncate(entityId?.ToString(), 64),
                OldValues = Serialize(oldValues),
                NewValues = Serialize(newValues),
                IpAddress = Truncate(_user.IpAddress, 64),
                UserAgent = Truncate(_user.UserAgent, 255)
            }));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to write audit log entry {Action} {Entity} {EntityId}", action, entity, entityId);
        }
    }

    private static string? Serialize(object? value)
    {
        if (value is null) return null;
        try
        {
            return JsonSerializer.Serialize(value, JsonOptions);
        }
        catch
        {
            return null;
        }
    }

    private static string? Truncate(string? value, int max) =>
        value is null ? null : (value.Length <= max ? value : value.Substring(0, max));
}
