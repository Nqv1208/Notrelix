namespace Notrelix.Domain.Automation.Scheduled.Events;

public sealed record ScheduledJobSoftDeletedDomainEvent(
    Guid WorkspaceId,
    Guid JobId,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(WorkspaceId, OccurredAt, null);
