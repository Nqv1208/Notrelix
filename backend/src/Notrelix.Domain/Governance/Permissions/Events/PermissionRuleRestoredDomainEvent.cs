namespace Notrelix.Domain.Governance.Permissions.Events;

public record PermissionRuleRestoredDomainEvent : WorkspaceScopedDomainEvent
{
    public Guid RuleId { get; }

    public PermissionRuleRestoredDomainEvent(
        Guid workspaceId,
        Guid ruleId,
        Guid? actorUserId,
        DateTimeOffset occurredAt)
        : base(workspaceId, occurredAt, actorUserId)
    {
        RuleId = ruleId;
    }
}
