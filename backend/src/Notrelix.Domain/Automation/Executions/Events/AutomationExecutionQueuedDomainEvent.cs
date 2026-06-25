namespace Notrelix.Domain.Automation.Executions.Events;

public sealed record AutomationExecutionQueuedDomainEvent(
    Guid WorkspaceId,
    Guid ExecutionId,
    Guid RuleId,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt, WorkspaceId, null);
