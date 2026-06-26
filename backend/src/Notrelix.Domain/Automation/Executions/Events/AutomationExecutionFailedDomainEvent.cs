namespace Notrelix.Domain.Automation.Executions.Events;

public sealed record AutomationExecutionFailedDomainEvent(
    Guid WorkspaceId,
    Guid ExecutionId,
    Guid RuleId,
    string Error,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt, WorkspaceId, null);
