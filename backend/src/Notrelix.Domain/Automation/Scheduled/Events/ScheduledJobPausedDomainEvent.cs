namespace Notrelix.Domain.Automation.Scheduled.Events;

public sealed record ScheduledJobPausedDomainEvent(
    Guid WorkspaceId,
    Guid JobId,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(WorkspaceId, OccurredAt, null);
