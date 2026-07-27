namespace Notrelix.Domain.Governance.Permissions.Events;

[EventName("governance.permission-rule-soft-deleted")]
public sealed record PermissionRuleSoftDeletedDomainEvent : WorkspaceScopedDomainEvent
{
    public Guid RuleId { get; }

    public PermissionRuleSoftDeletedDomainEvent(
        Guid accountId,
        Guid workspaceId,
        Guid ruleId,
        DateTimeOffset occurredAt)
        : base(accountId, workspaceId, occurredAt)
    {
        RuleId = ruleId;
    }
}
