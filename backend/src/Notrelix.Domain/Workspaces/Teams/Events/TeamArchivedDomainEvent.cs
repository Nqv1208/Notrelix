namespace Notrelix.Domain.Workspaces.Teams.Events;

public sealed record TeamArchivedDomainEvent(
    Guid WorkspaceId,
    Guid TeamId,
    Guid ArchivedBy,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(WorkspaceId, OccurredAt, ArchivedBy);
