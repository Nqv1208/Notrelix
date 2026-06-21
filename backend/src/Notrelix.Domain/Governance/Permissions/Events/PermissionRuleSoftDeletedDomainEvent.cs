using Notrelix.Domain.Common;

namespace Notrelix.Domain.Governance.Permissions.Events;

public record PermissionRuleSoftDeletedDomainEvent : DomainEvent
{
    public Guid RuleId { get; }

    public PermissionRuleSoftDeletedDomainEvent(
        Guid workspaceId,
        Guid ruleId,
        Guid? actorUserId,
        DateTimeOffset occurredAt)
        : base(occurredAt, workspaceId, actorUserId)
    {
        RuleId = ruleId;
    }
}
