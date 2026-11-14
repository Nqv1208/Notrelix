using Notrelix.Application.Features.Chat.Abstractions;

namespace Notrelix.Infrastructure.Data.Chat;

/// <summary>
/// The read boundaries in Postgres (CHT-009). Each statement is one UPDATE on the caller's membership rows, so the row lock
/// serializes two requests for the same boundary, and GREATEST keeps the stored value from moving backwards. Runs on the
/// request's transaction and under the Workspace's row policy.
/// </summary>
public sealed class ChatReadStateStore(ApplicationDbContext db) : IChatReadStateStore
{
    public async Task<bool> AdvanceAsync(Guid workspaceId, Guid spaceId, Guid userId, long target, CancellationToken cancellationToken)
    {
        var touched = await db.Database.ExecuteSqlInterpolatedAsync($"""
            UPDATE chat.memberships
            SET read_sequence = GREATEST(read_sequence, {target})
            WHERE space_id = {spaceId}
              AND user_id = {userId}
              AND workspace_id = {workspaceId}
              AND left_at IS NULL
            """, cancellationToken);
        return touched > 0;
    }

    public async Task<int> AdvanceAllAsync(Guid workspaceId, Guid userId, CancellationToken cancellationToken)
    {
        // The statement reads each space's last sequence once, from its own snapshot: a message committed after the
        // statement started is not marked, so it stays unread.
        return await db.Database.ExecuteSqlInterpolatedAsync($"""
            UPDATE chat.memberships AS m
            SET read_sequence = GREATEST(m.read_sequence, s.last_sequence)
            FROM chat.spaces AS s
            WHERE s.id = m.space_id
              AND s.workspace_id = {workspaceId}
              AND s.deleted_at IS NULL
              AND m.user_id = {userId}
              AND m.workspace_id = {workspaceId}
              AND m.left_at IS NULL
            """, cancellationToken);
    }
}
