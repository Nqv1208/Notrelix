namespace Notrelix.Domain.Automation.Scheduled.Events;

public sealed record ScheduledJobRunCompletedDomainEvent(
    Guid WorkspaceId,
    Guid JobId,
    DateTimeOffset OccurredAt,
    DateTimeOffset NextRunAt
) : WorkspaceScopedDomainEvent(WorkspaceId, OccurredAt, null);
