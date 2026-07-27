namespace Notrelix.Domain.Collaboration.Attachments.Events;

[EventName("collaboration.attachment-created")]
public sealed record AttachmentCreatedDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid AttachmentId,
    ResourceRef Target,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt);
