namespace Notrelix.Domain.Workspaces.Teams.Events;

public sealed record TeamMemberRemovedDomainEvent(
    Guid WorkspaceId,
    Guid TeamId,
    Guid UserId,
    Guid RemovedBy,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(WorkspaceId, OccurredAt, RemovedBy);
