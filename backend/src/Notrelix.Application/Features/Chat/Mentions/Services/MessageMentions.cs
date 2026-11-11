using Notrelix.Application.Features.Chat.Abstractions;
using Notrelix.Application.Features.Chat.Mentions.DTOs;
using Notrelix.Domain.Chat.Messages;

namespace Notrelix.Application.Features.Chat.Mentions.Services;

/// <summary>
/// The mentions of messages as readers see them (UC-CHT-G01, G04): in one query for the rows and one for the channels they link,
/// whatever the page size. A channel link the reader cannot see is left out, so a private channel is never revealed to someone
/// outside it; the text stays and renders as plain text.
/// </summary>
public static class MessageMentions
{
    public static async Task<IReadOnlyDictionary<Guid, IReadOnlyList<MentionDto>>> LoadAsync(
        IChatDbContext db, Guid workspaceId, Guid viewerId, IEnumerable<Guid> messageIds, CancellationToken cancellationToken)
    {
        var ids = messageIds.Distinct().ToList();
        if (ids.Count == 0)
            return new Dictionary<Guid, IReadOnlyList<MentionDto>>();

        var rows = await db.ChatMessageMentions.AsNoTracking()
            .Where(m => m.WorkspaceId == workspaceId && ids.Contains(m.MessageId))
            .OrderBy(m => m.Start)
            .ToListAsync(cancellationToken);
        if (rows.Count == 0)
            return new Dictionary<Guid, IReadOnlyList<MentionDto>>();

        var links = rows.Where(r => r.Kind == MentionKind.ChannelLink).Select(r => r.TargetId!.Value).Distinct().ToList();
        var visible = await MentionResolver.VisibleChannelsAsync(db, workspaceId, viewerId, isGuest: false, links, cancellationToken);

        return rows
            .Where(r => r.Kind != MentionKind.ChannelLink || visible.Contains(r.TargetId!.Value))
            .GroupBy(r => r.MessageId)
            .ToDictionary(g => g.Key, g => (IReadOnlyList<MentionDto>)g.Select(r => ToDto(r.ToSpan())).ToList());
    }

    public static MentionDto ToDto(MentionSpan span) => new(
        span.Kind.ToWire(),
        span.Start,
        span.Length,
        span.Kind == MentionKind.User ? span.TargetId : null,
        span.Kind == MentionKind.ChannelLink ? span.TargetId : null);

    /// <summary>Replaces a message's stored mentions with the resolved set (a send stores the first set; an edit replaces it).</summary>
    public static async Task ReplaceAsync(
        IChatDbContext db, Message message, IReadOnlyList<MentionSpan> spans, bool hadBefore, CancellationToken cancellationToken)
    {
        if (hadBefore)
        {
            var old = await db.ChatMessageMentions.Where(m => m.MessageId == message.Id).ToListAsync(cancellationToken);
            foreach (var row in old)
                db.ChatMessageMentions.Remove(row);
        }

        foreach (var span in spans)
            db.ChatMessageMentions.Add(MessageMention.Create(message.AccountId, message.WorkspaceId, message.SpaceId, message.Id, span));
    }
}
