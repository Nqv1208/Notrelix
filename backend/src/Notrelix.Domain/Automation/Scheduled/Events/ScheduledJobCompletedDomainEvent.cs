namespace Notrelix.Domain.Automation.Scheduled.Events;

[EventName("automation.scheduled-job-completed")]
public sealed record ScheduledJobCompletedDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid JobId,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt);
