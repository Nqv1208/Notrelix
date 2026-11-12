namespace Notrelix.Application.Features.Collaboration.Public.Attachments;

/// <summary>
/// Collaboration-owned boundary for the files on a Chat message (UC-CHT-G08, CHT-012). Chat keeps the message and decides who may see,
/// attach or remove (space membership, archive state, authorship); Collaboration keeps the attachment row, the object in storage and the
/// shared file policy (size cap, blocked extensions, sniffed content type, per-target cap) under the target kind <c>chat.message</c>.
/// The caller has already authorized the request: every operation here only scopes its rows to the exact message, so a file of another
/// message or of another target kind is never listed, opened or removed through a message that does not own it.
/// </summary>
public interface IChatMessageAttachments
{
    /// <summary>
    /// Stores a file under the message through the shared flow. The file policy refuses a blocked or executable type (415), a file over
    /// the size cap (413 or 400) and a message that already holds the maximum number of files (400) before or while the bytes are
    /// streamed; a refused upload leaves no row and no object. The row is added to the request unit of work, not committed here.
    /// </summary>
    Task<ChatMessageAttachmentFact> UploadAsync(Guid messageId, string fileName, Stream content, CancellationToken cancellationToken);

    /// <summary>The files of many messages in one query, oldest first, keyed by message id. Messages without files are absent.</summary>
    Task<IReadOnlyDictionary<Guid, IReadOnlyList<ChatMessageAttachmentFact>>> ListAsync(IReadOnlyCollection<Guid> messageIds, CancellationToken cancellationToken);

    /// <summary>Opens the stored bytes of one file of the message. A file of another message, a link, a removed file or a missing object is NotFound. The caller disposes the stream.</summary>
    Task<ChatMessageAttachmentContent> OpenAsync(Guid messageId, Guid attachmentId, CancellationToken cancellationToken);

    /// <summary>Removes one file of the message (a soft delete; the stored object is purged by the retention job). False when it is not a file of this message.</summary>
    Task<bool> RemoveAsync(Guid messageId, Guid attachmentId, CancellationToken cancellationToken);

    /// <summary>Removes every file of the message, as when the message is deleted. Returns how many were removed.</summary>
    Task<int> RemoveAllAsync(Guid messageId, CancellationToken cancellationToken);

    /// <summary>
    /// Removes the files of many messages at once, for the retention job (UC-CHT-H05): the job has no request principal, so the soft delete
    /// is recorded for the person named. The stored objects are purged by the attachment retention job. Returns how many files were removed.
    /// </summary>
    Task<int> RemoveAllAsync(IReadOnlyCollection<Guid> messageIds, Guid deletedBy, CancellationToken cancellationToken);
}

/// <summary>One stored file of a message. There is no URL: the bytes are read through an authorized request for the message.</summary>
public sealed record ChatMessageAttachmentFact(
    Guid Id,
    Guid MessageId,
    string FileName,
    long SizeBytes,
    string ContentType,
    Guid UploadedBy,
    DateTimeOffset CreatedAt);

/// <summary>An opened file. The consumer owns the stream and disposes it.</summary>
public sealed record ChatMessageAttachmentContent(Stream Content, string FileName, string ContentType, long SizeBytes);
