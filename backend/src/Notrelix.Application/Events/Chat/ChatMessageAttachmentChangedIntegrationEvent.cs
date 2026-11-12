namespace Notrelix.Application.Events.Chat;

/// <summary>
/// A file of a space's message was added or removed (UC-CHT-G08): ChangeKind "added" carries the file's metadata, "removed" only its id.
/// No bytes and no URL; it reaches the channel stream as a compact frame and a reader patches the message's file list or refetches it
/// over REST.
/// </summary>
[IntegrationEventTenantScope(IntegrationEventTenantScope.Workspace)]
[EventName("chat.message.attachment.changed", Version = 1)]
public sealed record ChatMessageAttachmentChangedIntegrationEvent(
    Guid EventId,
    Guid? AccountId,
    Guid? WorkspaceId,
    Guid SpaceId,
    Guid MessageId,
    string ChangeKind,
    Guid AttachmentId,
    string? FileName,
    long? SizeBytes,
    string? ContentType,
    Guid? UploadedBy,
    DateTimeOffset? AttachmentCreatedAt,
    Guid CorrelationId,
    Guid? ActorUserId = null,
    Guid? CausationId = null,
    DateTimeOffset OccurredAt = default
) : IntegrationEvent(
    eventId: EventId,
    messageName: "chat.message.attachment.changed",
    schemaVersion: 1,
    correlationId: CorrelationId,
    sourceEventId: null,
    accountId: AccountId,
    workspaceId: WorkspaceId,
    actorUserId: ActorUserId,
    causationId: CausationId,
    occurredAt: OccurredAt
);
