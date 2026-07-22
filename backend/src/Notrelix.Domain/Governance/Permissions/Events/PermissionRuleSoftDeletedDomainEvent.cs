namespace Notrelix.Domain.Governance.Permissions.Events;

public record PermissionRuleSoftDeletedDomainEvent : WorkspaceScopedDomainEvent
{
    public Guid AccountId { get; }
    public Guid RuleId { get; }

    public PermissionRuleSoftDeletedDomainEvent(
        Guid accountId,
        Guid workspaceId,
        Guid ruleId,
        DateTimeOffset occurredAt)
        : base(accountId, workspaceId, occurredAt)
    {
        AccountId = accountId;
        RuleId = ruleId;
    }
}
