namespace Notrelix.Domain.Workspaces.Spaces.Events;

public sealed record SpaceMovedDomainEvent(
    Guid SpaceId,
    Guid OldWorkspaceId,
    Guid NewWorkspaceId,
    Guid MovedBy,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt, NewWorkspaceId, MovedBy);
