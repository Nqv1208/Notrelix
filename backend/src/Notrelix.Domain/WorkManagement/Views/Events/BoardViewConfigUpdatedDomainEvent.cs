namespace Notrelix.Domain.WorkManagement.Views.Events;

[EventName("work-management.board-view-config-updated")]
public sealed record BoardViewConfigUpdatedDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid ViewId,
    Guid BoardId,
    Guid UpdatedBy,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt);
