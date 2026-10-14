using Dairy.Application.Interfaces;
using Dapper;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Dairy.Api.Extensions;

public class DatabaseHealthCheck : IHealthCheck
{
    private readonly IDbConnectionFactory _db;

    public DatabaseHealthCheck(IDbConnectionFactory db) => _db = db;

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            await using var conn = await _db.OpenAsync(cancellationToken);
            await conn.ExecuteScalarAsync<int>(new CommandDefinition("SELECT 1", cancellationToken: cancellationToken));
            return HealthCheckResult.Healthy("Database reachable.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Database unreachable.", ex);
        }
    }
}
