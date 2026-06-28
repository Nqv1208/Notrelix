namespace Notrelix.Domain.Automation.Scheduled.Events;

public sealed record ScheduledJobRestoredDomainEvent(
    Guid WorkspaceId,
    Guid JobId,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(WorkspaceId, OccurredAt, null);
