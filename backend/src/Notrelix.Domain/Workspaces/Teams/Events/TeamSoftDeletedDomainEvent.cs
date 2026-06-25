namespace Notrelix.Domain.Workspaces.Teams.Events;

public sealed record TeamSoftDeletedDomainEvent(
    Guid WorkspaceId,
    Guid TeamId,
    Guid DeletedBy,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt, WorkspaceId, DeletedBy);
