namespace Notrelix.Domain.Workspaces.Teams.Events;

public sealed record TeamRestoredDomainEvent(
    Guid WorkspaceId,
    Guid TeamId,
    Guid RestoredBy,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt, WorkspaceId, RestoredBy);
