namespace Notrelix.Domain.Governance.ShareLinks.Events;

[EventName("governance.share-link-expired")]
public sealed record ShareLinkExpiredDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid LinkId,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt);
