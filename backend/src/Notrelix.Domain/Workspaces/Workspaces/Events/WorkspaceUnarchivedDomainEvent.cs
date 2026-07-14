namespace Notrelix.Domain.Workspaces.Workspaces.Events;

public sealed record WorkspaceUnarchivedDomainEvent(
    Guid WorkspaceId,
    Guid UnarchivedBy,
    DateTimeOffset OccurredAt
) : WorkspaceRootDomainEvent(WorkspaceId, OccurredAt, UnarchivedBy);
