namespace Notrelix.Domain.Workspaces.Teams.Events;

public sealed record TeamRenamedDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid TeamId,
    string OldName,
    string NewName,
    Guid UpdatedBy,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(WorkspaceId, OccurredAt, UpdatedBy);
