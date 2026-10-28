namespace Notrelix.Domain.WorkManagement.Items.Events;

[EventName("work-management.board-item-moved-to-board")]
public sealed record BoardItemMovedToBoardDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid ItemId,
    Guid FromBoardId,
    Guid ToBoardId,
    Guid FromGroupId,
    Guid ToGroupId,
    string ItemKey,
    Guid MovedBy,
    DateTimeOffset OccurredAt,
    long Version
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt);
