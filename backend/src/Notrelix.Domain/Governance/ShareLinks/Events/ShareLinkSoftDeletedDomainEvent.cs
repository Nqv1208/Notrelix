namespace Notrelix.Domain.Governance.ShareLinks.Events;

[EventName("governance.share-link-soft-deleted")]
public sealed record ShareLinkSoftDeletedDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid LinkId,
    Guid DeletedBy,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt);
