namespace Notrelix.Domain.Governance.ShareLinks.Events;

public sealed record ShareLinkRotatedEvent(
    Guid WorkspaceId,
    Guid LinkId,
    Guid RotatedBy,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(WorkspaceId, OccurredAt, RotatedBy);
