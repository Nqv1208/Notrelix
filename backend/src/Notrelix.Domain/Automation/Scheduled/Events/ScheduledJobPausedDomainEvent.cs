namespace Notrelix.Domain.Automation.Scheduled.Events;

public sealed record ScheduledJobPausedDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid JobId,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt, null);
