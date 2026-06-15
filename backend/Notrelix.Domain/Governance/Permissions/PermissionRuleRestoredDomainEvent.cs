using Notrelix.Domain.Common;

namespace Notrelix.Domain.Governance.Permissions;

public record PermissionRuleRestoredDomainEvent : DomainEvent
{
    public Guid RuleId { get; }

    public PermissionRuleRestoredDomainEvent(
        Guid workspaceId,
        Guid ruleId,
        Guid? actorUserId,
        DateTimeOffset occurredAt)
        : base(occurredAt, workspaceId, actorUserId)
    {
        RuleId = ruleId;
    }
}
