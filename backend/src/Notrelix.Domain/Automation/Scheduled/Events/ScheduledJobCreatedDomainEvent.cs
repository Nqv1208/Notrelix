namespace Notrelix.Domain.Automation.Scheduled.Events;

public sealed record ScheduledJobCreatedDomainEvent(
    Guid WorkspaceId,
    Guid JobId,
    Guid RuleId,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(WorkspaceId, OccurredAt, null);
