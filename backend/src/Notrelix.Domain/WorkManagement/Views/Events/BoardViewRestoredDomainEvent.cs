namespace Notrelix.Domain.WorkManagement.Views.Events;

[EventName("work-management.board-view-restored")]
public sealed record BoardViewRestoredDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid ViewId,
    Guid BoardId,
    Guid RestoredBy,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt);
