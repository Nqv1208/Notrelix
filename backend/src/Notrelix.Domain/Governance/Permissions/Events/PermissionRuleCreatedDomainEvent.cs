namespace Notrelix.Domain.Governance.Permissions.Events;

[EventName("governance.permission-rule-created")]
public sealed record PermissionRuleCreatedDomainEvent : WorkspaceScopedDomainEvent
{
    public Guid RuleId { get; }
    public string Action { get; }

    public PermissionRuleCreatedDomainEvent(
        Guid accountId,
        Guid workspaceId,
        Guid ruleId,
        string action,
        DateTimeOffset occurredAt)
        : base(accountId, workspaceId, occurredAt)
    {
        RuleId = ruleId;
        Action = action;
    }
}
