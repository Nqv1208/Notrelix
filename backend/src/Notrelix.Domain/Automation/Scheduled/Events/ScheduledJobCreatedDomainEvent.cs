namespace Notrelix.Domain.Automation.Scheduled.Events;

[EventName("automation.scheduled-job-created")]
public sealed record ScheduledJobCreatedDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid JobId,
    Guid RuleId,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt);
