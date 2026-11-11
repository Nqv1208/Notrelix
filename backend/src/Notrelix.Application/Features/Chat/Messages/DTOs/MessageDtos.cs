using Notrelix.Application.Features.Chat.Attachments.DTOs;
using Notrelix.Application.Features.Chat.Reactions.DTOs;
using Notrelix.Domain.Chat.Messages;

namespace Notrelix.Application.Features.Chat.Messages.DTOs;

/// <summary>One message as readers see it. A tombstone has a null content and keeps its author, time and sequence.</summary>
public sealed record MessageDto(
    Guid Id,
    Guid SpaceId,
    long Sequence,
    string Kind,
    Guid AuthorId,
    string? Content,
    string? ClientMessageId,
    Guid? ReplyToId,
    Guid? ThreadRootId,
    long? ThreadSequence,
    bool AlsoSentToChannel,
    bool Edited,
    bool Deleted,
    DateTimeOffset CreatedAt,
    DateTimeOffset? EditedAt,
    DateTimeOffset? DeletedAt,
    long Version,
    ReplyPreviewDto? ReplyPreview = null,
    IReadOnlyList<ReactionSummaryDto>? Reactions = null,
    IReadOnlyList<MessageAttachmentDto>? Attachments = null)
{
    /// <summary>
    /// The message as readers see it. <paramref name="replyPreview"/> is the live quote of <see cref="ReplyToId"/>, read by the
    /// caller from the quoted message; it is null when the message quotes nothing. <paramref name="reactions"/> is the reaction
    /// summary (UC-CHT-G06), empty when nothing reacted.
    /// <paramref name="attachments"/> is the file list (UC-CHT-G08), empty when the message has none and always empty for a tombstone.
    /// </summary>
    public static MessageDto From(Message message, ReplyPreviewDto? replyPreview = null, IReadOnlyList<ReactionSummaryDto>? reactions = null, IReadOnlyList<MessageAttachmentDto>? attachments = null) => new(
        message.Id,
        message.SpaceId,
        message.Sequence,
        message.Kind.ToWire(),
        message.AuthorId,
        message.Content,
        message.ClientMessageId,
        message.ReplyToId,
        message.ThreadRootId,
        message.ThreadSequence,
        message.AlsoSentToChannel,
        message.IsEdited,
        message.IsDeleted,
        message.CreatedAt,
        message.EditedAt,
        message.DeletedAt,
        message.Version,
        replyPreview,
        reactions ?? [],
        attachments ?? []);
}

/// <summary>
/// The message a reply quotes, as a reader may see it (UC-CHT-F02). Read live at each request, so a quote of a deleted message
/// shows a tombstone. Content is null when the quoted message is deleted; the author and the sequence stay.
/// </summary>
public sealed record ReplyPreviewDto(Guid MessageId, long Sequence, Guid AuthorId, string? Content, bool Deleted);

/// <summary>A page of messages in ascending sequence order, with whether older or newer messages exist.</summary>
public sealed record MessagePageDto(IReadOnlyList<MessageDto> Items, bool HasOlder, bool HasNewer);

/// <summary>The stored message and whether this request created it (false when a retry with the same clientMessageId found it).</summary>
public sealed record SendMessageResultDto(MessageDto Message, bool Created);
