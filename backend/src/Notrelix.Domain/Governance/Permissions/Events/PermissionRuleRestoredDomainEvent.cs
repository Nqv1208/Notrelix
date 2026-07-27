namespace Notrelix.Domain.Governance.Permissions.Events;

[EventName("governance.permission-rule-restored")]
public sealed record PermissionRuleRestoredDomainEvent : WorkspaceScopedDomainEvent
{
    public Guid RuleId { get; }

    public PermissionRuleRestoredDomainEvent(
        Guid accountId,
        Guid workspaceId,
        Guid ruleId,
        DateTimeOffset occurredAt)
        : base(accountId, workspaceId, occurredAt)
    {
        RuleId = ruleId;
    }
}
