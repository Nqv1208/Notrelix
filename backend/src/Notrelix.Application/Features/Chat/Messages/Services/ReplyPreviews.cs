using Notrelix.Application.Features.Chat.Abstractions;
using Notrelix.Application.Features.Chat.Messages.DTOs;
using Notrelix.Application.Features.Chat.Reactions.Services;
using Notrelix.Domain.Chat.Messages;

namespace Notrelix.Application.Features.Chat.Messages.Services;

/// <summary>
/// Builds the message views readers see (UC-CHT-F02, G06): the live quote of each reply and the reaction summary of each message,
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

    /// <summary>One message as a DTO, with its live quote and its reaction summary.</summary>
    public static async Task<MessageDto> DtoAsync(IChatDbContext db, Guid workspaceId, Guid viewerId, Message message, CancellationToken cancellationToken)
    {
        var previews = await LoadAsync(db, workspaceId, message.ReplyToId is { } quoted ? [quoted] : [], cancellationToken);
        var reactions = await ReactionSummaries.ForMessageAsync(db, workspaceId, viewerId, message.Id, cancellationToken);
        return MessageDto.From(message, PreviewOf(message, previews), reactions);
    }

    /// <summary>A page of messages as DTOs, each with its live quote and reaction summary, in two lookups for the whole page.</summary>
    public static async Task<IReadOnlyList<MessageDto>> DtosAsync(
        IChatDbContext db, Guid workspaceId, Guid viewerId, IReadOnlyList<Message> messages, CancellationToken cancellationToken)
    {
        var previews = await LoadAsync(db, workspaceId, messages.Where(m => m.ReplyToId is not null).Select(m => m.ReplyToId!.Value), cancellationToken);
        var reactions = await ReactionSummaries.LoadAsync(db, workspaceId, viewerId, messages.Select(m => m.Id), cancellationToken);
        return messages
            .Select(m => MessageDto.From(m, PreviewOf(m, previews), reactions.TryGetValue(m.Id, out var summary) ? summary : []))
            .ToList();
    }

    private static ReplyPreviewDto? PreviewOf(Message message, IReadOnlyDictionary<Guid, ReplyPreviewDto> previews) =>
        message.ReplyToId is { } quoted && previews.TryGetValue(quoted, out var preview) ? preview : null;
}
