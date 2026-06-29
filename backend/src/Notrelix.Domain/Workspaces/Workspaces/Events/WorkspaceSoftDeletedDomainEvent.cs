namespace Notrelix.Domain.Workspaces.Workspaces.Events;

public sealed record WorkspaceSoftDeletedDomainEvent(
    Guid WorkspaceId,
    Guid DeletedBy,
    DateTimeOffset OccurredAt
) : WorkspaceRootDomainEvent(WorkspaceId, OccurredAt, DeletedBy);
