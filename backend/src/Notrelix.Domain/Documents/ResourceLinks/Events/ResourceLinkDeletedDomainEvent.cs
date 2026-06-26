namespace Notrelix.Domain.Documents.ResourceLinks.Events;

public sealed record ResourceLinkDeletedDomainEvent(
    Guid WorkspaceId,
    Guid LinkId,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt, WorkspaceId, null);
