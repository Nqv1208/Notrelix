using Npgsql;

namespace Notrelix.Integration.Tests.Containers;

/// <summary>
/// Resets all data in the test PostgreSQL database by truncating all tables
/// across all 12 schemas. Uses a single TRUNCATE CASCADE statement.
/// </summary>
public sealed class DatabaseReset
{
    private readonly string _connectionString;

    public DatabaseReset(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task ResetAsync(CancellationToken ct = default)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync(ct);

        var tables = new List<(string Schema, string Name)>();
        await using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = """
                SELECT table_schema, table_name
                FROM information_schema.tables
                WHERE table_type = 'BASE TABLE'
                  AND table_schema NOT IN ('pg_catalog', 'information_schema')
                  AND table_name <> '__EFMigrationsHistory'
                ORDER BY table_schema, table_name
                """;

            await using var reader = await cmd.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
                tables.Add((reader.GetString(0), reader.GetString(1)));
        }

        if (tables.Count == 0)
            return;

        await using var truncateCmd = conn.CreateCommand();
        var tableNames = string.Join(", ",
            tables.Select(t => $"{QuoteIdentifier(t.Schema)}.{QuoteIdentifier(t.Name)}"));
        truncateCmd.CommandText = $"TRUNCATE TABLE {tableNames} CASCADE";
        await truncateCmd.ExecuteNonQueryAsync(ct);
    }

    private static string QuoteIdentifier(string identifier)
        => $"\"{identifier.Replace("\"", "\"\"")}\"";
}
