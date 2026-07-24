namespace Notrelix.Domain.Automation.Scheduled.Events;

[EventName("automation.scheduled-job-soft-deleted")]
public sealed record ScheduledJobSoftDeletedDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid JobId,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt);
