using Notrelix.Domain.Common;
using Notrelix.Domain.SharedKernel;

namespace Notrelix.Domain.Automation.Executions.Events;

public sealed record AutomationExecutionCancelledEvent(
    Guid WorkspaceId,
    Guid ExecutionId,
    Guid RuleId,
    Guid CancelledBy,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt);
