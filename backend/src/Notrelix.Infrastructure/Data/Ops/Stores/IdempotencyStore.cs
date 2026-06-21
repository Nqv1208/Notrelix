using System.Data;
using Npgsql;

namespace Notrelix.Infrastructure.Data.Ops.Stores;

public sealed class IdempotencyStore : IIdempotencyStore
{
    private readonly IDbConnection _connection;

    public IdempotencyStore(IDbConnection connection)
    {
        _connection = connection;
    }

    public async Task<Guid?> TryAcquireAsync(
        string idempotencyKey,
        string scope,
        string requestMethod,
        string requestPath,
        string requestHash,
        Guid? workspaceId,
        Guid? userId,
        DateTimeOffset expiresAt,
        DateTimeOffset now,
        CancellationToken ct = default)
    {
        const string sql = """
            INSERT INTO ops.idempotency_keys (id, workspace_id, user_id, scope, idempotency_key, request_method, request_path, request_hash, status, expires_at, created_at)
            VALUES (gen_random_uuid(), @ws, @user, @scope, @key, @method, @path, @hash, 'Started', @expires, @now)
            ON CONFLICT (scope, idempotency_key) DO UPDATE SET locked_until = @until
            WHERE ops.idempotency_keys.status = 'Started' AND ops.idempotency_keys.locked_until IS NULL
            RETURNING id
            """;

        await using var cmd = new NpgsqlCommand(sql, (NpgsqlConnection)_connection);
        cmd.Parameters.AddWithValue("ws", (object?)workspaceId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("user", (object?)userId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("scope", scope);
        cmd.Parameters.AddWithValue("key", idempotencyKey);
        cmd.Parameters.AddWithValue("method", requestMethod);
        cmd.Parameters.AddWithValue("path", requestPath);
        cmd.Parameters.AddWithValue("hash", requestHash);
        cmd.Parameters.AddWithValue("expires", expiresAt);
        cmd.Parameters.AddWithValue("now", now);
        cmd.Parameters.AddWithValue("until", now.AddMinutes(5));

        var result = await cmd.ExecuteScalarAsync(ct);
        return result as Guid?;
    }

    public async Task<bool> CompleteAsync(
        Guid id,
        int responseStatusCode,
        string? responseBodyJson,
        DateTimeOffset now,
        CancellationToken ct = default)
    {
        const string sql = """
            UPDATE ops.idempotency_keys
            SET status = 'Completed', response_status_code = @code, response_body_json = @body, completed_at = @now, locked_until = NULL
            WHERE id = @id AND status = 'Started'
            """;

        await using var cmd = new NpgsqlCommand(sql, (NpgsqlConnection)_connection);
        cmd.Parameters.AddWithValue("id", id);
        cmd.Parameters.AddWithValue("code", responseStatusCode);
        cmd.Parameters.AddWithValue("body", (object?)responseBodyJson ?? DBNull.Value);
        cmd.Parameters.AddWithValue("now", now);

        var rows = await cmd.ExecuteNonQueryAsync(ct);
        return rows > 0;
    }

    public async Task<bool> FailAsync(
        Guid id,
        string errorMessage,
        DateTimeOffset now,
        CancellationToken ct = default)
    {
        const string sql = """
            UPDATE ops.idempotency_keys
            SET status = 'Failed', error_message = @error, completed_at = @now, locked_until = NULL
            WHERE id = @id
            """;

        await using var cmd = new NpgsqlCommand(sql, (NpgsqlConnection)_connection);
        cmd.Parameters.AddWithValue("id", id);
        cmd.Parameters.AddWithValue("error", errorMessage);
        cmd.Parameters.AddWithValue("now", now);

        var rows = await cmd.ExecuteNonQueryAsync(ct);
        return rows > 0;
    }
}
