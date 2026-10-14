using System.Data;
using MySqlConnector;

namespace Dairy.Infrastructure.Data;

/// <summary>Generates the next human code (PREFIX + zero padded number) and inserts with retry on a unique-key race. Table/column/prefix are trusted constants.</summary>
internal static class AutoCode
{
    public static async Task<(long Id, string Code)> InsertAsync(
        IDbConnection conn,
        string table,
        string column,
        string prefix,
        int width,
        string? explicitCode,
        Func<string, Task<long>> insert,
        CancellationToken ct)
    {
        for (var attempt = 0; ; attempt++)
        {
            var code = explicitCode;
            if (code is null)
            {
                var sql = "SELECT COALESCE(MAX(CAST(SUBSTRING(" + column + ", " + (prefix.Length + 1) + ") AS UNSIGNED)), 0) + 1 FROM `" + table +
                          "` WHERE " + column + " REGEXP '^" + prefix + "[0-9]+$'";
                var next = await conn.ScalarAsync<long>(sql, null, ct);
                code = prefix + next.ToString().PadLeft(width, '0');
            }

            try
            {
                var id = await insert(code);
                return (id, code);
            }
            catch (MySqlException ex) when (ex.ErrorCode == MySqlErrorCode.DuplicateKeyEntry && explicitCode is null && attempt < 4)
            {
                // someone else took the code; recompute and retry
            }
        }
    }
}
