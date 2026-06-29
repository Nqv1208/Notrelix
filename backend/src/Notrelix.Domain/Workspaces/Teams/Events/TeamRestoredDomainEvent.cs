namespace Notrelix.Domain.Workspaces.Teams.Events;

public sealed record TeamRestoredDomainEvent(
    Guid WorkspaceId,
    Guid TeamId,
    Guid RestoredBy,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(WorkspaceId, OccurredAt, RestoredBy);
