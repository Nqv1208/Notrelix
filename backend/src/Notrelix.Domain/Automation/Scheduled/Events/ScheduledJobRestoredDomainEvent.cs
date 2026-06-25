namespace Notrelix.Domain.Automation.Scheduled.Events;

public sealed record ScheduledJobRestoredDomainEvent(
    Guid WorkspaceId,
    Guid JobId,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt, WorkspaceId, null);
