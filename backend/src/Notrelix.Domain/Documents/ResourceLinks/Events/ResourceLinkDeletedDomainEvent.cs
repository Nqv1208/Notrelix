namespace Notrelix.Domain.Documents.ResourceLinks.Events;

public sealed record ResourceLinkDeletedDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid LinkId,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt, null);
