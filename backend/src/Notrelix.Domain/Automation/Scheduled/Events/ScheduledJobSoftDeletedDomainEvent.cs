namespace Notrelix.Domain.Automation.Scheduled.Events;

public sealed record ScheduledJobSoftDeletedDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid JobId,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt, null);
