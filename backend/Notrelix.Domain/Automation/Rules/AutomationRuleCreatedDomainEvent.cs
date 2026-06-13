using Notrelix.Domain.Common;

namespace Notrelix.Domain.Automation.Rules;

public record AutomationRuleCreatedDomainEvent : DomainEvent
{
    public Guid RuleId { get; }
    public string Name { get; }

    public AutomationRuleCreatedDomainEvent(
        Guid workspaceId,
        Guid ruleId,
        string name,
        Guid? actorUserId,
        DateTimeOffset occurredAt) 
        : base(occurredAt, workspaceId, actorUserId)
    {
        RuleId = ruleId;
        Name = name;
    }
}
