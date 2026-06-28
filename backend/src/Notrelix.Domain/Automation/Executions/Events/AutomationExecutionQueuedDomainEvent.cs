namespace Notrelix.Domain.Automation.Executions.Events;

public sealed record AutomationExecutionQueuedDomainEvent(
    Guid WorkspaceId,
    Guid ExecutionId,
    Guid RuleId,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(WorkspaceId, OccurredAt, null);
