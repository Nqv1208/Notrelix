namespace Notrelix.Domain.Workspaces.Spaces.Events;

public sealed record SpaceArchivedDomainEvent(
    Guid WorkspaceId,
    Guid SpaceId,
    Guid ArchivedBy,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt, WorkspaceId, ArchivedBy);
