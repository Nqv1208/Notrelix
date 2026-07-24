namespace Notrelix.Domain.Automation.Scheduled.Events;

[EventName("automation.scheduled-job-paused")]
public sealed record ScheduledJobPausedDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid JobId,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt);
