namespace Notrelix.Domain.Governance.ShareLinks.Events;

[EventName("governance.share-link-created")]
public sealed record ShareLinkCreatedDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid LinkId,
    ResourceKind ResourceKind,
    Guid ResourceId,
    Guid CreatedBy,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt);
