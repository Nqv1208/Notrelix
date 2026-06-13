using Notrelix.Domain.Common;

namespace Notrelix.Domain.Automation.Rules;

public record AutomationRuleEnabledDomainEvent : DomainEvent
{
    public Guid RuleId { get; }

    public AutomationRuleEnabledDomainEvent(
        Guid workspaceId,
        Guid ruleId,
        Guid? actorUserId,
        DateTimeOffset occurredAt) 
        : base(occurredAt, workspaceId, actorUserId)
    {
        RuleId = ruleId;
    }
}
