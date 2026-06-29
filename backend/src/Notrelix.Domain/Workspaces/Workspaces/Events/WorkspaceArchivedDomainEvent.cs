namespace Notrelix.Domain.Workspaces.Workspaces.Events;

public sealed record WorkspaceArchivedDomainEvent(
    Guid WorkspaceId,
    Guid ArchivedBy,
    DateTimeOffset OccurredAt
) : WorkspaceRootDomainEvent(WorkspaceId, OccurredAt, ArchivedBy);
