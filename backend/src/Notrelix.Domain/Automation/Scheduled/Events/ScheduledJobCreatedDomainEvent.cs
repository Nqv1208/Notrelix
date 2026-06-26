namespace Notrelix.Domain.Automation.Scheduled.Events;

public sealed record ScheduledJobCreatedDomainEvent(
    Guid WorkspaceId,
    Guid JobId,
    Guid RuleId,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt, WorkspaceId, null);
