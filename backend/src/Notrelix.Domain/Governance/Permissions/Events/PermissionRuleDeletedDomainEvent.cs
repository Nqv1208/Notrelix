namespace Notrelix.Domain.Governance.Permissions.Events;

[EventName("governance.permission-rule-deleted")]
public sealed record PermissionRuleDeletedDomainEvent : WorkspaceScopedDomainEvent
{
    public Guid RuleId { get; }

    public PermissionRuleDeletedDomainEvent(
        Guid accountId,
        Guid workspaceId,
        Guid ruleId,
        DateTimeOffset occurredAt)
        : base(accountId, workspaceId, occurredAt)
    {
        RuleId = ruleId;
    }
}
