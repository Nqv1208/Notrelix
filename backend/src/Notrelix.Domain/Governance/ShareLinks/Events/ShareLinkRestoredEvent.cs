namespace Notrelix.Domain.Governance.ShareLinks.Events;

public sealed record ShareLinkRestoredEvent(
    Guid WorkspaceId,
    Guid LinkId,
    Guid RestoredBy,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(WorkspaceId, OccurredAt, RestoredBy);
