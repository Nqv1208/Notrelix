namespace Notrelix.Domain.Automation.Scheduled.Events;

public sealed record ScheduledJobFailedDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid JobId,
    string Reason,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(WorkspaceId, OccurredAt, null);
