namespace Notrelix.Domain.WorkManagement.Views.Events;

public sealed record BoardViewConfigUpdatedDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid ViewId,
    Guid BoardId,
    Guid UpdatedBy,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(WorkspaceId, OccurredAt, UpdatedBy);
