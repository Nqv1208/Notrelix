namespace Notrelix.Domain.Documents.ResourceLinks.Events;

public sealed record ResourceLinkCreatedDomainEvent(
    Guid WorkspaceId,
    Guid SourceId,
    Guid TargetId,
    LinkType Type,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(WorkspaceId, OccurredAt, null);
