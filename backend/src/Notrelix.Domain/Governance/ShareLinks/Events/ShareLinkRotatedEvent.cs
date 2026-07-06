namespace Notrelix.Domain.Governance.ShareLinks.Events;

public sealed record ShareLinkRotatedEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid LinkId,
    Guid RotatedBy,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt, RotatedBy);
