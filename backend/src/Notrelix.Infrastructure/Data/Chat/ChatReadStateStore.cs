using Notrelix.Application.Features.Chat.Abstractions;
using Notrelix.Domain.Chat.Messages;

namespace Notrelix.Infrastructure.Data.Chat;

/// <summary>
/// The read boundaries and the stored mention counts in Postgres (CHT-009, UC-CHT-D04). Each statement is one UPDATE on membership
/// rows, so the row lock serializes two requests for the same row, and GREATEST keeps a boundary from moving backwards. Runs on the
/// request's transaction and under the Workspace's row policy. A send touches one row per mentioned member and counts nothing; a read
/// sets the mention count to the mentions still after the new boundary, so the number heals at every read.
/// </summary>
public sealed class ChatReadStateStore(ApplicationDbContext db) : IChatReadStateStore
{
    public async Task<bool> AdvanceAsync(Guid workspaceId, Guid spaceId, Guid userId, long target, CancellationToken cancellationToken)
    {
        // The mention count is the mentions after the new boundary (see the class summary). The messages it counts are those of the
        // rule in MentionCounts: live user messages of someone else, sent after the member joined, that mention them or everyone.
        // The row is locked in a statement of its own: the UPDATE below then takes a fresh snapshot, so its recount sees every mention a
        // send committed while this request waited for the lock (under READ COMMITTED one statement keeps its first snapshot).
        var locked = await db.Database.SqlQuery<int>($"""
            SELECT 1 AS "Value" FROM chat.memberships
            WHERE space_id = {spaceId} AND user_id = {userId} AND workspace_id = {workspaceId} AND left_at IS NULL
            FOR UPDATE
            """).ToListAsync(cancellationToken);
        if (locked.Count == 0)
            return false;

        var touched = await db.ChatMemberships
            .Where(m => m.SpaceId == spaceId && m.UserId == userId && m.WorkspaceId == workspaceId && m.LeftAt == null)
            .ExecuteUpdateAsync(s => s
                .SetProperty(m => m.ReadSequence, m => Math.Max(m.ReadSequence, target))
                .SetProperty(m => m.MentionCount, m => db.ChatMessages.Count(msg =>
                    msg.SpaceId == m.SpaceId && msg.Sequence > Math.Max(m.ReadSequence, target)
                    && msg.CreatedAt >= m.JoinedAt && msg.AuthorId != m.UserId && msg.Kind == MessageKind.User && msg.DeletedAt == null
                    && db.ChatMessageMentions.Any(x => x.MessageId == msg.Id && x.SpaceId == msg.SpaceId
                        && ((x.Kind == MentionKind.User && x.TargetId == m.UserId)
                            || x.Kind == MentionKind.BroadcastChannel || x.Kind == MentionKind.BroadcastHere)))),
                cancellationToken);
        return touched > 0;
    }

    public async Task<int> AdvanceAllAsync(Guid workspaceId, Guid userId, CancellationToken cancellationToken)
    {
        // Each space's last sequence is read from the statement's snapshot: a message committed after the statement
        // started is not marked, so it stays unread and keeps its mention.
        // Locked first, in space order (so two requests never wait on each other), then updated from a fresh snapshot; see AdvanceAsync.
        await db.Database.SqlQuery<int>($"""
            SELECT 1 AS "Value" FROM chat.memberships
            WHERE user_id = {userId} AND workspace_id = {workspaceId} AND left_at IS NULL
            ORDER BY space_id
            FOR UPDATE
            """).ToListAsync(cancellationToken);

        return await db.ChatMemberships
            .Where(m => m.UserId == userId && m.WorkspaceId == workspaceId && m.LeftAt == null
                        && db.ChatSpaces.Any(s => s.Id == m.SpaceId && s.WorkspaceId == workspaceId && s.DeletedAt == null))
            .ExecuteUpdateAsync(s => s
                .SetProperty(m => m.ReadSequence, m => Math.Max(m.ReadSequence,
                    db.ChatSpaces.Where(sp => sp.Id == m.SpaceId).Select(sp => sp.LastSequence).First()))
                .SetProperty(m => m.MentionCount, m => db.ChatMessages.Count(msg =>
                    msg.SpaceId == m.SpaceId
                    && msg.Sequence > Math.Max(m.ReadSequence, db.ChatSpaces.Where(sp => sp.Id == m.SpaceId).Select(sp => sp.LastSequence).First())
                    && msg.CreatedAt >= m.JoinedAt && msg.AuthorId != m.UserId && msg.Kind == MessageKind.User && msg.DeletedAt == null
                    && db.ChatMessageMentions.Any(x => x.MessageId == msg.Id && x.SpaceId == msg.SpaceId
                        && ((x.Kind == MentionKind.User && x.TargetId == m.UserId)
                            || x.Kind == MentionKind.BroadcastChannel || x.Kind == MentionKind.BroadcastHere)))),
                cancellationToken);
    }

    public Task AdvanceAuthorAsync(Guid workspaceId, Guid spaceId, Guid authorId, long sequence, CancellationToken cancellationToken) =>
        db.ChatMemberships
            .Where(m => m.SpaceId == spaceId && m.UserId == authorId && m.WorkspaceId == workspaceId && m.ReadSequence == sequence - 1)
            .ExecuteUpdateAsync(s => s.SetProperty(m => m.ReadSequence, sequence), cancellationToken);

    public Task AddMentionsAsync(
        Guid workspaceId, Guid spaceId, long sequence, DateTimeOffset createdAt, Guid authorId, MentionAudience audience, CancellationToken cancellationToken)
    {
        if (audience.IsEmpty)
            return Task.CompletedTask;

        var everyone = audience.Everyone;
        var users = audience.Users.ToArray();
        return db.ChatMemberships
            .Where(m => m.SpaceId == spaceId && m.WorkspaceId == workspaceId && m.LeftAt == null && m.UserId != authorId
                        && m.ReadSequence < sequence && m.JoinedAt <= createdAt
                        && (everyone || users.Contains(m.UserId)))
            .ExecuteUpdateAsync(s => s.SetProperty(m => m.MentionCount, m => m.MentionCount + 1), cancellationToken);
    }

    public async Task RemoveMentionsAsync(Guid workspaceId, IReadOnlyCollection<Guid> messageIds, CancellationToken cancellationToken)
    {
        if (messageIds.Count == 0)
            return;

        var ids = messageIds.ToArray();
        var spaceIds = await db.ChatMessages.AsNoTracking()
            .Where(msg => ids.Contains(msg.Id) && msg.WorkspaceId == workspaceId)
            .Select(msg => msg.SpaceId).Distinct().ToArrayAsync(cancellationToken);

        await db.ChatMemberships
            .Where(m => m.WorkspaceId == workspaceId && m.LeftAt == null && spaceIds.Contains(m.SpaceId) && m.MentionCount > 0)
            .ExecuteUpdateAsync(s => s.SetProperty(m => m.MentionCount, m => Math.Max(0, m.MentionCount - db.ChatMessages.Count(msg =>
                ids.Contains(msg.Id) && msg.SpaceId == m.SpaceId && msg.Sequence > m.ReadSequence
                && msg.CreatedAt >= m.JoinedAt && msg.AuthorId != m.UserId && msg.Kind == MessageKind.User && msg.DeletedAt == null
                && db.ChatMessageMentions.Any(x => x.MessageId == msg.Id && x.SpaceId == msg.SpaceId
                    && ((x.Kind == MentionKind.User && x.TargetId == m.UserId)
                        || x.Kind == MentionKind.BroadcastChannel || x.Kind == MentionKind.BroadcastHere))))),
                cancellationToken);
    }
}
