namespace Notrelix.Domain.Workspaces.Teams.Events;

public sealed record TeamRenamedDomainEvent(
    Guid WorkspaceId,
    Guid TeamId,
    string OldName,
    string NewName,
    Guid UpdatedBy,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt, WorkspaceId, UpdatedBy);
