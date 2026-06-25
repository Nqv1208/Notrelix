namespace Notrelix.Domain.Governance.Permissions.Events;

public record PermissionRuleRestoredDomainEvent : DomainEvent
{
    public Guid RuleId { get; }

    public PermissionRuleRestoredDomainEvent(
        Guid workspaceId,
        Guid ruleId,
        Guid? actorUserId,
        DateTimeOffset occurredAt)
        : base(occurredAt, workspaceId, actorUserId)
    {
        RuleId = ruleId;
    }
}
