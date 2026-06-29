namespace Notrelix.Domain.WorkManagement.Views.Events;

public sealed record BoardViewDeletedDomainEvent(
    Guid WorkspaceId,
    Guid ViewId,
    Guid BoardId,
    Guid DeletedBy,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(WorkspaceId, OccurredAt, DeletedBy);
