using Notrelix.Domain.Common;
using Notrelix.Domain.SharedKernel;

namespace Notrelix.Domain.Automation.Executions.Events;

public sealed record AutomationExecutionSucceededDomainEvent(
    Guid WorkspaceId,
    Guid ExecutionId,
    Guid RuleId,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt, WorkspaceId, null);
