namespace Notrelix.Domain.WorkManagement.Views.Events;

public sealed record BoardViewUserPreferenceCreatedDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid BoardId,
    Guid ViewId,
    Guid UserId,
    Guid PreferenceId,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(WorkspaceId, OccurredAt, null);
