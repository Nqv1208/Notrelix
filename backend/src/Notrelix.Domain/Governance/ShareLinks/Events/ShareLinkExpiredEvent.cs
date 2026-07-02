namespace Notrelix.Domain.Governance.ShareLinks.Events;

public sealed record ShareLinkExpiredEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid LinkId,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt, null);
