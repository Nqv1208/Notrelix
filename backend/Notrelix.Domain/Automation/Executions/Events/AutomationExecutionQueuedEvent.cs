using Notrelix.Domain.Common;

namespace Notrelix.Domain.Automation.Executions.Events;

public sealed record AutomationExecutionQueuedEvent(
    Guid WorkspaceId,
    Guid ExecutionId,
    Guid RuleId,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt, WorkspaceId, null);
