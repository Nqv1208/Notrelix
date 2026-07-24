namespace Notrelix.Domain.Automation.Executions.Events;

[EventName("automation.automation-execution-cancelled")]
public sealed record AutomationExecutionCancelledDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid ExecutionId,
    Guid RuleId,
    Guid CancelledBy,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt);
