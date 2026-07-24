namespace Notrelix.Domain.Documents.ResourceLinks.Events;

[EventName("documents.resource-link-restored")]
public sealed record ResourceLinkRestoredDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid LinkId,
    Guid RestoredBy,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt);