using Notrelix.Application.Features.Chat.Abstractions;
using Notrelix.Domain.Chat.Messages;

namespace Notrelix.Application.Features.Chat.Mentions.Services;

/// <summary>
/// How many messages mention a person and are still unread in a space (CHT-009, CHT-010, UC-CHT-D04). The count is computed from
/// the stored mentions, never kept as a number that could drift: a message counts when it is a user message that is not deleted,
/// is not the person's own, is after the person's read boundary, was sent after the person joined (a mention from before they
/// could see the space is never counted, COL-030), and mentions them by id or carries an @channel or @here. A message with
/// several mentions of one person counts once. Reading past a message clears it, a delete or an edit that drops the mention
/// removes it, and a muted space counts here but is left out of the badge totals by the caller (CHT-019).
/// @here is counted like @channel: who is present is not known to this side, so the badge reaches every member and the
/// notification step narrows it to the present ones.
/// </summary>
public static class MentionCounts
{
    /// <summary>The count per space for one person, over every live membership they hold in the Workspace.</summary>
    public static async Task<IReadOnlyDictionary<Guid, int>> ForUserAsync(
        IChatDbContext db, Guid workspaceId, Guid userId, CancellationToken cancellationToken)
    {
        var rows = await (
            from m in db.ChatMemberships.AsNoTracking()
            join msg in db.ChatMessages.AsNoTracking() on m.SpaceId equals msg.SpaceId
            where m.UserId == userId
                  && m.WorkspaceId == workspaceId
                  && m.LeftAt == null
                  && msg.Sequence > m.ReadSequence
                  && msg.CreatedAt >= m.JoinedAt
                  && msg.AuthorId != userId
                  && msg.Kind == MessageKind.User
                  && msg.DeletedAt == null
                  && db.ChatMessageMentions.Any(x => x.MessageId == msg.Id
                      && ((x.Kind == MentionKind.User && x.TargetId == userId)
                          || x.Kind == MentionKind.BroadcastChannel
                          || x.Kind == MentionKind.BroadcastHere))
            group msg by msg.SpaceId into g
            select new { SpaceId = g.Key, Count = g.Count() }).ToListAsync(cancellationToken);
        return rows.ToDictionary(r => r.SpaceId, r => r.Count);
    }

    /// <summary>The count in one space for each of the given people (a new message's recipients), in one grouped query.</summary>
    public static async Task<IReadOnlyDictionary<Guid, int>> ForSpaceAsync(
        IChatDbContext db, Guid spaceId, IReadOnlyCollection<Guid> userIds, CancellationToken cancellationToken)
    {
        if (userIds.Count == 0)
            return new Dictionary<Guid, int>();

        var rows = await (
            from m in db.ChatMemberships.AsNoTracking()
            join msg in db.ChatMessages.AsNoTracking() on m.SpaceId equals msg.SpaceId
            where m.SpaceId == spaceId
                  && m.LeftAt == null
                  && userIds.Contains(m.UserId)
                  && msg.Sequence > m.ReadSequence
                  && msg.CreatedAt >= m.JoinedAt
                  && msg.AuthorId != m.UserId
                  && msg.Kind == MessageKind.User
                  && msg.DeletedAt == null
                  && db.ChatMessageMentions.Any(x => x.MessageId == msg.Id
                      && ((x.Kind == MentionKind.User && x.TargetId == m.UserId)
                          || x.Kind == MentionKind.BroadcastChannel
                          || x.Kind == MentionKind.BroadcastHere))
            group msg by m.UserId into g
            select new { UserId = g.Key, Count = g.Count() }).ToListAsync(cancellationToken);
        return rows.ToDictionary(r => r.UserId, r => r.Count);
    }
}
