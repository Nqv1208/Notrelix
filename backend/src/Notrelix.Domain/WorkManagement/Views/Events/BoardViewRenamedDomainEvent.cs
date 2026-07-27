namespace Notrelix.Domain.WorkManagement.Views.Events;

[EventName("work-management.board-view-renamed")]
public sealed record BoardViewRenamedDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid ViewId,
    string OldName,
    string NewName,
    Guid UpdatedBy,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt);
