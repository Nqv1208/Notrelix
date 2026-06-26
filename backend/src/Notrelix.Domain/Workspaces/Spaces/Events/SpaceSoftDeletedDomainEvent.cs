namespace Notrelix.Domain.Workspaces.Spaces.Events;

public sealed record SpaceSoftDeletedDomainEvent(
    Guid WorkspaceId,
    Guid SpaceId,
    Guid DeletedBy,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt, WorkspaceId, DeletedBy);
