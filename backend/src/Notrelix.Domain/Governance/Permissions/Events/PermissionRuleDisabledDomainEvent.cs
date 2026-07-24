namespace Notrelix.Domain.Governance.Permissions.Events;

[EventName("governance.permission-rule-disabled")]
public sealed record PermissionRuleDisabledDomainEvent : WorkspaceScopedDomainEvent
{
    public Guid RuleId { get; }

    public PermissionRuleDisabledDomainEvent(
        Guid accountId,
        Guid workspaceId,
        Guid ruleId,
        DateTimeOffset occurredAt)
        : base(accountId, workspaceId, occurredAt)
    {
        RuleId = ruleId;
    }
}
