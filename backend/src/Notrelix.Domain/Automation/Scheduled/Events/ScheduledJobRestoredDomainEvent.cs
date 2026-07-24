namespace Notrelix.Domain.Automation.Scheduled.Events;

[EventName("automation.scheduled-job-restored")]
public sealed record ScheduledJobRestoredDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid JobId,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt);
