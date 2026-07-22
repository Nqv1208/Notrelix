namespace Notrelix.Domain.Governance.ShareLinks.Events;

public sealed record ShareLinkSoftDeletedEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid LinkId,
    Guid DeletedBy,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt);
