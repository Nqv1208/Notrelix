namespace Notrelix.Domain.Governance.Permissions.Events;

public record PermissionRuleSoftDeletedDomainEvent : WorkspaceScopedDomainEvent
{
    public Guid RuleId { get; }

    public PermissionRuleSoftDeletedDomainEvent(
        Guid workspaceId,
        Guid ruleId,
        Guid? actorUserId,
        DateTimeOffset occurredAt)
        : base(workspaceId, occurredAt, actorUserId)
    {
        RuleId = ruleId;
    }
}
