namespace Notrelix.Domain.WorkManagement.Views.Events;

[EventName("work-management.board-view-archived")]
public sealed record BoardViewArchivedDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid ViewId,
    Guid BoardId,
    Guid ArchivedBy,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt);
