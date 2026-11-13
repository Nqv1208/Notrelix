using Notrelix.Application.Features.Chat.Abstractions;
using Notrelix.Application.Features.Chat.Attachments.Services;
using Notrelix.Application.Features.Collaboration.Public.Attachments;
using Notrelix.Application.Features.Chat.Mentions.Services;
using Notrelix.Application.Features.Chat.Messages.DTOs;
using Notrelix.Application.Features.Chat.Reactions.Services;
using Notrelix.Domain.Chat.Messages;

namespace Notrelix.Application.Features.Chat.Messages.Services;

/// <summary>
/// Builds the message views readers see (UC-CHT-F02, G06): the live quote of each reply, the reaction summary, the mentions and the file list of each message,
/// in a fixed number of queries per page. A quote is always a message of the same space (the send refuses any other), and the
/// caller reads a page only when it may see the space, so a preview never shows content the viewer could not already read. A
/// deleted quoted message gives a tombstone preview with no content.
/// </summary>
public static class ReplyPreviews
{
    public static async Task<IReadOnlyDictionary<Guid, ReplyPreviewDto>> LoadAsync(
        IChatDbContext db, Guid workspaceId, IEnumerable<Guid> quotedIds, CancellationToken cancellationToken)
    {
        var ids = quotedIds.Distinct().ToList();
        if (ids.Count == 0)
            return new Dictionary<Guid, ReplyPreviewDto>();

        var rows = await db.ChatMessages.AsNoTracking()
            .Where(m => m.WorkspaceId == workspaceId && ids.Contains(m.Id))
            .Select(m => new ReplyPreviewDto(m.Id, m.Sequence, m.AuthorId, m.DeletedAt == null ? m.Content : null, m.DeletedAt != null))
            .ToListAsync(cancellationToken);
        return rows.ToDictionary(r => r.MessageId);
    }

    /// <summary>
    /// One message as a DTO, with its live quote, its reaction summary, its mentions and its files. A message the request has just stored is
    /// not in the database yet, so its mentions and the sender's notices come from <paramref name="resolution"/>.
    /// </summary>
    public static async Task<MessageDto> DtoAsync(
        IChatDbContext db, IChatMessageAttachments attachments, Guid workspaceId, Guid viewerId, bool viewerIsGuest, Message message, CancellationToken cancellationToken, MentionResolution? resolution = null)
    {
        var previews = await LoadAsync(db, workspaceId, message.ReplyToId is { } quoted ? [quoted] : [], cancellationToken);
        var reactions = await ReactionSummaries.ForMessageAsync(db, workspaceId, viewerId, message.Id, cancellationToken);
        var files = await MessageAttachments.ForMessageAsync(attachments, workspaceId, message.Id, cancellationToken);
        var threads = await LoadThreadsAsync(db, workspaceId, message.ThreadRootId is null ? [message.Id] : [], cancellationToken);
        var thread = threads.GetValueOrDefault(message.Id);
        if (resolution is not null)
            return MessageDto.From(message, PreviewOf(message, previews), reactions, resolution.Spans.Select(MessageMentions.ToDto).ToList(), resolution.Notices, files, thread);

        var mentions = await MessageMentions.LoadAsync(db, workspaceId, viewerId, viewerIsGuest, [message.Id], cancellationToken);
        return MessageDto.From(message, PreviewOf(message, previews), reactions, mentions.TryGetValue(message.Id, out var list) ? list : [], null, files, thread);
    }

    /// <summary>A page of messages as DTOs, each with its live quote and reaction summary, in two lookups for the whole page.</summary>
    public static async Task<IReadOnlyList<MessageDto>> DtosAsync(
        IChatDbContext db, IChatMessageAttachments attachments, Guid workspaceId, Guid viewerId, bool viewerIsGuest, IReadOnlyList<Message> messages, CancellationToken cancellationToken)
    {
        var previews = await LoadAsync(db, workspaceId, messages.Where(m => m.ReplyToId is not null).Select(m => m.ReplyToId!.Value), cancellationToken);
        var reactions = await ReactionSummaries.LoadAsync(db, workspaceId, viewerId, messages.Select(m => m.Id), cancellationToken);
        var mentions = await MessageMentions.LoadAsync(db, workspaceId, viewerId, viewerIsGuest, messages.Select(m => m.Id), cancellationToken);
        var files = await MessageAttachments.LoadAsync(attachments, workspaceId, messages.Select(m => m.Id), cancellationToken);
        var threads = await LoadThreadsAsync(db, workspaceId, messages.Where(m => m.ThreadRootId is null).Select(m => m.Id), cancellationToken);
        return messages
            .Select(m => MessageDto.From(
                m,
                PreviewOf(m, previews),
                reactions.TryGetValue(m.Id, out var summary) ? summary : [],
                mentions.TryGetValue(m.Id, out var list) ? list : [],
                null,
                files.TryGetValue(m.Id, out var fileList) ? fileList : [],
                threads.GetValueOrDefault(m.Id)))
            .ToList();
    }

    /// <summary>
    /// The stored threads of the given root messages, in one query for the whole page (the thread is keyed by its root id and holds the
    /// reply count and the last reply, so no reply is counted per message). A root with no reply has no row.
    /// </summary>
    public static async Task<IReadOnlyDictionary<Guid, MessageThread>> LoadThreadsAsync(
        IChatDbContext db, Guid workspaceId, IEnumerable<Guid> rootIds, CancellationToken cancellationToken)
    {
        var ids = rootIds.Distinct().ToList();
        if (ids.Count == 0)
            return new Dictionary<Guid, MessageThread>();

        var rows = await db.ChatThreads.AsNoTracking()
            .Where(t => t.WorkspaceId == workspaceId && ids.Contains(t.RootMessageId))
            .ToListAsync(cancellationToken);
        return rows.ToDictionary(t => t.RootMessageId);
    }

    private static ReplyPreviewDto? PreviewOf(Message message, IReadOnlyDictionary<Guid, ReplyPreviewDto> previews) =>
        message.ReplyToId is { } quoted && previews.TryGetValue(quoted, out var preview) ? preview : null;
}
