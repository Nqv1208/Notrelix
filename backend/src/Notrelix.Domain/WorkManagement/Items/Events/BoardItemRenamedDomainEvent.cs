namespace Notrelix.Domain.WorkManagement.Items.Events;

[EventName("work-management.board-item-renamed")]
public sealed record BoardItemRenamedDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid ItemId,
    Guid BoardId,
    string OldName,
    string NewName,
    Guid UpdatedBy,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt);
