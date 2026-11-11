using Notrelix.Application.Features.Chat.Abstractions;
using Notrelix.Application.Features.Chat.Reactions.DTOs;
using Notrelix.Domain.Chat.Messages;

namespace Notrelix.Application.Features.Chat.Reactions.Services;

/// <summary>
/// The reaction summary of messages (UC-CHT-G06): per message, per palette kind, the count and whether the viewer is among them.
/// One grouped query serves a whole page of messages, so a history or thread read costs one reaction query, not one per message.
/// </summary>
public static class ReactionSummaries
{
    public static async Task<IReadOnlyDictionary<Guid, IReadOnlyList<ReactionSummaryDto>>> LoadAsync(
        IChatDbContext db, Guid workspaceId, Guid viewerId, IEnumerable<Guid> messageIds, CancellationToken cancellationToken)
    {
        var ids = messageIds.Distinct().ToList();
        if (ids.Count == 0)
            return new Dictionary<Guid, IReadOnlyList<ReactionSummaryDto>>();

        var rows = await db.ChatReactions.AsNoTracking()
            .Where(r => r.WorkspaceId == workspaceId && ids.Contains(r.MessageId))
            .GroupBy(r => new { r.MessageId, r.Kind })
            .Select(g => new
            {
                g.Key.MessageId,
                g.Key.Kind,
                Count = g.Count(),
                Mine = g.Count(r => r.UserId == viewerId),
            })
            .ToListAsync(cancellationToken);

        return rows
            .GroupBy(r => r.MessageId)
            .ToDictionary(
                g => g.Key,
                g => (IReadOnlyList<ReactionSummaryDto>)g
                    .OrderBy(r => ReactionKinds.Rank(r.Kind))
                    .Select(r => new ReactionSummaryDto(r.Kind, r.Count, r.Mine > 0))
                    .ToList());
    }

    /// <summary>
    /// The summary after one change by the viewer, computed from the summary read under the space lock. A handler returns this in
    /// its response before the commit, so the response reflects the change without a second read.
    /// </summary>
    public static IReadOnlyList<ReactionSummaryDto> Apply(IReadOnlyList<ReactionSummaryDto> current, string kind, bool added)
    {
        var list = current.ToList();
        var index = list.FindIndex(r => r.Emoji == kind);
        if (added)
        {
            if (index >= 0)
                list[index] = list[index] with { Count = list[index].Count + 1, ReactedByMe = true };
            else
                list.Add(new ReactionSummaryDto(kind, 1, ReactedByMe: true));
        }
        else if (index >= 0)
        {
            var next = list[index].Count - 1;
            if (next <= 0)
                list.RemoveAt(index);
            else
                list[index] = list[index] with { Count = next, ReactedByMe = false };
        }
        return list.OrderBy(r => ReactionKinds.Rank(r.Emoji)).ToList();
    }

    /// <summary>The summary of one message, empty when nothing reacted.</summary>
    public static async Task<IReadOnlyList<ReactionSummaryDto>> ForMessageAsync(
        IChatDbContext db, Guid workspaceId, Guid viewerId, Guid messageId, CancellationToken cancellationToken)
    {
        var map = await LoadAsync(db, workspaceId, viewerId, [messageId], cancellationToken);
        return map.TryGetValue(messageId, out var summary) ? summary : [];
    }
}
