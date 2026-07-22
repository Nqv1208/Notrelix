namespace Notrelix.Domain.Governance.ShareLinks.Events;

public sealed record ShareLinkDisabledEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid LinkId,
    Guid DisabledBy,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt);
