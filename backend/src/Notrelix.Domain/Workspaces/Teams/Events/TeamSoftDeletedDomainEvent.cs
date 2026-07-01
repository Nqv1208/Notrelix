namespace Notrelix.Domain.Workspaces.Teams.Events;

public sealed record TeamSoftDeletedDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid TeamId,
    Guid DeletedBy,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(WorkspaceId, OccurredAt, DeletedBy);
