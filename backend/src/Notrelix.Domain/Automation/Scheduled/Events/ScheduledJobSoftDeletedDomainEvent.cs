namespace Notrelix.Domain.Automation.Scheduled.Events;

public sealed record ScheduledJobSoftDeletedDomainEvent(
    Guid WorkspaceId,
    Guid JobId,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt, WorkspaceId, null);
