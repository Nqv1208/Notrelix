namespace Notrelix.Domain.Chat.Messages.Events;

/// <summary>
/// A file was added to a message (Added true, with the file's metadata) or removed from it (Added false, only the file id), UC-CHT-G08.
/// The message itself does not change (no version bump); the fact lets other readers patch the message's file list.
/// </summary>
[EventName("chat.message-attachment-changed")]
public sealed record MessageAttachmentChangedDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid SpaceId,
    Guid MessageId,
    Guid ActorId,
    bool Added,
    Guid AttachmentId,
    string? FileName,
    long? SizeBytes,
    string? ContentType,
    Guid? UploadedBy,
    DateTimeOffset? AttachmentCreatedAt,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt);
