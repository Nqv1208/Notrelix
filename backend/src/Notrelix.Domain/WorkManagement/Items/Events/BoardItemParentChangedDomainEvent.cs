namespace Notrelix.Domain.WorkManagement.Items.Events;

[EventName("work-management.board-item-parent-assigned", Version = 2)]
public sealed record BoardItemParentChangedDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid BoardId,
    Guid ItemId,
    Guid? PreviousParentItemId,
    Guid? NewParentItemId,
    int PreviousLevel,
    int NewLevel,
    Guid UpdatedBy,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt);
