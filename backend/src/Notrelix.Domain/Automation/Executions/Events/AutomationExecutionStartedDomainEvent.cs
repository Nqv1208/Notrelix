namespace Notrelix.Domain.Automation.Executions.Events;

[EventName("automation.automation-execution-started")]
public sealed record AutomationExecutionStartedDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid ExecutionId,
    Guid RuleId,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt);
