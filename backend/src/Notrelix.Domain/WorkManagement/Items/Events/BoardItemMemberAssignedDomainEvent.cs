namespace Notrelix.Domain.WorkManagement.Items.Events;

[EventName("work-management.board-item-member-assigned")]
public sealed record BoardItemMemberAssignedDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid ItemId,
    Guid UserId,
    Guid AssignedBy,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt);
