namespace Notrelix.Domain.WorkManagement.Views.Events;

public sealed record BoardViewUserPreferenceSortChangedDomainEvent(
    Guid WorkspaceId,
    Guid BoardId,
    Guid ViewId,
    Guid UserId,
    Guid PreferenceId,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(WorkspaceId, OccurredAt, null);
