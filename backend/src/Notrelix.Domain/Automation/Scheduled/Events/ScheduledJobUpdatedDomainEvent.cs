namespace Notrelix.Domain.Automation.Scheduled.Events;

[EventName("automation.scheduled-job-updated")]
public sealed record ScheduledJobUpdatedDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid JobId,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt);
