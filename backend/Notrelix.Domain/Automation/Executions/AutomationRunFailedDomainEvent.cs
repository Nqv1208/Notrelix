using Notrelix.Domain.Common;

namespace Notrelix.Domain.Automation.Executions;

public record AutomationRunFailedDomainEvent : DomainEvent
{
    public Guid RunId { get; }
    public Guid RuleId { get; }
    public string Error { get; }

    public AutomationRunFailedDomainEvent(
        Guid workspaceId,
        Guid runId,
        Guid ruleId,
        string error,
        Guid? actorUserId,
        DateTimeOffset occurredAt) 
        : base(occurredAt, workspaceId, actorUserId)
    {
        RunId = runId;
        RuleId = ruleId;
        Error = error;
    }
}
