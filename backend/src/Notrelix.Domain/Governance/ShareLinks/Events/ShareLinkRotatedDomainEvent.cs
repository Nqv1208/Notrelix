namespace Notrelix.Domain.Governance.ShareLinks.Events;

[EventName("governance.share-link-rotated")]
public sealed record ShareLinkRotatedDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid LinkId,
    Guid RotatedBy,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt);
