namespace Notrelix.Domain.Collaboration.Attachments.Events;

public sealed record AttachmentCreatedDomainEvent(
    Guid WorkspaceId,
    Guid AttachmentId,
    ResourceRef Target,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(WorkspaceId, OccurredAt, null);
