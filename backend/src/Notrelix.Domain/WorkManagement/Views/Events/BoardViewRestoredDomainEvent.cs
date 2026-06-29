namespace Notrelix.Domain.WorkManagement.Views.Events;

public sealed record BoardViewRestoredDomainEvent(
    Guid WorkspaceId,
    Guid ViewId,
    Guid BoardId,
    Guid RestoredBy,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(WorkspaceId, OccurredAt, RestoredBy);
