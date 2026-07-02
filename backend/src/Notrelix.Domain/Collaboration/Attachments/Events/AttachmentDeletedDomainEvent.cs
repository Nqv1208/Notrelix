namespace Notrelix.Domain.Collaboration.Attachments.Events;

public sealed record AttachmentDeletedDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid AttachmentId,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt, null);
