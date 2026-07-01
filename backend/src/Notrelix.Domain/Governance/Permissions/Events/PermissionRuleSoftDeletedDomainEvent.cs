namespace Notrelix.Domain.Governance.Permissions.Events;

public record PermissionRuleSoftDeletedDomainEvent : WorkspaceScopedDomainEvent
{
    public Guid AccountId { get; }
    public Guid RuleId { get; }

    public PermissionRuleSoftDeletedDomainEvent(
        Guid accountId,
        Guid workspaceId,
        Guid ruleId,
        Guid? actorUserId,
        DateTimeOffset occurredAt)
        : base(workspaceId, occurredAt, actorUserId)
    {
        AccountId = accountId;
        RuleId = ruleId;
    }
}
