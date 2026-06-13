using Notrelix.Domain.Common;

namespace Notrelix.Domain.Automation.Rules;

public record AutomationRuleDisabledDomainEvent : DomainEvent
{
    public Guid RuleId { get; }

    public AutomationRuleDisabledDomainEvent(
        Guid workspaceId,
        Guid ruleId,
        Guid? actorUserId,
        DateTimeOffset occurredAt) 
        : base(occurredAt, workspaceId, actorUserId)
    {
        RuleId = ruleId;
    }
}
