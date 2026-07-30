namespace Notrelix.Domain.Automation.Scheduled.Events;

[EventName("automation.scheduled-job-deleted")]
public sealed record ScheduledJobDeletedDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid JobId,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt);
