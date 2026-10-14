namespace Dairy.Application.Interfaces;

/// <summary>Writes AuditLog rows. Implementations never throw.</summary>
public interface IAuditService
{
    /// <summary>Logs as the current authenticated user (IP / user agent taken from the request).</summary>
    Task LogAsync(string action, string entity, object? entityId, object? oldValues, object? newValues, CancellationToken ct = default);

    /// <summary>Logs for an explicit user (e.g. login, where the caller is not yet authenticated).</summary>
    Task LogForUserAsync(long? userId, string? username, string action, string entity, object? entityId, object? oldValues, object? newValues, CancellationToken ct = default);
}
