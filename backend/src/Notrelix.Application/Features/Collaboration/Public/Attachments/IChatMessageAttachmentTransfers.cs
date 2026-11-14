namespace Notrelix.Application.Features.Collaboration.Public.Attachments;

/// <summary>
/// The two-step transfer of a file on a Chat message (UC-CHT-G08): a pre-signed upload of the bytes, a commit that verifies them and applies
/// the shared file policy, and a pre-signed download. Chat authorizes every call before it reaches this boundary, exactly as for
/// <see cref="IChatMessageAttachments"/>. Available only when the storage provider reports pre-signed URLs; otherwise every call throws
/// <see cref="NotSupportedException"/> and the caller answers with the streaming path instead.
/// </summary>
public interface IChatMessageAttachmentTransfers
{
    /// <summary>
    /// Validates the declared name, size and type against the shared policy, creates a pending upload that expires, and returns the
    /// pre-signed PUT. Writes no attachment row. Refuses a blocked extension (415), a declared size over the cap (413) or a message that
    /// already holds the maximum number of files (400).
    /// </summary>
    Task<ChatMessageAttachmentUploadTicket> CreateUploadAsync(
        Guid messageId, string fileName, long declaredSize, string declaredContentType, ChatMessageAudioUpload? audio, CancellationToken cancellationToken);

    /// <summary>
    /// Verifies the stored object of a pending upload of this message and applies the file policy to its real bytes. On success adds the
    /// attachment row (not committed here) and returns it. A repeated commit of a completed upload returns the same file with
    /// <see cref="ChatMessageAttachmentCommit.Replayed"/> true and adds nothing. A policy refusal deletes the object and throws.
    /// </summary>
    Task<ChatMessageAttachmentCommit> CommitUploadAsync(Guid messageId, Guid uploadId, CancellationToken cancellationToken);

    /// <summary>True when the storage provider can issue pre-signed URLs. When false, every other member throws and the streaming path applies.</summary>
    bool SupportsPresignedUrls { get; }

    /// <summary>A pre-signed GET of one file of the message. A file of another message or a removed file is NotFound.</summary>
    Task<ChatMessageAttachmentDownload> CreateDownloadUrlAsync(Guid messageId, Guid attachmentId, CancellationToken cancellationToken);
}

/// <summary>The answer to a create: the pending upload id, the pre-signed PUT and when the upload window closes.</summary>
public sealed record ChatMessageAttachmentUploadTicket(
    Guid UploadId, string Method, string Url, string ContentType, long ContentLength, DateTimeOffset ExpiresAt);

/// <summary>
/// A commit result. <see cref="VerifiedDurationMs"/> is the duration read from the stored header (null for a non-audio file or when the header
/// carries none); Chat applies the voice ceilings to it. <see cref="Replayed"/> is true when the upload had already been committed.
/// </summary>
public sealed record ChatMessageAttachmentCommit(ChatMessageAttachmentFact Attachment, bool IsVoice, int? VerifiedDurationMs, bool Replayed);

/// <summary>A pre-signed download of one file, valid until <see cref="ExpiresAt"/>.</summary>
public sealed record ChatMessageAttachmentDownload(Guid AttachmentId, string FileName, string ContentType, long SizeBytes, string Url, DateTimeOffset ExpiresAt);
