using Notrelix.Application.Features.Chat.Abstractions;
using Notrelix.Application.Features.Chat.Messages.DTOs;
using Notrelix.Domain.Chat.Messages;

namespace Notrelix.Application.Features.Chat.Messages.Services;

/// <summary>
/// Reads the quoted message of each reply in a page (UC-CHT-F02). A quote is always a message of the same space (the send
/// refuses any other), and the caller reads a page only when it may see the space, so a preview never shows content the
/// viewer could not already read. A deleted quoted message gives a tombstone preview with no content.
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

    /// <summary>One message as a DTO, with its live quote when it quotes one.</summary>
    public static async Task<MessageDto> DtoAsync(IChatDbContext db, Guid workspaceId, Message message, CancellationToken cancellationToken)
    {
        var previews = await LoadAsync(db, workspaceId, message.ReplyToId is { } quoted ? [quoted] : [], cancellationToken);
        return MessageDto.From(message, PreviewOf(message, previews));
    }

    /// <summary>A page of messages as DTOs, each with its live quote, in one lookup.</summary>
    public static async Task<IReadOnlyList<MessageDto>> DtosAsync(
        IChatDbContext db, Guid workspaceId, IReadOnlyList<Message> messages, CancellationToken cancellationToken)
    {
        var previews = await LoadAsync(db, workspaceId, messages.Where(m => m.ReplyToId is not null).Select(m => m.ReplyToId!.Value), cancellationToken);
        return messages.Select(m => MessageDto.From(m, PreviewOf(m, previews))).ToList();
    }

    private static ReplyPreviewDto? PreviewOf(Message message, IReadOnlyDictionary<Guid, ReplyPreviewDto> previews) =>
        message.ReplyToId is { } quoted && previews.TryGetValue(quoted, out var preview) ? preview : null;
}
