namespace Notrelix.Domain.Automation.Executions.Events;

[EventName("automation.automation-execution-queued")]
public sealed record AutomationExecutionQueuedDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid ExecutionId,
    Guid RuleId,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt);
