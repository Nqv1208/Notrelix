namespace Notrelix.Domain.Governance.ShareLinks.Events;

public sealed record ShareLinkDisabledEvent(
    Guid WorkspaceId,
    Guid LinkId,
    Guid DisabledBy,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(WorkspaceId, OccurredAt, DisabledBy);
