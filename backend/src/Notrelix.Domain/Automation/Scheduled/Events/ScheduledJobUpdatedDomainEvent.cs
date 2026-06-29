namespace Notrelix.Domain.Automation.Scheduled.Events;

public sealed record ScheduledJobUpdatedDomainEvent(
    Guid WorkspaceId,
    Guid JobId,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(WorkspaceId, OccurredAt, null);
