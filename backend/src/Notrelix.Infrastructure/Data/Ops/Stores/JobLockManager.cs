using System.Data;

namespace Notrelix.Infrastructure.Data.Ops.Stores;

public sealed class JobLockManager : IJobLockManager
{
    private readonly IDbConnection _connection;

    public JobLockManager(IDbConnection connection)
    {
        _connection = connection;
    }

    public async Task<long?> AcquireAsync(string lockKey, string lockedBy, TimeSpan duration, CancellationToken ct = default)
    {
        const string sql = """
            INSERT INTO ops.job_locks (id, lock_key, locked_by, fencing_token, locked_until, metadata_json, acquired_at, created_at)
            VALUES (gen_random_uuid(), @key, @by, 1, @until, '{}', NOW(), NOW())
            ON CONFLICT (lock_key) DO UPDATE SET
                fencing_token = ops.job_locks.fencing_token + 1,
                locked_by = @by,
                locked_until = @until,
                renewed_at = NOW(),
                updated_at = NOW()
            WHERE ops.job_locks.locked_until < NOW()
            RETURNING fencing_token
            """;

        await using var cmd = new NpgsqlCommand(sql, (NpgsqlConnection)_connection);
        cmd.Parameters.AddWithValue("key", lockKey);
        cmd.Parameters.AddWithValue("by", lockedBy);
        cmd.Parameters.AddWithValue("until", DateTimeOffset.UtcNow.Add(duration));

        var result = await cmd.ExecuteScalarAsync(ct);
        return result as long?;
    }

    public async Task<bool> RenewAsync(string lockKey, string lockedBy, TimeSpan duration, CancellationToken ct = default)
    {
        const string sql = """
            UPDATE ops.job_locks
            SET locked_until = @newUntil, renewed_at = NOW(), updated_at = NOW()
            WHERE lock_key = @key AND locked_by = @by AND locked_until > NOW()
            """;

        await using var cmd = new NpgsqlCommand(sql, (NpgsqlConnection)_connection);
        cmd.Parameters.AddWithValue("key", lockKey);
        cmd.Parameters.AddWithValue("by", lockedBy);
        cmd.Parameters.AddWithValue("newUntil", DateTimeOffset.UtcNow.Add(duration));

        var rows = await cmd.ExecuteNonQueryAsync(ct);
        return rows > 0;
    }

    public async Task<bool> ReleaseAsync(string lockKey, string lockedBy, CancellationToken ct = default)
    {
        const string sql = """
            UPDATE ops.job_locks
            SET locked_until = NOW(), updated_at = NOW()
            WHERE lock_key = @key AND locked_by = @by
            """;

        await using var cmd = new NpgsqlCommand(sql, (NpgsqlConnection)_connection);
        cmd.Parameters.AddWithValue("key", lockKey);
        cmd.Parameters.AddWithValue("by", lockedBy);

        var rows = await cmd.ExecuteNonQueryAsync(ct);
        return rows > 0;
    }
}
