namespace Notrelix.Domain.WorkManagement.Items.Events;

[EventName("work-management.board-item-created")]
public sealed record BoardItemCreatedDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid BoardId,
    Guid GroupId,
    Guid ItemId,
    string Name,
    Guid CreatedBy,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt);
