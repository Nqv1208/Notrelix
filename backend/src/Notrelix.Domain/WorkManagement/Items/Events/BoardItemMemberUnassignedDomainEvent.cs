namespace Notrelix.Domain.WorkManagement.Items.Events;

[EventName("work-management.board-item-member-unassigned")]
public sealed record BoardItemMemberUnassignedDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid ItemId,
    Guid UserId,
    Guid UnassignedBy,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt);
