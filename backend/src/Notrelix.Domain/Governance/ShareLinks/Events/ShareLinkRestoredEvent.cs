namespace Notrelix.Domain.Governance.ShareLinks.Events;

public sealed record ShareLinkRestoredEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid LinkId,
    Guid RestoredBy,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt);
