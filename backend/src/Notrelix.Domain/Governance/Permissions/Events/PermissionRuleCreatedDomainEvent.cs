namespace Notrelix.Domain.Governance.Permissions.Events;

public record PermissionRuleCreatedDomainEvent : WorkspaceScopedDomainEvent
{
    public Guid AccountId { get; }
    public Guid RuleId { get; }
    public string Action { get; }

    public PermissionRuleCreatedDomainEvent(
        Guid accountId,
        Guid workspaceId,
        Guid ruleId,
        string action,
        Guid? actorUserId,
        DateTimeOffset occurredAt)
        : base(workspaceId, occurredAt, actorUserId)
    {
        AccountId = accountId;
        RuleId = ruleId;
        Action = action;
    }
}
