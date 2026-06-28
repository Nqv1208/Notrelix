namespace Notrelix.Domain.Automation.Executions.Events;

public sealed record AutomationExecutionFailedDomainEvent(
    Guid WorkspaceId,
    Guid ExecutionId,
    Guid RuleId,
    string Error,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(WorkspaceId, OccurredAt, null);
