namespace Notrelix.Domain.Automation.Scheduled.Events;

[EventName("automation.scheduled-job-run-completed")]
public sealed record ScheduledJobRunCompletedDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid JobId,
    DateTimeOffset OccurredAt,
    DateTimeOffset NextRunAt
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt);
