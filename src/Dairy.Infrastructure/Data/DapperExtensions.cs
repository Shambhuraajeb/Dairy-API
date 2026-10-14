using System.Data;
using Dairy.Application.Common;
using Dapper;

namespace Dairy.Infrastructure.Data;

/// <summary>
/// Convenience helpers over Dapper. All take a connection from IDbConnectionFactory.OpenAsync and
/// honour CancellationToken. SQL is parameterized; sort columns come only from a whitelist.
/// </summary>
public static class DapperExtensions
{
    // ---------- inline SQL ----------

    public static async Task<List<T>> QueryListAsync<T>(this IDbConnection conn, string sql, object? param = null, CancellationToken ct = default)
    {
        var rows = await conn.QueryAsync<T>(new CommandDefinition(sql, param, cancellationToken: ct));
        return rows.ToList();
    }

    public static Task<T?> QueryFirstOrNullAsync<T>(this IDbConnection conn, string sql, object? param = null, CancellationToken ct = default) =>
        conn.QueryFirstOrDefaultAsync<T>(new CommandDefinition(sql, param, cancellationToken: ct));

    public static Task<T?> ScalarAsync<T>(this IDbConnection conn, string sql, object? param = null, CancellationToken ct = default) =>
        conn.ExecuteScalarAsync<T>(new CommandDefinition(sql, param, cancellationToken: ct));

    /// <summary>INSERT/UPDATE/DELETE; returns affected rows.</summary>
    public static Task<int> ExecAsync(this IDbConnection conn, string sql, object? param = null, CancellationToken ct = default) =>
        conn.ExecuteAsync(new CommandDefinition(sql, param, cancellationToken: ct));

    /// <summary>INSERT then returns LAST_INSERT_ID().</summary>
    public static async Task<long> InsertAsync(this IDbConnection conn, string insertSql, object? param = null, CancellationToken ct = default)
    {
        var sql = insertSql.TrimEnd().TrimEnd(';') + "; SELECT LAST_INSERT_ID();";
        return await conn.ExecuteScalarAsync<long>(new CommandDefinition(sql, param, cancellationToken: ct));
    }

    /// <summary>
    /// Server-side paging. <paramref name="selectSql"/> must NOT contain ORDER BY / LIMIT; it is appended here.
    /// <paramref name="countSql"/> is a SELECT COUNT(*) with the same FROM/WHERE. Both use the same <paramref name="param"/>.
    /// <paramref name="sortWhitelist"/> maps API sort keys (camelCase, case-insensitive) to trusted SQL column expressions.
    /// <paramref name="defaultSort"/> is a trusted ORDER BY fragment (e.g. "u.Name ASC") used when SortBy is empty/unknown.
    /// </summary>
    public static async Task<PagedResult<T>> QueryPagedAsync<T>(
        this IDbConnection conn,
        string selectSql,
        string countSql,
        object? param,
        PagedQuery query,
        IReadOnlyDictionary<string, string> sortWhitelist,
        string defaultSort,
        CancellationToken ct = default)
    {
        var orderBy = BuildOrderBy(query, sortWhitelist, defaultSort);
        var pagedSql = $"{selectSql} ORDER BY {orderBy} LIMIT {query.PageSize} OFFSET {query.Offset}";

        var total = await conn.ExecuteScalarAsync<long>(new CommandDefinition(countSql, param, cancellationToken: ct));
        if (total == 0 || query.Offset >= total)
            return new PagedResult<T>(new List<T>(), total, query.Page, query.PageSize);

        var items = await conn.QueryAsync<T>(new CommandDefinition(pagedSql, param, cancellationToken: ct));
        return new PagedResult<T>(items.ToList(), total, query.Page, query.PageSize);
    }

    private static string BuildOrderBy(PagedQuery query, IReadOnlyDictionary<string, string> whitelist, string defaultSort)
    {
        if (!string.IsNullOrWhiteSpace(query.SortBy))
        {
            foreach (var kv in whitelist)
            {
                if (string.Equals(kv.Key, query.SortBy.Trim(), StringComparison.OrdinalIgnoreCase))
                    return $"{kv.Value} {(query.Descending ? "DESC" : "ASC")}";
            }
        }
        return defaultSort;
    }

    // ---------- stored procedures (parameters named p_<Name>: pass new { p_FarmerId = 1, ... }) ----------

    public static async Task<List<T>> QuerySpAsync<T>(this IDbConnection conn, string procedure, object? param = null, CancellationToken ct = default)
    {
        var rows = await conn.QueryAsync<T>(Sp(procedure, param, ct));
        return rows.ToList();
    }

    public static Task<T?> QuerySpFirstOrDefaultAsync<T>(this IDbConnection conn, string procedure, object? param = null, CancellationToken ct = default) =>
        conn.QueryFirstOrDefaultAsync<T>(Sp(procedure, param, ct));

    /// <summary>Procedure with several result sets; dispose the returned reader.</summary>
    public static Task<SqlMapper.GridReader> QueryMultipleSpAsync(this IDbConnection conn, string procedure, object? param = null, CancellationToken ct = default) =>
        conn.QueryMultipleAsync(Sp(procedure, param, ct));

    /// <summary>Procedure that returns no result set (or whose result you ignore).</summary>
    public static Task<int> ExecuteSpAsync(this IDbConnection conn, string procedure, object? param = null, CancellationToken ct = default) =>
        conn.ExecuteAsync(Sp(procedure, param, ct));

    /// <summary>Procedure that returns a single value as first column of first row.</summary>
    public static Task<T?> ExecuteSpScalarAsync<T>(this IDbConnection conn, string procedure, object? param = null, CancellationToken ct = default) =>
        conn.ExecuteScalarAsync<T>(Sp(procedure, param, ct));

    /// <summary>Creating procedure that returns one row with columns Id (and optionally Number).</summary>
    public static async Task<CreatedResult> QueryCreatedSpAsync(this IDbConnection conn, string procedure, object? param = null, CancellationToken ct = default)
    {
        var row = await conn.QueryFirstOrDefaultAsync<CreatedResult>(Sp(procedure, param, ct));
        return row ?? throw new InvalidOperationException($"Stored procedure {procedure} did not return a row.");
    }

    private static CommandDefinition Sp(string procedure, object? param, CancellationToken ct) =>
        new(procedure, param, commandType: CommandType.StoredProcedure, cancellationToken: ct);
}
