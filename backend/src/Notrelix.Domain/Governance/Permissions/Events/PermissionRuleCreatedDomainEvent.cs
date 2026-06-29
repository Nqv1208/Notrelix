namespace Notrelix.Domain.Governance.Permissions.Events;

public record PermissionRuleCreatedDomainEvent : WorkspaceScopedDomainEvent
{
    public Guid RuleId { get; }
    public string Action { get; }

    public PermissionRuleCreatedDomainEvent(
        Guid workspaceId,
        Guid ruleId,
        string action,
        Guid? actorUserId,
        DateTimeOffset occurredAt)
        : base(workspaceId, occurredAt, actorUserId)
    {
        RuleId = ruleId;
        Action = action;
    }
}
