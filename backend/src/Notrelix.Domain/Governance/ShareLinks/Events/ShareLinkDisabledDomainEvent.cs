namespace Notrelix.Domain.Governance.ShareLinks.Events;

[EventName("governance.share-link-disabled")]
public sealed record ShareLinkDisabledDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid LinkId,
    Guid DisabledBy,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt);
