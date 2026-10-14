using MySqlConnector;

namespace Dairy.Application.Interfaces;

public interface IDbConnectionFactory
{
    /// <summary>Opens a new MySQL connection. Caller disposes: <c>await using var conn = await _db.OpenAsync(ct);</c></summary>
    Task<MySqlConnection> OpenAsync(CancellationToken ct = default);
}
