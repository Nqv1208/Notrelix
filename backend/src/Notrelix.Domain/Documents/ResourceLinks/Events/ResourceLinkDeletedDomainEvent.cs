namespace Notrelix.Domain.Documents.ResourceLinks.Events;

[EventName("documents.resource-link-deleted")]
public sealed record ResourceLinkDeletedDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid LinkId,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt);
