namespace Notrelix.Domain.Documents.ResourceLinks.Events;

public sealed record ResourceLinkDeletedDomainEvent(
    Guid WorkspaceId,
    Guid LinkId,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(WorkspaceId, OccurredAt, null);
