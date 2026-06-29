namespace Notrelix.Domain.Collaboration.Attachments.Events;

public sealed record AttachmentDeletedDomainEvent(
    Guid WorkspaceId,
    Guid AttachmentId,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(WorkspaceId, OccurredAt, null);
