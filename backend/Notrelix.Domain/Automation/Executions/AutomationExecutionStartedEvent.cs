using Notrelix.Domain.Common;
using Notrelix.Domain.SharedKernel;

namespace Notrelix.Domain.Automation.Executions;

public sealed record AutomationExecutionStartedEvent(
    Guid WorkspaceId,
    Guid ExecutionId,
    Guid RuleId,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt);
