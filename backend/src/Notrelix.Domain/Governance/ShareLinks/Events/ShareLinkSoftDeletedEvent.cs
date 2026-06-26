namespace Notrelix.Domain.Governance.ShareLinks.Events;

public sealed record ShareLinkSoftDeletedEvent(
    Guid WorkspaceId,
    Guid LinkId,
    Guid DeletedBy,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt, WorkspaceId, DeletedBy);
