namespace Notrelix.Domain.Governance.ShareLinks.Events;

[EventName("governance.share-link-restored")]
public sealed record ShareLinkRestoredDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid LinkId,
    Guid RestoredBy,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt);
