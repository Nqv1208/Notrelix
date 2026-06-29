namespace Notrelix.Domain.Collaboration.Attachments.Events;

public sealed record AttachmentRestoredDomainEvent(
    Guid WorkspaceId,
    Guid AttachmentId,
    Guid RestoredBy,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(WorkspaceId, OccurredAt, RestoredBy);
