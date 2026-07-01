namespace Notrelix.Domain.Automation.Executions.Events;

public sealed record AutomationExecutionCancelledDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid ExecutionId,
    Guid RuleId,
    Guid CancelledBy,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(WorkspaceId, OccurredAt, CancelledBy);
