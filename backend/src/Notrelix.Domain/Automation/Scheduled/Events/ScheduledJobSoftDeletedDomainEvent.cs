namespace Notrelix.Domain.Automation.Scheduled.Events;

public sealed record ScheduledJobSoftDeletedDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid JobId,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(WorkspaceId, OccurredAt, null);
