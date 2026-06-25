namespace Notrelix.Domain.Governance.Permissions.Events;

public record PermissionRuleDisabledDomainEvent : DomainEvent
{
    public Guid RuleId { get; }

    public PermissionRuleDisabledDomainEvent(
        Guid workspaceId,
        Guid ruleId,
        Guid? actorUserId,
        DateTimeOffset occurredAt)
        : base(occurredAt, workspaceId, actorUserId)
    {
        RuleId = ruleId;
    }
}
