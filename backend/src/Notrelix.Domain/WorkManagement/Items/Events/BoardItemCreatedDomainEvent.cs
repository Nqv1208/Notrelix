namespace Notrelix.Domain.WorkManagement.Items.Events;

[EventName("work-management.board-item-created", Version = 2)]
public sealed record BoardItemCreatedDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid BoardId,
    Guid GroupId,
    Guid ItemId,
    string Name,
    Guid CreatedBy,
    DateTimeOffset OccurredAt,
    Guid? ParentItemId,
    int ItemLevel
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt);
