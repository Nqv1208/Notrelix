namespace Notrelix.Domain.Governance.Permissions.Events;

public record PermissionRuleDisabledDomainEvent : WorkspaceScopedDomainEvent
{
    public Guid RuleId { get; }

    public PermissionRuleDisabledDomainEvent(
        Guid workspaceId,
        Guid ruleId,
        Guid? actorUserId,
        DateTimeOffset occurredAt)
        : base(workspaceId, occurredAt, actorUserId)
    {
        RuleId = ruleId;
    }
}
