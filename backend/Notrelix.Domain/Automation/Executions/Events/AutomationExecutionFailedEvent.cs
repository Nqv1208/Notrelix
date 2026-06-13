using Notrelix.Domain.Common;
using Notrelix.Domain.SharedKernel;

namespace Notrelix.Domain.Automation.Executions.Events;

public sealed record AutomationExecutionFailedEvent(
    Guid WorkspaceId,
    Guid ExecutionId,
    Guid RuleId,
    string Error,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt, WorkspaceId, null);
