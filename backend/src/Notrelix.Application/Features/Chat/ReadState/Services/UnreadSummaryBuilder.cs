using Notrelix.Application.Features.Chat.Abstractions;
using Notrelix.Application.Features.Chat.ReadState.DTOs;
using Notrelix.Domain.Chat.Spaces;

namespace Notrelix.Application.Features.Chat.ReadState.Services;

/// <summary>
/// Builds the unread summary of a person in a Workspace (UC-CHT-D01, D04) from the stored read state: one statement over the person's
/// live memberships joined to their spaces, for one space only when <c>onlySpaceId</c> is given (a caller that announces one space).
/// Nothing is counted. A space is unread when its last sequence is past the person's boundary, the number is the sequence difference
/// (sequences are dense per space, so it is exact for a conversation where only the people write; it also counts system messages,
/// tombstones and the person's own messages sent after they fell behind), and the mention count is the stored one (UC-CHT-D04).
/// The cost does not depend on the backlog.
/// </summary>
public static class UnreadSummaryBuilder
{
    public static async Task<UnreadSummaryDto> BuildAsync(
        IChatDbContext db, Guid workspaceId, Guid userId, DateTimeOffset now, CancellationToken cancellationToken, Guid? onlySpaceId = null)
    {
        var spaces = await (
            from m in db.ChatMemberships.AsNoTracking()
            join s in db.ChatSpaces.AsNoTracking() on m.SpaceId equals s.Id
            where m.UserId == userId
                  && m.WorkspaceId == workspaceId
                  && s.WorkspaceId == workspaceId
                  && m.LeftAt == null
                  && s.DeletedAt == null
                  && (onlySpaceId == null || m.SpaceId == onlySpaceId)
            select new
            {
                m.SpaceId,
                s.Name,
                m.ReadSequence,
                s.LastSequence,
                m.MentionCount,
                m.NotificationLevel,
                m.MutedUntil,
            }).ToListAsync(cancellationToken);

        var rows = spaces
            .OrderBy(s => s.Name, StringComparer.OrdinalIgnoreCase)
            .ThenBy(s => s.SpaceId)
            .Select(s =>
            {
                var level = Membership.EffectiveLevel(s.NotificationLevel, s.MutedUntil, now);
                var behind = (int)Math.Min(Math.Max(s.LastSequence - s.ReadSequence, 0), int.MaxValue);
                return new SpaceUnreadDto(
                    s.SpaceId,
                    s.Name,
                    s.ReadSequence,
                    s.LastSequence,
                    behind,
                    behind == 0 ? null : s.ReadSequence + 1,
                    s.MentionCount,
                    level.ToWire(),
                    level == NotificationLevel.None);
            })
            .ToList();

        var unmuted = rows.Where(r => !r.Muted).ToList();
        return new UnreadSummaryDto(rows, unmuted.Sum(r => r.UnreadCount), unmuted.Sum(r => r.MentionCount));
    }
}
