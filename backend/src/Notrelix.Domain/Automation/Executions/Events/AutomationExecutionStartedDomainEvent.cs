namespace Notrelix.Domain.Automation.Executions.Events;

public sealed record AutomationExecutionStartedDomainEvent(
    Guid WorkspaceId,
    Guid ExecutionId,
    Guid RuleId,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(WorkspaceId, OccurredAt, null);
