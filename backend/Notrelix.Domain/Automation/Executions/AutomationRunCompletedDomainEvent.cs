using Notrelix.Domain.Common;

namespace Notrelix.Domain.Automation.Executions;

public record AutomationRunCompletedDomainEvent : DomainEvent
{
    public Guid RunId { get; }
    public Guid RuleId { get; }

    public AutomationRunCompletedDomainEvent(
        Guid workspaceId,
        Guid runId,
        Guid ruleId,
        Guid? actorUserId,
        DateTimeOffset occurredAt) 
        : base(occurredAt, workspaceId, actorUserId)
    {
        RunId = runId;
        RuleId = ruleId;
    }
}
