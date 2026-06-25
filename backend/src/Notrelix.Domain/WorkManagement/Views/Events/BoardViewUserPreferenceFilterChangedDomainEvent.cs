namespace Notrelix.Domain.WorkManagement.Views.Events;

public sealed record BoardViewUserPreferenceFilterChangedDomainEvent(
    Guid WorkspaceId,
    Guid BoardId,
    Guid ViewId,
    Guid UserId,
    Guid PreferenceId,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt, WorkspaceId, null);
