namespace Notrelix.Domain.Collaboration.Attachments.Events;

[EventName("collaboration.attachment-restored")]
public sealed record AttachmentRestoredDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid AttachmentId,
    Guid RestoredBy,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt);
