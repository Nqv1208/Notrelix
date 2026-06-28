namespace Notrelix.Domain.Governance.Security.Events;

public sealed record SecurityEventRecordedEvent(
    Guid SecurityEventId,
    Guid WorkspaceId,
    SecurityEventType Type,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(WorkspaceId, OccurredAt, null);
