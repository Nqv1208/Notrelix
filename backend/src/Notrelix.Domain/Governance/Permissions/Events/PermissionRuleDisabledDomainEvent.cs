namespace Notrelix.Domain.Governance.Permissions.Events;

public record PermissionRuleDisabledDomainEvent : WorkspaceScopedDomainEvent
{
    public Guid AccountId { get; }
    public Guid RuleId { get; }

    public PermissionRuleDisabledDomainEvent(
        Guid accountId,
        Guid workspaceId,
        Guid ruleId,
        Guid? actorUserId,
        DateTimeOffset occurredAt)
        : base(accountId, workspaceId, occurredAt, actorUserId)
    {
        AccountId = accountId;
        RuleId = ruleId;
    }
}
