namespace Notrelix.Domain.Automation.Executions.Events;

[EventName("automation.automation-execution-failed")]
public sealed record AutomationExecutionFailedDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid ExecutionId,
    Guid RuleId,
    string Error,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt);
