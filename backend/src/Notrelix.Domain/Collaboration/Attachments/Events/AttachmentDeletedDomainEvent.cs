namespace Notrelix.Domain.Collaboration.Attachments.Events;

[EventName("collaboration.attachment-deleted")]
public sealed record AttachmentDeletedDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid AttachmentId,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt);
