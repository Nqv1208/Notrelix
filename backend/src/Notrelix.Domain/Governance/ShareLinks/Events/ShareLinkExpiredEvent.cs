namespace Notrelix.Domain.Governance.ShareLinks.Events;

public sealed record ShareLinkExpiredEvent(
    Guid WorkspaceId,
    Guid LinkId,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt, WorkspaceId, null);
