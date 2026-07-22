namespace Notrelix.Domain.Collaboration.Attachments.Events;

public sealed record AttachmentCreatedDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid AttachmentId,
    ResourceRef Target,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt);
