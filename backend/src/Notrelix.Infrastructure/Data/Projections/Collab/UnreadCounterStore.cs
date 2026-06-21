using System.Data;
using Npgsql;

namespace Notrelix.Infrastructure.Data.Projections.Collab;

public sealed class UnreadCounterStore : IUnreadCounterStore
{
    private readonly IDbConnection _connection;

    public UnreadCounterStore(IDbConnection connection)
    {
        _connection = connection;
    }

    public async Task<int> IncrementAsync(Guid workspaceId, Guid userId, string counterType, CancellationToken ct = default)
    {
        const string sql = """
            INSERT INTO collab.unread_counters (workspace_id, user_id, counter_type, counter_value, updated_at)
            VALUES (@ws, @user, @type, 1, NOW())
            ON CONFLICT (workspace_id, user_id, counter_type)
            DO UPDATE SET counter_value = collab.unread_counters.counter_value + 1, updated_at = NOW()
            RETURNING counter_value
            """;

        await using var cmd = new NpgsqlCommand(sql, (NpgsqlConnection)_connection);
        cmd.Parameters.AddWithValue("ws", workspaceId);
        cmd.Parameters.AddWithValue("user", userId);
        cmd.Parameters.AddWithValue("type", counterType);

        var result = await cmd.ExecuteScalarAsync(ct);
        return result is int i ? i : 0;
    }

    public async Task<int> DecrementAsync(Guid workspaceId, Guid userId, string counterType, int delta = 1, CancellationToken ct = default)
    {
        const string sql = """
            UPDATE collab.unread_counters
            SET counter_value = GREATEST(counter_value - @delta, 0), updated_at = NOW()
            WHERE workspace_id = @ws AND user_id = @user AND counter_type = @type
            RETURNING counter_value
            """;

        await using var cmd = new NpgsqlCommand(sql, (NpgsqlConnection)_connection);
        cmd.Parameters.AddWithValue("ws", workspaceId);
        cmd.Parameters.AddWithValue("user", userId);
        cmd.Parameters.AddWithValue("type", counterType);
        cmd.Parameters.AddWithValue("delta", delta);

        var result = await cmd.ExecuteScalarAsync(ct);
        return result is int i ? i : 0;
    }

    public async Task<int> GetAsync(Guid workspaceId, Guid userId, string counterType, CancellationToken ct = default)
    {
        const string sql = """
            SELECT counter_value FROM collab.unread_counters
            WHERE workspace_id = @ws AND user_id = @user AND counter_type = @type
            """;

        await using var cmd = new NpgsqlCommand(sql, (NpgsqlConnection)_connection);
        cmd.Parameters.AddWithValue("ws", workspaceId);
        cmd.Parameters.AddWithValue("user", userId);
        cmd.Parameters.AddWithValue("type", counterType);

        var result = await cmd.ExecuteScalarAsync(ct);
        return result is int i ? i : 0;
    }

    public async Task RebuildFromNotificationsAsync(Guid workspaceId, Guid userId, string counterType, CancellationToken ct = default)
    {
        const string sql = """
            INSERT INTO collab.unread_counters (workspace_id, user_id, counter_type, counter_value, updated_at)
            SELECT @ws, @user, @type, COUNT(*), NOW()
            FROM collab.notifications
            WHERE workspace_id = @ws AND user_id = @user AND status = 'Unread'
            ON CONFLICT (workspace_id, user_id, counter_type)
            DO UPDATE SET counter_value = EXCLUDED.counter_value, updated_at = NOW()
            """;

        await using var cmd = new NpgsqlCommand(sql, (NpgsqlConnection)_connection);
        cmd.Parameters.AddWithValue("ws", workspaceId);
        cmd.Parameters.AddWithValue("user", userId);
        cmd.Parameters.AddWithValue("type", counterType);

        await cmd.ExecuteNonQueryAsync(ct);
    }
}
