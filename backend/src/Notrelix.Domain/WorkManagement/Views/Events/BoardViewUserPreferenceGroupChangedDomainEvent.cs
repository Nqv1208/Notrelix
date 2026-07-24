namespace Notrelix.Domain.WorkManagement.Views.Events;

[EventName("work-management.board-view-user-preference-group-changed")]
public sealed record BoardViewUserPreferenceGroupChangedDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid BoardId,
    Guid ViewId,
    Guid UserId,
    Guid PreferenceId,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt);
