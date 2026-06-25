namespace Notrelix.Domain.Automation.Scheduled.Events;

public sealed record ScheduledJobCompletedDomainEvent(
    Guid WorkspaceId,
    Guid JobId,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt, WorkspaceId, null);
