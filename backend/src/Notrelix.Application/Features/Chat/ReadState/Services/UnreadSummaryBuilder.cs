using Notrelix.Application.Features.Chat.Abstractions;
using Notrelix.Application.Features.Chat.ReadState.DTOs;
using Notrelix.Domain.Chat.Messages;
using Notrelix.Domain.Chat.Spaces;

namespace Notrelix.Application.Features.Chat.ReadState.Services;

/// <summary>
/// Builds the unread summary of a person in a Workspace (UC-CHT-D01). Two statements: the person's live memberships with
/// the space names, and one grouped count of the messages after each boundary. The count is computed in the database;
/// no message is loaded. A message counts when it is a user message, is not deleted, is not the person's own, and its
/// sequence is after the person's read boundary in its space.
/// </summary>
public static class UnreadSummaryBuilder
{
    public static async Task<UnreadSummaryDto> BuildAsync(
        IChatDbContext db, Guid workspaceId, Guid userId, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var spaces = await (
            from m in db.ChatMemberships.AsNoTracking()
            join s in db.ChatSpaces.AsNoTracking() on m.SpaceId equals s.Id
            where m.UserId == userId
                  && m.WorkspaceId == workspaceId
                  && s.WorkspaceId == workspaceId
                  && m.LeftAt == null
                  && s.DeletedAt == null
            select new
            {
                m.SpaceId,
                s.Name,
                m.ReadSequence,
                s.LastSequence,
                m.NotificationLevel,
                m.MutedUntil,
                m.MentionCount,
            }).ToListAsync(cancellationToken);

        var unread = await (
            from m in db.ChatMemberships.AsNoTracking()
            join msg in db.ChatMessages.AsNoTracking() on m.SpaceId equals msg.SpaceId
            where m.UserId == userId
                  && m.WorkspaceId == workspaceId
                  && m.LeftAt == null
                  && msg.Sequence > m.ReadSequence
                  && msg.AuthorId != userId
                  && msg.Kind == MessageKind.User
                  && msg.DeletedAt == null
            group msg by msg.SpaceId into g
            select new { SpaceId = g.Key, Count = g.Count(), First = g.Min(x => x.Sequence) }).ToListAsync(cancellationToken);

        var counts = unread.ToDictionary(u => u.SpaceId);
        var rows = spaces
            .OrderBy(s => s.Name, StringComparer.OrdinalIgnoreCase)
            .ThenBy(s => s.SpaceId)
            .Select(s =>
            {
                var level = Membership.EffectiveLevel(s.NotificationLevel, s.MutedUntil, now);
                var muted = level == NotificationLevel.None;
                counts.TryGetValue(s.SpaceId, out var count);
                return new SpaceUnreadDto(
                    s.SpaceId,
                    s.Name,
                    s.ReadSequence,
                    s.LastSequence,
                    count?.Count ?? 0,
                    count is null ? null : (long?)count.First,
                    s.MentionCount,
                    level.ToWire(),
                    muted);
            })
            .ToList();

        var unmuted = rows.Where(r => !r.Muted).ToList();
        return new UnreadSummaryDto(rows, unmuted.Sum(r => r.UnreadCount), unmuted.Sum(r => r.MentionCount));
    }
}
