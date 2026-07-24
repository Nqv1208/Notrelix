namespace Notrelix.Domain.WorkManagement.Views.Events;

[EventName("work-management.board-view-user-preference-filter-changed")]
public sealed record BoardViewUserPreferenceFilterChangedDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid BoardId,
    Guid ViewId,
    Guid UserId,
    Guid PreferenceId,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt);
